using Smithers.Sessions;
using SmithersDS4.Reading;
using SmithersDS4.Reading.Calibration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;

namespace SmithersDS4.Sessions
{
    public enum CaptureMode
    {
        Trigger,
        Stream,
        Sweeping
    }
  
    public class DS4VideoShotDefintion : ShotDefinition
    {
        public override int ShotDuration { get { return 100; } }

        /// <summary>
        /// Maximum frames which will be recorded. If more frames arrive during
        /// ShotDuration, subsequent frames will be discarded.
        /// </summary>
        public override int MaximumFrameCount { get { return 80; } }
    }

    public class DS4Session : Session<DS4Meta, Shot<ShotDefinition, DS4SavedItem>, ShotDefinition, DS4SavedItem>, INotifyPropertyChanged
    {
        public DS4Session(string path, IEnumerable<ShotDefinition> shotDefinition)
            : base(path, shotDefinition)
        {
            this.SweepCounter = 0;
        }
        object _uploader;

        public event PropertyChangedEventHandler PropertyChanged;

        public int SweepCounter { get; set; }

        public DS4Meta Metadata { get; set; }

        public string CompressedScanFile
        {
            get
            {
                return SessionPath + ".scan";
            }
        }

        public object Uploader
        {
            get
            {
                return _uploader;
            }
            set
            {
                _uploader = value;
                OnPropertyChanged("Uploader");
            }
        }

        bool isCompressing = false;

        public Boolean Compressing
        {
            get
            {
                return isCompressing;
            }

            set
            {
                isCompressing = value;
                OnPropertyChanged("Compressing");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class SessionManager<TSession, TMetadata, TShot, TShotDefinition, TSavedItem>
        where TShotDefinition : Smithers.Sessions.ShotDefinition
        where TSavedItem : DS4SavedItem, new()
        where TShot : Shot<TShotDefinition, TSavedItem>, new()
        where TMetadata : DS4Meta
        where TSession : DS4Session
    {
        public static readonly string CALIBRATION_FILE = "Calibration.json";

        DS4Session _session;

        TShot _nextShot;
        TShot _capturingShot;
        TShot _writingShot;

        DateTime? _lastShotTaken = null;

        bool _wroteCalibrationRecord;
        bool _cameraConfigLocked;

        List<Handle<MemoryFrame>> _frameHandles = new List<Handle<MemoryFrame>>();
        int _frameCount;
        List<DS4SavedItem.AccelerometerStats> acceleroStatsList = new List<DS4SavedItem.AccelerometerStats>();

        Task<DS4CalibrationRecord> _calibration;
        CaptureMode _mode;

        FrameReader _reader;

        FrameSerializer _serializer = new FrameSerializer();

        /// <summary>
        /// Fires when ready for a new shot.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ReadyForShot;

        /// <summary>
        /// Fires before each shot is taken.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotBeginning;

        /// <summary>
        /// Fires after each shot is taken, but before frames are written to disk.
        ///
        /// If an error occurs, ErrorMessage will be set.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotCompletedSuccess;

        /// <summary>
        /// Fires after each shot is taken, but before frames are written to disk.
        ///
        /// If an error occurs, ErrorMessage will be set.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotCompletedError;

        /// <summary>
        /// Fires after each shot is written to disk.
        /// 
        /// If an error occurs, ErrorMessage and Exception will be set.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotSavedSuccess;

        public event EventHandler<SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> FrameCaptured;

        /// <summary>
        /// Fires after each shot is written to disk.
        /// 
        /// If an error occurs, ErrorMessage and Exception will be set.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> ShotSavedError;

        /// <summary>
        /// Fires after the last shot is successfully written to disk.
        /// </summary>
        public event EventHandler<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>> LastShotFinished;
        
        public TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs> accChangedEventHandler;
        
        private bool _sweeping;


        public DS4Session Session
        { 
            get { return _session   ; }
            set { _session = value; } 
        }

        public bool isSweeping
        {
           get 
           {
               return _mode == CaptureMode.Sweeping && _sweeping;
           }
        }

        public int NumFrameInMemory { get { return _frameCount; } }

        public SessionManager(TSession session)
        {
            _session = session;
            _wroteCalibrationRecord = false;
            _mode = CaptureMode.Trigger;
            accChangedEventHandler = new TypedEventHandler<Accelerometer, AccelerometerReadingChangedEventArgs>(AccReadingChanged);
        }

        /// <summary>
        /// Get ready for the first shot.
        /// </summary>
        public void AttachToReader(FrameReader reader)
        {
            if (_reader != null)
                throw new InvalidOperationException("We've already attached to a reader!");

            _reader = reader;
        }

        private void AccReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs e)
        {
            AccelerometerReading reading = e.Reading;

            if (_sweeping && _capturingShot != null)
            {
                acceleroStatsList.Add(new DS4SavedItem.AccelerometerStats
                {
                    accX = reading.AccelerationX,
                    accY = reading.AccelerationY,
                    accZ = reading.AccelerationZ,
                    timestamp = reading.Timestamp.UtcTicks

                });
            }
            
        }

        /// <summary>
        /// Move to the next shot which needs to be completed. When a specific
        /// shot is provided, that logic is used instead.
        /// </summary>
        /// <returns></returns>
        public virtual void PrepareForNextShot(TShot shot = null)
        {
            if (shot != null)
            {
                if (!_session.Shots.Contains(shot as Shot<ShotDefinition, DS4SavedItem>))
                    throw new ArgumentException("Shot does not belong to this session");
                else if (shot.Completed)
                    throw new ArgumentException("Shot is already completed");

                _nextShot = shot;
            }
            else
            {
                _nextShot = _session.Shots.Find(x => !x.Completed) as TShot;
            }

            if (_nextShot != null && ReadyForShot != null)
                ReadyForShot(this, new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_nextShot));
        }

        public virtual void CaptureShot(CaptureMode mode)
        {
            if (_capturingShot != null)
                throw new InvalidOperationException("We're in the middle of capturing a shot!");
            else if (_writingShot != null)
                throw new InvalidOperationException("We're in the middle of writing a shot!");
            else if ( mode != CaptureMode.Stream && _nextShot == null)
                throw new InvalidOperationException("Capture is already finished");

            _capturingShot = _nextShot;
            _mode = mode;

            _session.SweepCounter = 0;
            _reader.BeginScenePerception();

            if (ShotBeginning != null)
                ShotBeginning(this, new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot));
        }

