using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading.Calibration
{
    public class DS4CalibrationRecordInternal
    {
        public float[] DepthFocalLength { get; set; }
        public float[] ColorFocalLength { get; set; }

        public float[] ColorPrincipalPoint { get; set; }
        public float[] DepthPrincipalPoint { get; set; }

        public  float[] ColorFOV { get; set; }
        public  float[] DepthFOV { get; set; }

        public int ColorWidth { get; set; }
        public int ColorHeight { get; set; }

        public int DepthWidth { get; set; }
        public int DepthHeight { get; set; }

        public int DepthStride { get; set; }

        public ushort LowConfValue { get; set; }

        public float[] Extrinsics { get; set; }
    }

    public class DS4CalibrationRecord
    {
        public DS4CalibrationRecordInternal DeviceCapture { get; set; }

        public string API { get; set; }
    }
}
