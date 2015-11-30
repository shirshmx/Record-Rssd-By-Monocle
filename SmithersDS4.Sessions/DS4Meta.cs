using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Sessions
{
    public class DS4SavedItem : Smithers.Sessions.SavedItem
    {

        public DS4SavedItem()
            : base()
        {
        }

        public float[] CameraPose { get; set; }

        public string TrackingAccuracy { get; set; }

        public string VoxelResolution { get; set; }

        public long ColorTimeStamp { get; set; }

        public long DepthTimeStamp { get; set; }

        public struct AccelerometerStats
        {
            public double accX;
            public double accY;
            public double accZ;
            public long timestamp;
        }
        public AccelerometerStats acceleroStats { get; set; }
    }

    public class DS4Meta
    {

        public class DS4DeviceConfig
        {
            public int BackLightCompensation { get; set; }

            public int Brightness { get; set; }

            public int Contrast { get; set; }

            public int ColorGain { get; set; }

            public float ColorExposure { get; set; }

            public float DepthGain { get; set; }

            public float DepthExposure { get; set; }

            public float minDepthRange { get; set; }

            public float maxDepthRange { get; set; }

            public ushort confidenceThreshold { get; set; }

            public bool colorAutoExposure { get; set; }

            public bool autoPowerline { get; set; }

            public bool depthAutoExposure { get; set; }
        }

        public string Name {get; set;}

        public DS4DeviceConfig DeviceConfig;
        public string CaptureMode;

        public float Height { get; set; }

        public float Weight { get; set; }

        public string Gender { get; set; }

        public List<List<DS4SavedItem>> FrameItems { get; set; }

        
    }
}
