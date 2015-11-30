using Microsoft.Kinect;
using Monocle.Properties;
using SmithersDS4.Reading;
using Smithers.Sessions.Archiving;
using SmithersDS4.Visualization;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Smithers.Client;

namespace Monocle
{
     public class CollectData
    {
        public SmithersDS4.Sessions.DS4Meta shortMetadata { get; set; }
        public SmithersDS4.Reading.Calibration.DS4CalibrationRecord calibration { get; set; }

 //       public Task<SmithersDS4.Reading.Calibration.DS4CalibrationRecord> RecordData;
    }
    public static class InkInputHelper
    {
        public static void DisableWPFTabletSupport()
        {
            // Get a collection of the tablet devices for this window.  
            TabletDeviceCollection devices = System.Windows.Input.Tablet.TabletDevices;
            if (devices.Count > 0)
            {
                // Get the Type of InputManager.
                Type inputManagerType = typeof(System.Windows.Input.InputManager);

                // Call the StylusLogic method on the InputManager.Current instance.
                object stylusLogic = inputManagerType.InvokeMember("StylusLogic",
                            BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                            null, InputManager.Current, null);

                if (stylusLogic != null)
                {
                    //  Get the type of the stylusLogic returned from the call to StylusLogic.
                    Type stylusLogicType = stylusLogic.GetType();

                    // Remove the first tablet device in the devices collection.
                    stylusLogicType.InvokeMember("OnTabletRemoved",
                            BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                            null, stylusLogic, new object[] { (uint)0 });
                }
            }
        }
    }

    [Guid("41C81592-514C-48BD-A22E-E6AF638521A6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInputPanelConfiguration
    {
        /// <summary>
        /// Enables a client process to opt-in to the focus tracking mechanism for Windows Store apps that controls the invoking and dismissing semantics of the touch keyboard.
        /// </summary>
        /// <returns>If this method succeeds, it returns S_OK. Otherwise, it returns an HRESULT error code.</returns>
        int EnableFocusTracking();
    }
    [ComImport, Guid("2853ADD3-F096-4C63-A78F-7FA3EA837FB7")]
    class InputPanelConfiguration
    {
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ApplicationController _appController;
        CaptureController _captureController;
        CameraImagePresenter _cameraImagePresenter;
        //private ProjectionMode _projectionMode;
        private Storyboard _flashAttack;
        private Storyboard _flashDecay;
        private string _gender;
        string baseDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Body Labs", "Monocle");

        private SolidColorBrush buttonBg;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            InkInputHelper.DisableWPFTabletSupport();


            _appController = new ApplicationController(baseDirectory);
            _captureController = _appController.CaptureController;

            if (Settings.Default.accessKey == "")
            {
                MessageBox.Show("Please setup keys in the credentials.json in the " + baseDirectory + " directory",
                                "Missing Credentials", MessageBoxButton.OK);
                Environment.Exit(0);
            }

            KinectSensor.GetDefault().Open();

            _cameraImagePresenter = new SmithersDS4.Visualization.CameraImagePresenter(camera, Settings.Default.useDSAPI);
            _cameraImagePresenter.CameraMode = SmithersDS4.Visualization.CameraMode.Depth;
            _cameraImagePresenter.Enabled = true;
            //camera.Source = _captureController.ColorBitmap.Bitmap;

            _captureController.SessionManager.ShotBeginning += (sender, e) =>
            {
                if (_captureController.CaptureMode == SmithersDS4.Sessions.CaptureMode.Trigger)
                {
                    _flashAttack.Begin();
                    _cameraImagePresenter.Enabled = false;
                }
                else
                {
                    _cameraImagePresenter.Enabled = true;
                }

            };

            _captureController.SessionManager.ShotCompletedSuccess += (sender, e) =>
            {
                _cameraImagePresenter.Enabled = true;
            };

            _captureController.SessionManager.ShotCompletedError += (sender, e) =>
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    _flashDecay.Begin();
                    MessageBox.Show(e.ErrorMessage);
                    _cameraImagePresenter.Enabled = true;
                });
            };

