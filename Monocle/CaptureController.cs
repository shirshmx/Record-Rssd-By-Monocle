using SmithersDS4.Reading;
using Smithers.Sessions;
using Smithers.Visualization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmithersDS4.Sessions;
using Monocle.Properties;

namespace Monocle
{
    public class CaptureController
    {

        ObjectPool<MemoryFrame> _pool = new ObjectPool<MemoryFrame>(150);
        FrameReader _reader;
        FrameRateReporter _frameRateReporter = new FrameRateReporter();
        BitmapBuilder _bb = new BitmapBuilder();

        string _baseDirectory;
        DS4Session _session;

        SmithersDS4.Sessions.SessionManager<DS4Session, DS4Meta, Shot<ShotDefinition, DS4SavedItem>, ShotDefinition, DS4SavedItem> _sessionManager;
        private SmithersLogger _logger;


        public SmithersLogger Logger { get { return _logger; } }
        public FrameReader FrameReader { get { return _reader; } }

        public FrameRateReporter FrameRateReporter { get { return _frameRateReporter; } }
        public SmithersDS4.Sessions.SessionManager<DS4Session, DS4Meta, Shot<ShotDefinition, DS4SavedItem>, ShotDefinition, DS4SavedItem> SessionManager { get { return _sessionManager; } }
        public DS4Session Session { get { return _session; } }

        public CaptureMode CaptureMode { get; set; }

        public CaptureController(string baseDirectory, SmithersLogger logger)
        {
            _baseDirectory = baseDirectory;
            _logger = logger;

            try
            {
                _reader = new FrameReader(_pool, logger);
            }
            catch (Smithers.Reading.FrameData.ScannerNotFoundException e)
            {
                _logger.info("Failed to initizale camera" + e.Message);
                throw e;
            }
            StartNewSession();
            _sessionManager = new SmithersDS4.Sessions.SessionManager<DS4Session, DS4Meta, Shot<ShotDefinition, DS4SavedItem>, ShotDefinition, DS4SavedItem>(_session);
            this.CaptureMode = CaptureMode.Sweeping;

            _sessionManager.AttachToReader(_reader);
            //_reader.FrameArrived += _sessionManager.FrameArrived;
            _reader.FrameArrived += _frameRateReporter.FrameArrived;
            //_reader.StartCapture += StartCapture;

            try
            {
                _reader.StartAsync();
            }
            catch (Smithers.Reading.FrameData.ScannerNotFoundException e)
            {
                _logger.info(e.Message);
            }
        }

        public void StartCapture(object sender,EventArgs ea)
        {
            // Instead of using a predefined capture program, we add shots on the fly
            if (this.CaptureMode == CaptureMode.Stream || this.CaptureMode == CaptureMode.Sweeping)
            {
                ShotDefinition videoShot = new DS4VideoShotDefintion();

                _session.AddShot(videoShot);
            }
            else
            {
                _session.AddShot(ShotDefinition.DEFAULT);
            }

            _sessionManager.PrepareForNextShot();

            if (this.CaptureMode == CaptureMode.Sweeping)
            {
                _logger.info("Sweeping Start...");
                try
                {
                    _sessionManager.SweepingStart();
                }
                catch (Exception e)
                {
                    _logger.info("Unexpected exception: " + e.Message);
                    throw e;
                }
            }
            else{
                _sessionManager.CaptureShot(this.CaptureMode);
            }

        }

        public void StopCapture()
        {
            //if (this.CaptureMode != CaptureMode.Stream)
            //{
            //    throw new InvalidOperationException("Stop Capture is only applicable to Stream capture mode");
            //}
            if (_reader.Record)
            {
                _logger.info("Sweeping Finished");
            }
            //-------------in a comment to record only rssdk file

            /*if (this.CaptureMode == CaptureMode.Sweeping)
            {
                _sessionManager.SweepingEnd();
                _reader.CaptureEnd();
                _logger.info("Sweeping Finished");
            }*/
            else
            {
               // _reader.Stop();
            }
        }

        public void StartNewSession(string name="")
        {
            string guid = Guid.NewGuid().ToString();
            string sessionFolderName = name !="" ? String.Concat(name, "-", guid): guid;
            string path = Path.Combine(_baseDirectory, sessionFolderName);
            _session = new DS4Session(path, Enumerable.Empty<ShotDefinition>());
            //FIXEME: Load device config here
            _session.Metadata = new DS4Meta
            {
                DeviceConfig = new DS4Meta.DS4DeviceConfig { },
                CaptureMode = this.CaptureMode.ToString().ToLower(),
                FrameItems = new List<List<DS4SavedItem>>()
            };

            if (_sessionManager != null)
            {
                _sessionManager.Reset(_session);
            }
        }

        public ProjectionMode ProjectionMode { get; set; }

        public SkeletonPresenter SkeletonPresenter { get; set; }

    }
}
