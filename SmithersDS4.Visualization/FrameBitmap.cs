using SmithersDS4.Reading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmithersDS4.Visualization
{
    public class LargeFrameBitmap : Smithers.Visualization.FrameBitmap
    {
        public LargeFrameBitmap() : base(Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT) { }
    }

    public class SmallFrameBitmap : Smithers.Visualization.FrameBitmap
    {
        public SmallFrameBitmap() : base(Frame.DEPTH_STRIDE, Frame.DEPTH_HEIGHT) { }
    }

    public class DepthFramePreviewBitmap
    {
        WriteableBitmap _bitmap;
        public DepthFramePreviewBitmap(int width, int height)
        {
            _bitmap = new WriteableBitmap(
                width, 
                height, 
                96.0, 
                96.0, 
                PixelFormats.Rgb24,
                null);
        }

        public WriteableBitmap Bitmap { get { return _bitmap; } }
    }
}