        public virtual bool ValidateShot(out string message)
        {
            message = null;
            return true;
        }

        public virtual void SweepingStart(bool isFirstShot = false)
        {
            _mode = CaptureMode.Sweeping;
            if (_mode != CaptureMode.Sweeping)
                throw new InvalidOperationException("You can only release shot voluntarily in Sweeping mode");
            else if (_writingShot != null)
                throw new InvalidOperationException("We're in the middle of writing a shot!");

            _capturingShot = _nextShot;
            
            _reader.BeginScenePerception();

            _session.SweepCounter += 1;
            _sweeping = true;

            if (_reader.Accelerometer != null)
            {
                _reader.Accelerometer.ReadingChanged += (accChangedEventHandler);
            }

            _reader.Device.SetColorAutoExposure(true);
            _cameraConfigLocked = false;
        }

        public virtual async void SweepingEnd()
        {
            if (_mode != CaptureMode.Sweeping)
                throw new InvalidOperationException("You can only release shot voluntarily in Sweeping mode");

            if (!_sweeping || _capturingShot == null) return;

            _sweeping = false;
            _reader.CaptureEnd();
            _lastShotTaken = null;
            MoveToNextShot();

            if (_reader.Accelerometer != null)
            {
                _reader.Accelerometer.ReadingChanged -= (accChangedEventHandler);
            }
            await Task.Run(() => SaveAcceleroData());

            acceleroStatsList.Clear();
            _session.Shots.Clear();
        }

        private void SaveAcceleroData()
        {
            string sessionPath = _session.SessionPath;

            string sweepFileName = string.Format("sweep_{0:D2}.json", _session.SweepCounter);
            string accPath = Path.Combine(sessionPath, "AccData", sweepFileName);

            Smithers.Sessions.JSONHelper.Instance.Serialize(acceleroStatsList, accPath);
            
        }

