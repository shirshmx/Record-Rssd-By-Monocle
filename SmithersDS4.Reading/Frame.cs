using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public static class Frame
    {
        public static readonly int COLOR_WIDTH = 1920;
        public static readonly int COLOR_HEIGHT = 1080;
        public static readonly float COLOR_RATE = 30;
        public static readonly int COLOR_PIXELS = COLOR_WIDTH * COLOR_HEIGHT;

        //public static readonly int DEPTH_WIDTH = 320;
        //public static readonly int DEPTH_HEIGHT = 240;
        public static readonly int DEPTH_WIDTH = 480;
        public static readonly int DEPTH_HEIGHT = 360;
        public static readonly int DEPTH_STRIDE = 480;
        public static readonly float DEPTH_RATE = 30; // Keep these the same, since we're using synchronized frames.
        public static readonly int DEPTH_PIXELS = DEPTH_STRIDE * DEPTH_HEIGHT;

        public static readonly int LEFT_WIDTH = DEPTH_WIDTH;
        public static readonly int LEFT_HEIGHT = DEPTH_HEIGHT;

        public static readonly int RIGHT_WIDTH = DEPTH_WIDTH;
        public static readonly int RIGHT_HEIGHT = DEPTH_HEIGHT;

        public static readonly int CAMERA_POSE_LEN = 12;
    }
}
