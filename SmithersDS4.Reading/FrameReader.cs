using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using Windows.Devices.Sensors;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;


#if DSAPI
using DSAPIBridge;
#endif

using System.Runtime.InteropServices;
using System.Threading;


namespace SmithersDS4.Reading
{
    public class FrameArrivedEventArgs : EventArgs
    {
        public Handle<MemoryFrame> FrameHandle { get; set; }
    }

    public class FrameReader
    {
        PXCMSession _session;
        PXCMSenseManager _senseManager;        
        bool _stop = false;
        ObjectPool<MemoryFrame> _pool;

#if DSAPI
        DSAPIManaged _dsAPI;
        public DSAPIManaged DSAPI { get { return _dsAPI; } }
#endif

        bool _useDSAPI = false;

        public bool Mirrored { get; set; }
        public bool Synced { get; set; }
        public bool Record { get; set; }
        public bool Playback { get; set; }
        public bool RealTime { get; set; }
        public int RecordNumber { get ; set; }
        public String RecordedFile { get; set; }
        public string FolderPath { get; set; }
        
        public event EventHandler<FrameArrivedEventArgs> FrameArrived;
        //public event EventHandler<RoutedEventArgs> Click;
        //public event EventHandler StartCapture;
        //public event EventHandler/*<Smithers.Sessions.SessionManagerEventArgs<TShot, TShotDefinition, TSavedItem>>*/ ShotSavedSuccess;
        private PXCMScenePerception _perceptionHandle;

        public PXCMCapture.Device Device { get { return _senseManager.captureManager.device; } }

        public Accelerometer Accelerometer { get { return Accelerometer.GetDefault(); } }

        private float[] _initPose = new float[16] {
            1.0f, 0.0f, 0.0f, 2.0f,
            0.0f, 1.0f, 0.0f, 2.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
        };
        private bool _perceptionPaused;
        private bool _capturing;
        public bool Capturing {get{return _capturing;}}
        private SmithersLogger _logger;
        public string TrackingAccuracy
        {
            get
            {
                if (_perceptionHandle != null)
                {
                    return _perceptionHandle.QueryTrackingAccuracy().ToString();
                }
                return string.Empty;
            }
        }

        public FrameReader(ObjectPool<MemoryFrame> pool, SmithersLogger logger)
        {
#if DSAPI
            

            _dsAPI = new DSAPIManaged();
            _dsAPI.initializeDevice();
#else
            _pool = pool;            
            _logger = logger;
            //Directory.SetCurrentDirectory("")

            // TODO Inject this instead of creating it
            _session = PXCMSession.CreateInstance();

            if (_session == null)
            {
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("No valid plugged-in DS4 sensor found.");
            }

            _senseManager = PXCMSenseManager.CreateInstance();

            if (_senseManager == null)
            {
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Failed to create an SDK pipeline object");
            }

            _session.SetCoordinateSystem(PXCMSession.CoordinateSystem.COORDINATE_SYSTEM_REAR_OPENCV);

#endif

            this.Synced = true;
            this.Mirrored = false;
            this.Record = false;
            this.Playback = false;
            this.RealTime = true;
        }

        public void StartAsync()
        {
            System.Threading.Thread thread;

#if DSAPI
            thread = new System.Threading.Thread(this.RunLoopWithDSAPI);
#else
            thread = new System.Threading.Thread(this.RunLoop);
#endif
            thread.Start();
        }
       
