#if DSAPI
using DSAPIBridge;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading.Calibration
{

    [StructLayout(LayoutKind.Sequential)]
    public struct DS4Calibration
    {

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 9)]
        public double[] lr_rotation;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
		public double[] lr_translation;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public double[] color_translation;
	
        public double non_rect_baseLine;

        [StructLayout(LayoutKind.Sequential)]
        public struct DSCalibIntrinsicsNonRectified
        {
            public float fx;
            public float fy;
            public float px;
            public float py;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 5)]
            public float[] k;

            public Int32 w;
            public Int32 h;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DSCalibIntrinsicsRectified
        {
            public float rfx;
            public float rfy;
            public float rpx;
            public float rpy;
            public Int32 rw;
            public Int32 rh;
        }

        public DSCalibIntrinsicsNonRectified calib_non_rect_left;

        public DSCalibIntrinsicsNonRectified calib_non_rect_right;

        public DSCalibIntrinsicsRectified calib_rect_lr;
        public DSCalibIntrinsicsRectified calib_rect_color;


    }

    public class Calibrator
    {

        public static DS4CalibrationRecord Calibrate(PXCMCapture.Device device)
        {
            PXCMProjection projection = device.CreateProjection();
            /* Get a calibration instance */
            PXCMCalibration calib = projection.QueryInstance<PXCMCalibration>();
            PXCMCalibration.StreamCalibration calibration;
            PXCMCalibration.StreamTransform transformation;
            
            calib.QueryStreamProjectionParameters(PXCMCapture.StreamType.STREAM_TYPE_COLOR, out calibration, out transformation);

            float[] translation = transformation.translation;

            DS4CalibrationRecord record = new DS4CalibrationRecord
            {
                DeviceCapture = new DS4CalibrationRecordInternal
                {
                    ColorFocalLength = device.QueryColorFocalLength().toFloatArray(),
                    DepthFocalLength = device.QueryDepthFocalLength().toFloatArray(),

                    ColorFOV = device.QueryColorFieldOfView().toFloatArray(),
                    DepthFOV = device.QueryDepthFieldOfView().toFloatArray(),

                    ColorPrincipalPoint = device.QueryColorPrincipalPoint().toFloatArray(),
                    DepthPrincipalPoint = device.QueryDepthPrincipalPoint().toFloatArray(),

                    ColorWidth = Frame.COLOR_WIDTH,
                    ColorHeight = Frame.COLOR_HEIGHT,

                    DepthWidth = Frame.DEPTH_WIDTH,
                    DepthHeight = Frame.DEPTH_HEIGHT,
                    DepthStride = Frame.DEPTH_STRIDE,

                    LowConfValue = device.QueryDepthLowConfidenceValue(),

                    Extrinsics = new float[] {  1.0f,
                                            0.0f,
                                            0.0f,
                                            translation[0],
                                            0.0f,
                                            1.0f,
                                            0.0f,
                                            translation[1],
                                            0.0f,
                                            0.0f,
                                            1.0f,
                                            translation[2],
                                            0.0f,
                                            0.0f,
                                            0.0f,
                                            1.0f}

                },

                API = "RSSDK"
            };

            return record;
        }

#if DSAPI
        private static DS4CalibrationRecord Calibrate(DSAPIManaged dsAPI)
        {
            DS4Calibration calibration;

            int iSize = Marshal.SizeOf(typeof(DS4Calibration));
            // Allocate memory (in the Global Heap) for the unmanaged
            // representation of a TestStruct struct.
            IntPtr calibrationPtr = Marshal.AllocHGlobal(iSize);

            // Call the IntPtr version API.
            unsafe
            {
                dsAPI.loadCalibartionInfo(calibrationPtr.ToPointer());
            }
            // Copy the modified contents of the unmanaged representation
            // of test_struct back to the members of the managed test_struct.
            calibration = (DS4Calibration)(Marshal.PtrToStructure(calibrationPtr, typeof(DS4Calibration)));
            // Free the unmanaged representation of test_struct.
            Marshal.FreeHGlobal(calibrationPtr);

            DS4Calibration.DSCalibIntrinsicsRectified calibRectDepth  = calibration.calib_rect_lr;
            DS4Calibration.DSCalibIntrinsicsRectified calibRectColor  = calibration.calib_rect_color;
            
            DS4CalibrationRecord record = new DS4CalibrationRecord
            {
                DeviceCapture = new DS4CalibrationRecordInternal
                {
                    ColorFocalLength = new float[] { calibRectColor.rfx, calibRectColor.rfy },
                    DepthFocalLength = new float[] { calibRectDepth.rfx, calibRectDepth.rfy },

                    ColorPrincipalPoint = new float[] { calibRectColor.rpx, calibRectColor.rpy },
                    DepthPrincipalPoint = new float[] { calibRectDepth.rpx, calibRectDepth.rpy },

                    ColorWidth = Frame.COLOR_WIDTH,
                    ColorHeight = Frame.COLOR_HEIGHT,

                    DepthWidth = Frame.DEPTH_WIDTH,
                    DepthHeight = Frame.DEPTH_HEIGHT,
                    DepthStride = Frame.DEPTH_STRIDE,

                    //LowConfValue = device.QueryDepthLowConfidenceValue(),

                    Extrinsics = new float[] {  1.0f,
                                            0.0f,
                                            0.0f,
                                            (float)calibration.color_translation[0],
                                            0.0f,
                                            1.0f,
                                            0.0f,
                                            (float)calibration.color_translation[1],
                                            0.0f,
                                            0.0f,
                                            1.0f,
                                            (float)calibration.color_translation[2],
                                            0.0f,
                                            0.0f,
                                            0.0f,
                                            1.0f}

                },

                API = "DSAPI"
            };

            return record;
        }
#endif

        public static Task<DS4CalibrationRecord> CalibrateAsync(FrameReader reader)
        {

#if DSAPI
            return Task<DS4CalibrationRecord>.Run(() => Calibrator.Calibrate(reader.DSAPI));
#else

            return Task<DS4CalibrationRecord>.Run(() => Calibrator.Calibrate(reader.Device));
#endif
        }
    }
}
