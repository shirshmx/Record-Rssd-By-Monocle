using SmithersDS4.Reading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmithersDS4.Visualization
{
    public enum CameraMode
    {
        Color,
        Depth
    }

    public class CameraImagePresenter
    {
        public Image CameraPrimary { get; private set; }
        public CameraMode CameraMode { get; set; }
        public bool Enabled { get; set; }

        LargeFrameBitmap _colorBitmap = new LargeFrameBitmap();
        SmallFrameBitmap _depthBitmap = new SmallFrameBitmap();

        DepthFramePreviewBitmap _depthPreviewBitmap = new DepthFramePreviewBitmap(SmithersDS4.Reading.Frame.DEPTH_STRIDE,
                                                                                  SmithersDS4.Reading.Frame.DEPTH_HEIGHT);
        private bool _use_DSAPI;


        public CameraImagePresenter(Image cameraPrimary, bool use_DSAPI = false)
        {
            this.CameraPrimary = cameraPrimary;

            this.Enabled = true;

            _use_DSAPI = use_DSAPI;

            EnsureImageSource(null);
        }

        private void EnsureImageSource(ImageSource primary)
        {
            ImageSource primarySource = primary == null ? null : primary;

            if (this.CameraPrimary.Source != primarySource)
                this.CameraPrimary.Source = primarySource;
        }

        public void FrameArrived(object sender, FrameArrivedEventArgs e)
        {
            if (!this.Enabled) return;

            byte[] bytes;
            WriteableBitmap bitmap;

            Handle<MemoryFrame> frameHandle = e.FrameHandle.Clone();

            switch (this.CameraMode)
            {
                case CameraMode.Color:
                    bytes = frameHandle.Item.BufferColor;
                    bitmap = _colorBitmap.Bitmap;
                    break;
                case CameraMode.Depth:
                    bytes = frameHandle.Item.BufferDepthPreview;
                    if (_use_DSAPI)
                        bitmap = _depthPreviewBitmap.Bitmap;
                    else
                        bitmap = _depthBitmap.Bitmap;
                    break;
                default:
                    throw new InvalidOperationException("Shouldn't get here"); // Avoid unassigned variable error
            }

            CameraPrimary.Dispatcher.InvokeAsync(() =>
            {
                bitmap.Lock();

                Int32Rect rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

                bitmap.WritePixels(rect, bytes, bitmap.PixelWidth * ((this.CameraMode == CameraMode.Depth && _use_DSAPI) ? 3:4), 0);

                bitmap.AddDirtyRect(rect);
                bitmap.Unlock();

                EnsureImageSource(bitmap);

                frameHandle.Dispose();
            });
        }
    }
}
