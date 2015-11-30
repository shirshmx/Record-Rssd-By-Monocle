using Smithers.Sessions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Sessions
{

    [StructLayout(LayoutKind.Sequential)]
    public struct DS4SensorProperty
    {
        public bool colorAutoExposure;
        public float colorExposure;
        public float colorGain;
        public float depthExposure;
        public float depthGain;
    }

    public static class DS4DeviceExtension
    {
        public static void WriteDeviceConfig<T>(this PXCMCapture.Device device, T metaData, CaptureMode captureMode) where T : DS4Meta
        {
            int backLightCompensation = device.QueryColorBackLightCompensation();
            int brightness = device.QueryColorBrightness();
            int contrast = device.QueryColorContrast();

            PXCMRangeF32 depthRange = device.QueryDepthSensorRange();
            float minDepthRange = depthRange.min;
            float maxDepthRange = depthRange.max;

            ushort confidenceThreshold = device.QueryDepthConfidenceThreshold();

            bool autoExposure = device.QueryColorAutoExposure();
            bool autoPowerline = device.QueryColorAutoPowerLineFrequency();

            int colorExposure = device.QueryColorExposure();
            int colorGain = device.QueryColorGain();

            float depthExposure = device.QueryDSLeftRightExposure();
            int depthGain = device.QueryDSLeftRightGain();

            var depthExposureInfo = device.QueryDSLeftRightExposureInfo();
            bool depthAutoExposure = depthExposureInfo.automatic;
  
            metaData.DeviceConfig.BackLightCompensation = backLightCompensation;
            metaData.DeviceConfig.minDepthRange = minDepthRange;
            metaData.DeviceConfig.maxDepthRange = maxDepthRange;
            metaData.DeviceConfig.colorAutoExposure = autoExposure;
            metaData.DeviceConfig.depthAutoExposure = depthAutoExposure;

            metaData.DeviceConfig.ColorExposure = (float)colorExposure;
            metaData.DeviceConfig.ColorGain = colorGain;

            if (metaData.DeviceConfig.DepthExposure == 0)
            {
                metaData.DeviceConfig.DepthExposure = 33.3f;
                metaData.DeviceConfig.DepthGain = 1;
            }
                //{
                //    BackLightCompensation = backLightCompensation,
                //    Brightness = brightness,
                //    Contrast = contrast,
                //    minDepthRange = minDepthRange,
                //    maxDepthRange = maxDepthRange,
                //    confidenceThreshold = confidenceThreshold,
                //    autoPowerline = autoPowerline,

                //    colorAutoExposure = autoExposure,
                //    depthAutoExposure = depthAutoExposure,

                //    ColorExposure = (float)colorExposure,
                //    ColorGain = colorGain,
                //    DepthExposure = depthExposure,
                //    DepthGain = depthGain
                //};

            metaData.CaptureMode = captureMode.ToString().ToLower();
        }

        public static void SetDeviceConfig(this PXCMCapture.Device device)
        {

            string deviceConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Body Labs", "DS4-Monocle", "config.json");

            DS4Meta metadataDS4 = JSONHelper.Instance.DeserializeObject<DS4Meta>(deviceConfigPath);
            DS4Meta.DS4DeviceConfig deviceConfig = metadataDS4.DeviceConfig;

            if (deviceConfig.Contrast != 0)
            {
                device.SetColorBackLightCompensation(deviceConfig.BackLightCompensation);
                device.SetColorAutoExposure(deviceConfig.colorAutoExposure);
                device.SetColorContrast(deviceConfig.Contrast);
                device.SetColorGain(deviceConfig.ColorGain);
                device.SetDepthConfidenceThreshold(deviceConfig.confidenceThreshold);
            }


        }

#if DSAPI
        public static void WriteDeviceConfig<T>(this DSAPIBridge.DSAPIManaged dsAPI, T metaData, CaptureMode captureMode) where T : DS4Meta
        {

            DS4SensorProperty sensorProperty;

            int iSize = Marshal.SizeOf(typeof(DS4SensorProperty));

            IntPtr sensorPropertyPtr = Marshal.AllocHGlobal(iSize);

            // Call the IntPtr version API.
            unsafe
            {
                dsAPI.getSensorProperty(sensorPropertyPtr.ToPointer());
            }

            sensorProperty = (DS4SensorProperty)(Marshal.PtrToStructure(sensorPropertyPtr, typeof(DS4SensorProperty)));

            // Free the unmanaged representation of test_struct.
            Marshal.FreeHGlobal(sensorPropertyPtr);

            metaData.DeviceConfig = new DS4Meta.DS4DeviceConfig 
            {
                ColorGain = (int)sensorProperty.colorGain,
                ColorExposure = sensorProperty.colorExposure,
                colorAutoExposure = sensorProperty.colorAutoExposure,

                DepthGain = sensorProperty.depthGain,
                DepthExposure = sensorProperty.depthExposure,

            };

            metaData.CaptureMode = captureMode.ToString().ToLower();

        }
#endif
    }
}