        private IEnumerable<PXCMCapture.DeviceInfo> GetDevices()
        {
            PXCMSession.ImplDesc desc = new PXCMSession.ImplDesc();
            desc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            desc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            List<PXCMCapture.DeviceInfo> result = new List<PXCMCapture.DeviceInfo>();

            for (int i = 0; ; i++)
            {
                PXCMSession.ImplDesc desc1;
                if (_session.QueryImpl(desc, i, out desc1) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;
                PXCMCapture capture;
                if (_session.CreateImpl<PXCMCapture>(desc1, out capture) < pxcmStatus.PXCM_STATUS_NO_ERROR) continue;
                for (int j = 0; ; j++)
                {
                    PXCMCapture.DeviceInfo dinfo;
                    if (capture.QueryDeviceInfo(j, out dinfo) < pxcmStatus.PXCM_STATUS_NO_ERROR) break;

                    result.Add(dinfo);
                    //                    devices_iuid[sm1] = desc1.iuid;
                }
                capture.Dispose();
            }
            return result;
        }

#if DSAPI
        private void RunLoopWithDSAPI()
        {

            _dsAPI.setDepthExposureAndGain(33f, 1.0f);

            _dsAPI.enableDepth(Frame.DEPTH_WIDTH, Frame.DEPTH_WIDTH, (int)Frame.DEPTH_RATE);
            _dsAPI.enableColor(Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, (int)Frame.COLOR_RATE);

            _dsAPI.startCapture();

            while (!_stop)
            {
                _dsAPI.grabFrame();

                MemoryFrame frame = _pool.ObjectFromPool();

                byte[] depthBytes = _dsAPI.getDepthImage();

                byte[] colorBytes = _dsAPI.getColorImage();

                byte[] depthPreviewBytes = _dsAPI.getDepthImageAsRGB();
                
                Buffer.BlockCopy(depthBytes, 0, frame.BufferDepth, 0, depthBytes.Length);
                Buffer.BlockCopy(depthPreviewBytes, 0, frame.BufferDepthPreview, 0, depthPreviewBytes.Length);
                Buffer.BlockCopy(colorBytes, 0, frame.BufferColor, 0, colorBytes.Length);

                using (Handle<MemoryFrame> frameHandle = new Handle<MemoryFrame>(frame, _pool))
                {
                    FrameArrivedEventArgs eventArgs = new FrameArrivedEventArgs() { FrameHandle = frameHandle };

                    if (this.FrameArrived != null)
                    {
                        this.FrameArrived(this, eventArgs);
                    }
                }
            }
        }
#endif

        private void RunLoop()
        {
            /* IEnumerable<PXCMCapture.DeviceInfo> devices = this.GetDevices();
             if (devices.Count() == 0)
             {
                 throw new Smithers.Reading.FrameData.ScannerNotFoundException("No devices found");                
             }
             PXCMCapture.DeviceInfo device = devices.First();
             if (devices.Count() > 1)
             {
                 Console.WriteLine(String.Format("More than one device, using this one: {0}", device.name));
             }
             _senseManager.captureManager.FilterByDeviceInfo(device);*/
            
            pxcmStatus status;
            status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, Frame.COLOR_RATE);
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to enable color stream:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable color stream");
            }