            _captureController.SessionManager.FrameCaptured += (sender, e) =>
            {

                this.Dispatcher.InvokeAsync(() =>
                {
                    _flashDecay.Begin();

                    int numFramesInMemory = _captureController.SessionManager.NumFrameInMemory;

                    lblCaptureCount.Content = "Captured Frames: " + numFramesInMemory;
                    tbCapturedSweeps.Text = _captureController.Session.SweepCounter.ToString();
                    tbCapturedFrames.Text = numFramesInMemory.ToString();

                    var sensorProperty = _captureController.Session.Metadata.DeviceConfig;

                    inputImageExposure.Text = sensorProperty.DepthExposure.ToString();
                    inputImageGain.Text = sensorProperty.DepthGain.ToString();

                    colorImageExposure.Text = sensorProperty.ColorExposure.ToString();
                    colorImageGain.Text = sensorProperty.ColorGain.ToString();
                });
            };
            
            
            _captureController.SessionManager.ShotSavedSuccess += (sender, e) =>
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    SetBtnCaptureContent("Start Recording");
                    btnCapture.IsEnabled = true;
                    btnEndSessionAndUpload.IsEnabled = true;
                    btnEndAndStartNewSession.IsEnabled = true;

                    BtnRedoSweep.Visibility = Visibility.Visible;

                });
            };

            _captureController.SessionManager.ShotSavedError += (sender, e) =>
            {
                this.Dispatcher.InvokeAsync(() =>
                {
                    _flashDecay.Begin();
                    if (e.Exception == null)
                        MessageBox.Show(e.ErrorMessage);
                    else
                        MessageBox.Show(e.ErrorMessage + ": " + e.Exception.Message);
                });
            };

            _appController.UploadManager.UploadFinished += UploadManager_UploadFinished;

            //_captureController.SkeletonPresenter = new SkeletonPresenter(canvas);
            //_captureController.SkeletonPresenter.ShowBody = true;
            //_captureController.SkeletonPresenter.ShowHands = true;
            ////            _captureController.FrameReader.AddResponder(_captureController.SkeletonPresenter);
            //_captureController.SkeletonPresenter.ProjectionMode = ProjectionMode.COLOR_IMAGE;
            //_captureController.SkeletonPresenter.CoordinateMapper = KinectSensor.GetDefault().CoordinateMapper;
            //_captureController.SkeletonPresenter.Underlay = camera;

            //            _captureController.FrameReader.AddResponder(_cameraImagePresenter);

            _captureController.FrameReader.FrameArrived += _cameraImagePresenter.FrameArrived;

            _captureController.FrameRateReporter.FpsChanged += this.FpsChanged;

            _flashAttack = FindResource("FlashAttack") as Storyboard;
            _flashDecay = FindResource("FlashDecay") as Storyboard;

            buttonBg = (SolidColorBrush)(new BrushConverter().ConvertFrom("#3cdc3c"));
        }

        void UploadManager_UploadFinished(object sender, UploadResult e)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                if (e.Success)
                {
                    _captureController.Logger.info("Upload Finished: " + _captureController.Session.CompressedScanFile);
                    _endAndStartNewSession();
                }
            });
        }

        void _endAndStartNewSession()
        {
            _captureController.StartNewSession();
            _captureController.FrameReader.RecordNumber = 0;
            _cameraImagePresenter.Enabled = true;

            spInputScan.Visibility = Visibility.Visible;
            btnEndSessionAndUpload.Visibility = Visibility.Collapsed;
            btnEndAndStartNewSession.Visibility = Visibility.Collapsed;
            btnStartNew.Visibility = Visibility.Visible;
            BtnRedoSweep.Visibility = Visibility.Collapsed;
            spSessionInfo.Visibility = Visibility.Collapsed;
            spScanOperation.Visibility = Visibility.Collapsed;

            BtnRedoSweep.IsEnabled = true;

            SetBtnCaptureContent("Start a new session to begin");

            btnCapture.Background = Brushes.LightGreen;
            btnCapture.IsEnabled = false;

            tbCapturedSweeps.Text = "0";
            tbCapturedFrames.Text = "--";
            inputName.Text = "";
            inputWeight.Text = "";
            inputHeight.Text = "";
            rbMale.IsChecked = false;
            rbFemale.IsChecked = false;
            btnStartNew.FontSize = 18;
            btnStartNew.Content = "Select Gender to start";
            btnStartNew.IsEnabled = false;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
 
            //Windows 8 API to enable touch keyboard to monitor for focus tracking in this WPF application
            InputPanelConfiguration cp = new InputPanelConfiguration();
            IInputPanelConfiguration icp = cp as IInputPanelConfiguration;
            if (icp != null)
                icp.EnableFocusTracking();
    
        }

        private void btnCapture_Click(object sender, RoutedEventArgs e)
        {
            bool isSweeping = _captureController.SessionManager.isSweeping;
            this.Dispatcher.Invoke(() =>
                {
                    btnEndSessionAndUpload.IsEnabled = true;
                    btnEndAndStartNewSession.IsEnabled = true;

                    SetBtnCaptureContent("Stop");

                    try
                    {
                        if (_captureController.FrameReader.Record)
                        {
                            SetBtnCaptureContent("Saving Sweep...");
                            btnCapture.IsEnabled = false;
                            btnEndAndStartNewSession.IsEnabled = false;
                            btnEndSessionAndUpload.IsEnabled = false;
                            
                            _captureController.FrameReader.RecordedFile = null;
                            _captureController.FrameReader.Record = false;
                            _captureController.FrameReader.Playback = false;
                            
                             _captureController.FrameReader.InitRecordPlay();
                            StopCapture();
                        }
                        else
                        {
                            if (_captureController.FrameReader.RecordedFile != null)
                                _captureController.FrameReader.Playback = true;
                            else
                            {
                                if (_captureController.FrameReader.RecordNumber == 0)
                                {
                                    Directory.CreateDirectory(baseDirectory + "\\" + inputName.Text + "-rssdk");
                                    _captureController.FrameReader.FolderPath = baseDirectory + "\\" + inputName.Text + "-rssdk";
                                    string CALIBRATION_FILE = "Calibration.json";
                                    string calibrationCopyPath = System.IO.Path.Combine(_captureController.FrameReader.FolderPath, CALIBRATION_FILE);
                                    CollectData dataAndCalibration = new CollectData();
                                    dataAndCalibration.calibration = SmithersDS4.Reading.Calibration.Calibrator.Calibrate(_captureController.FrameReader.Device);
                                    dataAndCalibration.shortMetadata = new SmithersDS4.Sessions.DS4Meta();
                                    dataAndCalibration.shortMetadata.Name = _captureController.Session.Metadata.Name;
                                    dataAndCalibration.shortMetadata.Gender = _captureController.Session.Metadata.Gender;
                                    dataAndCalibration.shortMetadata.Height = _captureController.Session.Metadata.Height;
                                    dataAndCalibration.shortMetadata.Weight = _captureController.Session.Metadata.Weight;
                                    Smithers.Sessions.JSONHelper.Instance.Serialize(dataAndCalibration, calibrationCopyPath);
                                }
                                string fileName = string.Format("sweep_{0:D2}.rssdk", ++_captureController.FrameReader.RecordNumber);
                                _captureController.FrameReader.RecordedFile = System.IO.Path.Combine(_captureController.FrameReader.FolderPath, fileName);
                                _captureController.FrameReader.Record = true;
                            }

                            _captureController.FrameReader.InitRecordPlay();
                            StartCapture();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                });
        }

        private void ToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            string modeString = (sender as ToggleButton).Tag.ToString();
            SmithersDS4.Visualization.CameraMode cameraMode = (SmithersDS4.Visualization.CameraMode)Enum.Parse(typeof(SmithersDS4.Visualization.CameraMode), modeString);
            
            foreach (var child in spCamera.Children)
            {
                if (child is ToggleButton)
                {
                    (child as ToggleButton).IsChecked = child == sender;
                }
            }
            //if (cameraMode == SmithersDS4.Visualization.CameraMode.Color || cameraMode == SmithersDS4.Visualization.CameraMode.ColorDepth)
            //    _projectionMode = ProjectionMode.COLOR_IMAGE;
            //else
            //    _projectionMode = ProjectionMode.DEPTH_IMAGE;
            //_captureController.SkeletonPresenter.ProjectionMode = _projectionMode;

            _cameraImagePresenter.CameraMode = cameraMode;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _captureController.FrameReader.Stop();
            _captureController.FrameReader.Dispose();

            Environment.Exit(0);
        }

        private async Task DoUpload()
        {
            Uploader uploader = (Uploader)_captureController.Session.Uploader;
            if (uploader != null && uploader.InProgress)
            {
                _appController.TryCancelUpload();
                
                this.Dispatcher.Invoke(() =>
                {
                    btnEndAndStartNewSession.IsEnabled = true;
                });
            }
            else if (! _captureController.Session.Compressing)
            {
                _captureController.Logger.info("Start Uploading: " + _captureController.Session.CompressedScanFile);
                _cameraImagePresenter.Enabled = false;
                await _appController.CompressAndUploadAndStartNewSession();
            }
        }

        private void FpsChanged(object sender, FpsChangedEventArgs ea)
        {
           
                this.Dispatcher.Invoke(() =>
                    {
                        lblFrameRate.Content = String.Format("Frame Rate: {0}", Math.Round(ea.Fps));

                        string accuracy = _captureController.FrameReader.TrackingAccuracy;

                        if (!_captureController.SessionManager.isSweeping)
                        {
                            if (accuracy == "HIGH")
                            {
                                lblSceneTrackingAccuracy.Content = accuracy;
                                lblSceneTrackingAccuracy.Foreground = Brushes.Green;
                            }
                            else if (accuracy == "MID")
                            {
                                lblSceneTrackingAccuracy.Content = accuracy;
                                lblSceneTrackingAccuracy.Foreground = Brushes.YellowGreen;
                            }
                            else
                            {
                                lblSceneTrackingAccuracy.Content = "RESETING";
                                lblSceneTrackingAccuracy.Foreground = Brushes.Red;
                            }
                        }
                    });
        }

        private void ToggleCaptureMode_Click(object sender, RoutedEventArgs e)
        {
            string modeString = (sender as ToggleButton).Tag.ToString();
            
            if (modeString == "Trigger") {
                 SetBtnCaptureContent("Start");
                _captureController.CaptureMode = SmithersDS4.Sessions.CaptureMode.Trigger;
            }
            else if (modeString == "Stream")
            {
                SetBtnCaptureContent("Start");
                _captureController.CaptureMode = SmithersDS4.Sessions.CaptureMode.Stream;
            }
            else if (modeString == "Sweep")
            {
                SetBtnCaptureContent("Start");
                btnCapture.Background = Brushes.Green;
                _captureController.CaptureMode = SmithersDS4.Sessions.CaptureMode.Sweeping;
            }
        }
       
        private void StartCapture()
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                
                SetBtnCaptureContent("stop");
                btnCapture.Background = Brushes.Red;
                spScanOperation.Visibility = Visibility.Visible;
                BtnRedoSweep.Visibility = Visibility.Collapsed;
                spCaptureStats.Visibility = Visibility.Visible;
                btnEndSessionAndUpload.IsEnabled = false;
                btnEndAndStartNewSession.IsEnabled = false;
                });
        }
        private void StopCapture()
        {
            
            SetBtnCaptureContent("Start Recording");
            btnCapture.IsEnabled = true;
            btnEndSessionAndUpload.IsEnabled = true;
            btnEndAndStartNewSession.IsEnabled = true;

            BtnRedoSweep.Visibility = Visibility.Visible;
            
            _captureController.StopCapture();
            btnCapture.Background = buttonBg;
        }

        private void TogglePortraitMode_Click(object sender, RoutedEventArgs e)
        {
            bool InPortraitMode = (sender as ToggleButton).IsChecked.Value;

            if (InPortraitMode)
            {
                //Apply transformation
                camera.LayoutTransform = new RotateTransform(-90, 0, 0);
            }
            else
            {
                camera.LayoutTransform = new RotateTransform(0, 0, 0); 
            }

            (sender as ToggleButton).IsChecked = InPortraitMode;
        }

        private void SaveNewInfoAndStartSession(object sender, RoutedEventArgs e)
        {
            string name = inputName.Text;

            string height = inputHeight.Text;
            string weight = inputWeight.Text;
            string gender = _gender;
            //try
            //{
            //    _captureController.FrameReader.StartAsync();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //    //_logger.info(e.Message);
            //}

            _captureController.Logger.info("Starting new session");
            _captureController.StartNewSession(name);

            _captureController.Session.Metadata.Name = name;
            _captureController.Session.Metadata.Gender = gender;

            float parsedHeight, parsedWeight;

            if (float.TryParse(height, out parsedHeight))
            {
                _captureController.Session.Metadata.Height = parsedHeight;
            }
            if(float.TryParse(weight, out parsedWeight))
            {
                _captureController.Session.Metadata.Weight = float.Parse(weight);
            }

            spInputScan.Visibility = Visibility.Collapsed;
            btnStartNew.Visibility = Visibility.Collapsed;
            btnEndSessionAndUpload.Visibility = Visibility.Visible;
            btnEndAndStartNewSession.Visibility = Visibility.Visible;
            spSessionInfo.Visibility = Visibility.Visible;
            spUploadPanel.Visibility = Visibility.Collapsed;

            spSessionInfo.DataContext = _captureController.Session;

            TextBlock btnCaption = btnCapture.Content as TextBlock;

            btnCaption.Text = "Start Recording";

            btnCapture.Background = buttonBg;
            btnCapture.IsEnabled = true;
            btnEndSessionAndUpload.IsEnabled = false;
            btnEndAndStartNewSession.IsEnabled = false;
            
        }

        private void Gender_Checked(object sender, RoutedEventArgs e)
        {
            _gender = rbMale.IsChecked == true ? "Male" : "Female";
            btnStartNew.FontSize = 25;
            btnStartNew.Content = "New Session";
            btnStartNew.IsEnabled = true;
        }

        private void inputImageParameterKeyDown(object sender, KeyEventArgs e)
        {
            float expsoure, gain;
            if (e.Key == Key.Return)
            {
                string inputExposure = inputImageExposure.Text;
                string inputGain = inputImageGain.Text;

                try
                {
                    expsoure = inputExposure != "" ? float.Parse(inputExposure) : 33.3f;
                    gain = inputGain != "" ? float.Parse(inputGain) : 1.0f;

                    _captureController.FrameReader.SetDepthImageExposureAndGain(expsoure, gain);
                    _captureController.Session.Metadata.DeviceConfig.DepthExposure = expsoure;
                    _captureController.Session.Metadata.DeviceConfig.DepthGain = gain;

                }
                catch (Exception)
                {

                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = _captureController.FrameReader.FolderPath;
                Process.Start(@path);
            }
            catch (Exception)
            {
                MessageBox.Show("Capture Not Started yet.");
            }
        }

        private void colorImageParameterKeyDown(object sender, KeyEventArgs e)
        {
            float expsoure, gain;
            if (e.Key == Key.Return)
            {
                string inputExposure = colorImageExposure.Text;
                string inputGain = colorImageGain.Text;

                try
                {
                    expsoure = inputExposure != "" ? float.Parse(inputExposure) : 15.62f;
                    gain = inputGain != "" ? float.Parse(inputGain) : 32.0f;

                    _captureController.FrameReader.SetColorImageExposureAndGain(expsoure, gain);
                }
                catch (Exception)
                {

                }
            }
        }

        private void SetBtnCaptureContent(string caption)
        {
            TextBlock btnCaption = btnCapture.Content as TextBlock;

            btnCaption.Text = caption;
        }

        private async void EndCurrentSessionAndUpload(object sender, RoutedEventArgs e)
        {
            spUploadPanel.Visibility = Visibility.Visible;
            spUploadPanel.DataContext = _captureController.Session;
            btnEndSessionAndUpload.DataContext = _captureController.Session;
            btnEndAndStartNewSession.DataContext = _captureController.Session;
            btnCapture.IsEnabled = false;
            BtnRedoSweep.IsEnabled = false;

            btnEndAndStartNewSession.IsEnabled = false;
            
            await DoUpload();

        }

        private void DeleteAndRedoLastSweep(object sender,  RoutedEventArgs e)
        {
            //remove rssdk file
            if (_captureController.FrameReader.RecordNumber > 1)
            {
                File.Delete(_captureController.FrameReader.FolderPath+ "\\sweep_0" + _captureController.FrameReader.RecordNumber+"rssdk");
                _captureController.FrameReader.RecordNumber--;
            }
            int lastSweep = _captureController.Session.SweepCounter;

            btnCapture.IsEnabled = false;

            //_captureController.SessionManager.DeleteSweep(lastSweep);
            tbCapturedSweeps.Text = _captureController.Session.SweepCounter.ToString();
            tbCapturedFrames.Text = "0";

            BtnRedoSweep.Visibility = Visibility.Collapsed;
            btnCapture.IsEnabled = true;
        }

        private void EndAndStartNewSession(object sender, RoutedEventArgs e)
        {
            _endAndStartNewSession();
        }

        // if want to use playback option, need to visible the ptnPlay button
        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();           
            ofd.Filter = "RSSDK clip|*.rssdk|Old format clip|*.pcsdk|All files|*.*";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;

            _captureController.FrameReader.RecordedFile = (ofd.ShowDialog() == true) ? ofd.FileName : null;
            if (_captureController.FrameReader.RecordedFile != null)
            {
                cbxRealTime.Visibility = System.Windows.Visibility.Visible;
            }
        }
        // enable\disable realtime option in playback mode
        private void cbxRealTime_Checked(object sender, RoutedEventArgs e)
        {
            _captureController.FrameReader.RealTime = true;
        }

        private void cbxRealTime_Unchecked(object sender, RoutedEventArgs e)
        {
            _captureController.FrameReader.RealTime = false;
        }

    }
}