        private void LockCameraExposureAndGain()
        {

#if DSAPI
             _reader.DSAPI.WriteDeviceConfig<TMetadata>(_session.Metadata, _mode);


             _reader.DSAPI.setColorExposureAndGain(_session.Metadata.DeviceConfig.ColorExposure,
                                                      _session.Metadata.DeviceConfig.ColorGain);

#else
            _reader.Device.WriteDeviceConfig<TMetadata>(_session.Metadata as TMetadata, _mode);

            //_reader.Device.SetColorAutoExposure(false);
            //_reader.Device.SetColorExposure((int)_session.Metadata.DeviceConfig.ColorExposure);
            //_reader.Device.SetColorGain((int)_session.Metadata.DeviceConfig.ColorGain);
#endif

    
        }

        public virtual void FrameArrived(object sender, FrameArrivedEventArgs ea)
        {
            // When the first frame arrive, start the calibration operation. This won't work
            // if we try to do it right after calling _sensor.Open().
            if (_calibration == null)
            {
                _calibration = Calibrator.CalibrateAsync(_reader);
                // set device config once
                //_reader.Device.SetDeviceConfig();
            }

            if (_capturingShot == null ) return;

            if (_mode == CaptureMode.Sweeping && !_sweeping)
            {
                return;
            }

            if (!_cameraConfigLocked)
            {
                LockCameraExposureAndGain();
                _cameraConfigLocked = true;
            }

            if (!_lastShotTaken.HasValue)
            {
                _lastShotTaken = DateTime.Now;
            }

            DateTime currentFrame = DateTime.Now;

            double durationFromLastShot = (currentFrame - _lastShotTaken.Value).TotalMilliseconds;


            if (durationFromLastShot != 0 && durationFromLastShot < _capturingShot.ShotDefinition.ShotDuration)
            {
                return;
            }
            // (1) Serialize frame data

            if (_frameCount >= _session.MaximumFrameCount)
            {
                Console.WriteLine(string.Format("Too many frames! Got {0} but we only have room for {1}", _frameCount + 1, _session.MaximumFrameCount));
            }

            _frameHandles.Add(ea.FrameHandle.Clone());

            // Increment whether we saved the data or not (this allows an improved error message)
            _frameCount += 1;

            if (FrameCaptured != null)
            {
                var outEvent = new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, null, null);
                FrameCaptured(this, outEvent);
            }


            if (_mode == CaptureMode.Sweeping)
            {

                if (_capturingShot !=null && _frameCount < _capturingShot.ShotDefinition.MaximumFrameCount)
                {
                    _lastShotTaken = DateTime.Now;
                    return;
                }
            }
            else if ((_mode != CaptureMode.Sweeping) && _frameCount < _capturingShot.ShotDefinition.MaximumFrameCount)
            {
                return;
            }

            if (_capturingShot != null)
            {
                MoveToNextShot();
            }
        }

        private void MoveToNextShot()
        {
            // (2) Move to the next shot
            string message;
            if (!ValidateShot(out message))
            {
                this.ClearFrames();

                var ea2 = new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

                _capturingShot = null;

                if (ShotCompletedError != null)
                    ShotCompletedError(this, ea2);

                return;
            }

            var outEvent = new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_capturingShot, message);

            _writingShot = _capturingShot;
            _capturingShot = null;

            if (ShotCompletedSuccess != null)
                ShotCompletedSuccess(this, outEvent);

            FinishShot();
        }

        private async void FinishShot()
        {

            try
            {
                await Task.Run(() => SaveFrameData());
            }
            catch (Exception e)
            {
                this.ClearFrames();

                var ea = new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_writingShot, "An error occurred while saving", e);

                _writingShot = null;

                if (ShotSavedError != null)
                    ShotSavedError(this, ea);

                return;
            }

            _writingShot.Completed = true;

            string metadataPath = Path.Combine(_session.SessionPath, DS4Session.METADATA_FILE);

            //Write device config