            _logger.info("Color Stream Enabled:" + status.ToString());
            //status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, 0, 0, Frame.DEPTH_RATE);
            status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_RATE);
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to enable depth stream:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable depth stream");
            }

            _logger.info("Depth Stream Enabled:" + status.ToString());

            //status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_LEFT, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_RATE);
            //if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            //{
            //    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable depth stream");
            //}

            //status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_RIGHT, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_RATE);
            //if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            //{
            //    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable depth stream");
            //}
            status = _senseManager.EnableScenePerception();
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to enable scene perception:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Scene Perception failed");
            }
            _logger.info("Scene Perception Enabled:" + status.ToString());

            _perceptionHandle = _senseManager.QueryScenePerception();

            status = _senseManager.Init();
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to open sensor in the above mode:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Init failed");
            }

            _logger.info("Sensor Initialized Successfully:" + status.ToString());

            PXCMImage.ImageInfo depthInfo = new PXCMImage.ImageInfo();
            PXCMImage.ImageInfo colorInfo = new PXCMImage.ImageInfo();

            depthInfo.height = Frame.DEPTH_HEIGHT;
            depthInfo.width = Frame.DEPTH_WIDTH;
            colorInfo.height = Frame.COLOR_HEIGHT;
            colorInfo.width = Frame.COLOR_WIDTH;

            /* For UV Mapping & Projection only: Save certain properties */
            Projection projection = new Projection(_senseManager.session, _senseManager.captureManager.device, depthInfo, colorInfo);

            AccelerometerReading accelerometerReading;

            AccelerometerStats acceleroStats = new AccelerometerStats();
            //bool firstFrameRecord = true;
           // bool firstFrameNotRecord = false;
            //pxcmStatus stsInitRecord = pxcmStatus.PXCM_STATUS_PROCESS_FAILED;
            while (!_stop)
            {
                //PXCMCapture.Device.MirrorMode mirrorMode = this.Mirrored ? PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL : PXCMCapture.Device.MirrorMode.MIRROR_MODE_DISABLED;
                //{
                //    _senseManager.captureManager.device.SetMirrorMode(mirrorMode);
                //}

                /* Wait until a frame is ready: Synchronized or Asynchronous */
                
               
                //if ((Record || Playback) && firstFrameRecord)
                //{
                //    stsInitRecord = InitRecordPlay();
                //    if (this.StartCapture != null)
                //    {
                //        EventArgs eventArgs = new EventArgs();
                //        this.StartCapture(this, eventArgs);
                //    }
                //    firstFrameRecord = false;
                //    firstFrameNotRecord = true;
                //}
                //if (!Record && !Playback && firstFrameNotRecord)
                //{
                //    InitRecordPlay();
                //    if (this.ShotSavedSuccess != null)
                //    {
                //        EventArgs eventArgs = new EventArgs();
                //        this.ShotSavedSuccess(this, eventArgs);
                //    }
                //    firstFrameRecord = true;
                //    firstFrameNotRecord = false;
                //}
                //-------
                status = _senseManager.AcquireFrame(this.Synced);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    continue;
                }
                MemoryFrame frame = _pool.ObjectFromPool();
                ///* Display images */

                PXCMCapture.Sample sample;

                sample = _senseManager.QueryScenePerceptionSample();

                if (sample == null)
                    sample = _senseManager.QuerySample();

                //if (_perceptionHandle != null && sample !=null)
                //{
                //    _perceptionHandle.GetCameraPose(frame.CameraPose);
                //    frame.TrackingAccuracy = _perceptionHandle.QueryTrackingAccuracy().ToString();
                //    frame.VoxelResolution = _perceptionHandle.QueryVoxelResolution().ToString();

                //    float sceneQuality = _perceptionHandle.CheckSceneQuality(sample);

                //    if (sceneQuality > 0.3f && _perceptionPaused && !_capturing)
                //    {
                //        _perceptionPaused = false;
                //        _senseManager.PauseScenePerception(false);
                //        _perceptionHandle.Reset(_initPose);
                //    }
                //    else if (!(frame.TrackingAccuracy == "HIGH" || frame.TrackingAccuracy == "MID") && !_capturing)
                //    {
                //        ResetScenePerception();
                //    }
                //}

                //if (this.Accelerometer != null)
                //{
                //    accelerometerReading =  Accelerometer.GetCurrentReading();
                //    acceleroStats.accX = accelerometerReading.AccelerationX;
                //    acceleroStats.accY = accelerometerReading.AccelerationY;
                //    acceleroStats.accZ = accelerometerReading.AccelerationZ;
                //    acceleroStats.timestamp = accelerometerReading.Timestamp;

                //    frame.AcceleroStats = acceleroStats;
                //}

                if (sample.color != null)
                {
                    sample.color.CopyToByteArray(frame.BufferColor);

                    frame.ColorTimeStamp = sample.color.QueryTimeStamp();
                }
                if (sample.depth != null)
                {
                    sample.depth.CopyToByteArray(frame.BufferDepth);
                    sample.depth.CopyToByteArray(frame.BufferDepthPreview, true);

                    frame.DepthTimeStamp = sample.depth.timeStamp;
                }

#if DEBUG
                if (sample.left != null)
                {
                    sample.left.CopyToByteArray(frame.BufferIRLeft);
                }

                if (sample.right != null)
                {
                    sample.right.CopyToByteArray(frame.BufferIRRight);
                }

                // Get mapping.
                if (sample.depth != null)
                {
                    projection.DepthToCameraCoordinates(sample.depth, frame.DepthToCameraMapping);
                }