#if !DSAPI
            _reader.Device.WriteDeviceConfig<TMetadata>(_session.Metadata as TMetadata, _mode);
#else

            _reader.DSAPI.WriteDeviceConfig<TMetadata>(_session.Metadata, _mode);
#endif
    
            await Task.Run(() => Smithers.Sessions.JSONHelper.Instance.Serialize(_session.Metadata, metadataPath));
            

            //// Perform calibration if we haven't already
            if (!_wroteCalibrationRecord)
            {
                DS4CalibrationRecord record = await _calibration;

                string calibrationPath = Path.Combine(_session.SessionPath, CALIBRATION_FILE);
                JSONHelper.Instance.Serialize(record, calibrationPath);

                //copy calibration and metadata to record folder
                
                _wroteCalibrationRecord = true;
            }

            // Clear the frames to make sure we don't use them again
            this.ClearFrames();

            var ea2 = new Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>(_writingShot);

            _writingShot = null;

            if (ShotSavedSuccess != null)
                ShotSavedSuccess(this, ea2);

            if (_mode == CaptureMode.Stream || (_mode == CaptureMode.Sweeping))
            {
                ShotDefinition videoShot = new DS4VideoShotDefintion();

                _session.AddShot((TShotDefinition)videoShot);
            }

            PrepareForNextShot();

            if (_nextShot == null)
            {
                if (LastShotFinished != null)
                    LastShotFinished(this, ea2);
            }

            if (_mode == CaptureMode.Stream || _mode == CaptureMode.Sweeping)
            {
                //keep going
                _capturingShot = _nextShot;
            }
        }

        protected virtual IEnumerable<Smithers.Serialization.Writers.IWriter> WritersForFrame(MemoryFrame frame, int frameIndex)
        {
            var writers = new List<Smithers.Serialization.Writers.IWriter>();

            //if (frameIndex == 0)
            //{
            //    // For each pose, only save the first color and depth mapping frames
            //    writers.Add(new ColorFrameWriter(frame, _serializer));
            //}

            writers.Add(new ColorFrameWriter(frame, _serializer));
            writers.Add(new DepthFrameWriter(frame, _serializer));
#if DEBUG
            //writers.Add(new PXCMPoint3DF32ListWriter(frame, _serializer));
            //writers.Add(new PXCMPoint3DF32ObjWriter(frame, _serializer));
            //if (_reader.DSAPI == null)
            //{
            //    writers.Add(new IRLeftFrameWriter(frame, _serializer));
            //    writers.Add(new IRRightFrameWriter(frame, _serializer));
            //}
#endif
            return writers;
        }

        protected virtual string GeneratePath(TShot shot, MemoryFrame frame, int frameIndex, Smithers.Serialization.Writers.IWriter writer)
        {
            string folderName = writer.Type.Name;

            int shotCount = _session.Shots.IndexOf(shot as Shot<ShotDefinition, DS4SavedItem>) + 1;
            string shotName = string.Format("Shot_{0:D3}", shotCount);

            // 0 -> Frame_001, 1 -> Frame_002, etc.
            string frameName = string.Format("Frame_{0:D3}", frameIndex + 1);

            string fileName = string.Format(
                "{0}{1}{2}{3}",
                shotName,
                shotName == null ? "" : "_",
                frameName,
                writer.FileExtension
            );

            if (_mode == CaptureMode.Sweeping)
            {
                string sweepName = string.Format("sweep_{0:D2}", _session.SweepCounter);
                string sweepFileName = string.Format("{0:D3}{1}", frameIndex+1, writer.FileExtension);
                //In sweep mode, put sweepName out to be folder so someone can conveniently fit one sweep
                return Path.Combine(sweepName, folderName, sweepFileName);
            }

            return Path.Combine(folderName, fileName);
        }

        protected virtual IEnumerable<Tuple<Smithers.Serialization.Writers.IWriter, TSavedItem>> PrepareWriters(TShot shot, IEnumerable<MemoryFrame> frames)
        {
            var preparedWriters = new List<Tuple<Smithers.Serialization.Writers.IWriter, TSavedItem>>();

            //IWriter calibrationWriter = new CalibrationWriter(_calibration.Result);

            //preparedWriters.Add(new Tuple<IWriter, TSavedItem>(
            //    calibrationWriter,
            //    new TSavedItem()
            //    {
            //        Type = calibrationWriter.Type,
            //        Timestamp = calibrationWriter.Timestamp,
            //        Path = calibrationWriter.Type.Name + calibrationWriter.FileExtension,
            //    }
            //));

            int i = 0;
            DS4SavedItem.AccelerometerStats accStats = new DS4SavedItem.AccelerometerStats();

            foreach (MemoryFrame frame in frames)
            {
                foreach (Smithers.Serialization.Writers.IWriter writer in WritersForFrame(frame, i))
                {
                    accStats.accX = frame.AcceleroStats.accX;
                    accStats.accY = frame.AcceleroStats.accY;
                    accStats.accZ = frame.AcceleroStats.accZ;
                    accStats.timestamp = frame.AcceleroStats.timestamp.UtcTicks;

                    preparedWriters.Add(new Tuple<Smithers.Serialization.Writers.IWriter, TSavedItem>(
                        writer,
                        new TSavedItem()
                        {
                            Type = writer.Type,
                            Timestamp =  TimeSpan.FromTicks(frame.DepthTimeStamp),
                            Path = GeneratePath(shot, frame, i, writer),
                            CameraPose = frame.CameraPose,
                            TrackingAccuracy = frame.TrackingAccuracy,
                            VoxelResolution = frame.VoxelResolution,

                            DepthTimeStamp = frame.DepthTimeStamp,
                            ColorTimeStamp = frame.ColorTimeStamp,

                            acceleroStats = accStats
                        }
                    ));
                }

                i += 1;
            }

            return preparedWriters;
        }

        private void SaveFrameData()
        {
            IEnumerable<MemoryFrame> frames = _frameHandles.Select(x => x.Item);

            var preparedWriters = PrepareWriters(_writingShot, frames);

            foreach (Tuple<Smithers.Serialization.Writers.IWriter, TSavedItem> preparedWriter in preparedWriters)
            {
                Smithers.Serialization.Writers.IWriter writer = preparedWriter.Item1;
                TSavedItem savedItem = preparedWriter.Item2;

                string path = Path.Combine(_session.SessionPath, savedItem.Path);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (FileStream stream = new FileStream(path, FileMode.Create))
                {
                    writer.Write(stream);
                    stream.Close();
                }

                _writingShot.SavedItems.Add(savedItem);
            }

            List<TSavedItem> subList = new List<TSavedItem>();

            foreach(TSavedItem item in _writingShot.SavedItems) {
                subList.Add(item);
            }

            _session.Metadata.FrameItems.Add(subList as List<DS4SavedItem>);

        }

        private void ClearFrames()
        {
            foreach (Handle<MemoryFrame> frame in _frameHandles)
            {
                frame.Dispose();
            }
            _frameHandles.Clear();
            _frameCount = 0;
        }

        public void DeleteShot(TShot shot)
        {
            if (!_session.Shots.Contains(shot as Shot<ShotDefinition, DS4SavedItem>))
                throw new ArgumentException("Shot does not belong to this session");

            foreach (Smithers.Sessions.SavedItem item in shot.SavedItems)
            {
                string path = Path.Combine(_session.SessionPath, item.Path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            shot.SavedItems.Clear();
        }

        public void DeleteSweep(int sweepCount)
        {
            if (sweepCount > _session.SweepCounter)
                throw new ArgumentException("Sweep not taken yet");

            string sweepName = string.Format("sweep_{0:D2}", sweepCount);

            Directory.Delete(Path.Combine(_session.SessionPath, sweepName), true);

            _session.Metadata.FrameItems.RemoveAt(_session.Metadata.FrameItems.Count - 1);

            _session.SweepCounter -= 1;
        }

        public void Reset(TSession session)
        {
            _wroteCalibrationRecord = false;
            this.Session = session;
        }
    }
}