#endif
                //frame.LowConfidenceDepthValue = projection.InvalidDepthValue;


                using (Handle<MemoryFrame> frameHandle = new Handle<MemoryFrame>(frame, _pool))
                {
                    FrameArrivedEventArgs eventArgs = new FrameArrivedEventArgs() { FrameHandle = frameHandle };

                    if (this.FrameArrived != null)
                    {
                        this.FrameArrived(this, eventArgs);
                    }
                }
                _senseManager.ReleaseFrame();
                
               //int bitmap_width, bitmap_height;
                //byte[] bitmap_data;

                //PXCMCapture.StreamType[] streams = form.QueryStreams();
                //bool streamState = false;
                //for (int s = 0; s < PXCMCapture.STREAM_LIMIT; s++)
                //{
                //    streamState = streamState || form.GetStreamState(PXCMCapture.StreamTypeFromIndex(s));
                //}
                //if (streamState)
                //{
                //    for (int inx = 0; inx < 2; inx++)
                //        if (sample[streams[inx]] != null)
                //        {
                //            form.SetImage(inx, sample[streams[inx]]);
                //        }
                //}
                //else
                //{
                //    if (form.GetProjectionState() && sample.color != null && sample.depth != null)
                //    {
                //        bitmap_data = projection.DepthToColorCoordinatesByFunction(sample.color, sample.depth, form.GetDotsState(), out bitmap_width, out bitmap_height);
                //        form.SetBitmap(0, bitmap_width, bitmap_height, bitmap_data);
                //    }
                //    if (form.GetUVMapState() && sample.color != null && sample.depth != null)
                //    {
                //        bitmap_data = projection.DepthToColorCoordinatesByUVMAP(sample.color, sample.depth, form.GetDotsState(), out bitmap_width, out bitmap_height);
                //        form.SetBitmap(0, bitmap_width, bitmap_height, bitmap_data);
                //    }
                //    if (form.GetInvUVMapState() && sample.color != null && sample.depth != null)
                //    {
                //        bitmap_data = projection.ColorToDepthCoordinatesByInvUVMap(sample.color, sample.depth, form.GetDotsState(), out bitmap_width, out bitmap_height);
                //        form.SetBitmap(0, bitmap_width, bitmap_height, bitmap_data);
                //    }

                //    if (sample.depth != null)
                //    {
                //        bitmap_data = GetRGB32Pixels(sample.depth, out bitmap_width, out bitmap_height);
                //        form.SetBitmap(1, bitmap_width, bitmap_height, bitmap_data);
                //    }
                //}
                //form.UpdatePanel();

            }
            //projection.Dispose();
        }

        public pxcmStatus InitRecordPlay()
        {
            pxcmStatus status;
            _senseManager.Close();
            PXCMCaptureManager cm = _senseManager.QueryCaptureManager();
            if (Playback||Record)
            {
                status = cm.SetFileName(this.RecordedFile, this.Record);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR )
                {
                    _logger.info("Unable to set file name:" + status.ToString());
                    // return status;
                    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to set file record name");
                }
            }
            PXCMVideoModule.DataDesc desc = new PXCMVideoModule.DataDesc();
            if (cm.QueryCapture() != null && Playback)
            {
                //recordedFile = null;
                cm.SetRealtime(RealTime);
                cm.QueryCapture().QueryDeviceInfo(0, out desc.deviceInfo);
                status = _senseManager.EnableStreams(desc);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    _logger.info("Unable to enable capture stream:" + status.ToString());
                    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable capture stream");
                }
            }  
            else
            {
                status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, Frame.COLOR_RATE);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
                { 
                    _logger.info("Unable to enable color stream:" + status.ToString());
                    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable color stream");
                }
               status = _senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_DEPTH, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_RATE);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    _logger.info("Unable to enable depth stream:" + status.ToString());
                    throw new Smithers.Reading.FrameData.ScannerNotFoundException("Unable to enable depth stream");
                }

                _logger.info("Depth Stream Enabled:" + status.ToString());
            }

            status = _senseManager.EnableScenePerception();
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to enable scene perception:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Scene Perception failed");
            }
            _logger.info("Scene Perception Enabled:" + status.ToString());
            
            _perceptionHandle = _senseManager.QueryScenePerception();

            status = _senseManager.Init();
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                _logger.info("Unable to open sensor in the above mode:" + status.ToString());
                throw new Smithers.Reading.FrameData.ScannerNotFoundException("Init failed");
            }

            _logger.info("Sensor Initialized Successfully:" + status.ToString());
            return status;
        }

        public void Stop()
        {
            _stop = true;
            System.Threading.Thread.Sleep(250); // TODO not like this
        }

        public void Dispose()
        {
            if (!_useDSAPI)
            {
                _senseManager.Close();
                _senseManager.Dispose();
                _senseManager = null;
            }
        }
        public void stopSenseManager()
        {
            if (!_useDSAPI)
            {
                _senseManager.Close();
            }
        }

        public void SetDepthImageExposureAndGain(float exposure, float gain)
        {

#if DSAPI 
            DSAPI.setDepthExposureAndGain(exposure, gain);
#else

            this.Device.SetDSLeftRightAutoExposure(false);
            this.Device.SetDSLeftRightExposure((int)exposure);
            this.Device.SetDSLeftRightGain((int)gain);
#endif
        }

        public void SetColorImageExposureAndGain(float exposure, float gain)
        {

#if DSAPI 
                DSAPI.setColorExposureAndGain(exposure, gain);
#else
            this.Device.SetColorAutoExposure(false);
            this.Device.SetColorExposure((int)exposure);
            this.Device.SetColorGain((int)gain);
#endif
        }

        public void BeginScenePerception()
        {
            if (Playback)
                Record = false;
            _capturing = true;
        }

        public void CaptureEnd()
        {
            _capturing = false;
            ResetScenePerception();
        }

        private void ResetScenePerception()
        {
            _senseManager.PauseScenePerception(true);
            _perceptionPaused = true;
            if(_perceptionHandle!= null)
                _perceptionHandle.Reset(_initPose);
        }
    }
}