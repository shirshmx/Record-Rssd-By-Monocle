using Smithers.Reading.FrameData;
using Smithers.Reading.FrameData.Extensions;
using Smithers.Serialization.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmithersDS4.Reading
{
    public class FrameSerializer
    {
        public static readonly PixelFormat COLOR_PIXEL_FORMAT_WPF = PixelFormats.Bgr32;
        public static readonly byte COLOR_BYTES_PER_PIXEL = (byte)((COLOR_PIXEL_FORMAT_WPF.BitsPerPixel + 7) / 8);

        public static readonly PixelFormat DEPTH_PIXEL_FORMAT_WPF = PixelFormats.Bgr32;
        public static readonly byte DEPTH_BYTES_PER_PIXEL = 2;


        private static BitmapSource BufferCaptureBitmapHelper(Array data, int width, int height, int bytesPerPixel, byte[] outBuffer, int stride)
        {
            long bytes = bytesPerPixel * width * height;

            if (outBuffer.Length < bytes)
                throw new ArgumentException(string.Format("Buffer is too short, at least {0} needed", bytes));

            if (bytes > int.MaxValue)
                throw new ArgumentException(string.Format("FIXME, Buffer.BlockCopy doesn't handle blocks longer than {0}", int.MaxValue));

            System.Buffer.BlockCopy(data, 0, outBuffer, 0, (int)bytes);

            return BitmapSource.Create(
                width,
                height,
                96,
                96,
                bytesPerPixel == 1 ? PixelFormats.Gray8 : PixelFormats.Gray16,
                bytesPerPixel == 1 ? BitmapPalettes.Gray16 : BitmapPalettes.Gray16,
                outBuffer,
                stride * bytesPerPixel
            );
        }

        public static BitmapSource CreateColorBitmap(byte[] buffer, int width, int height)
        {
            long bytes = width * height * COLOR_BYTES_PER_PIXEL;

            if (buffer.Length != bytes)
                throw new ArgumentException(string.Format("Buffer is incorrect length, expected {0}", bytes));

            return BitmapSource.Create(
                width,
                height,
                96,
                96,
                COLOR_PIXEL_FORMAT_WPF,
                null,
                buffer,
                width * COLOR_BYTES_PER_PIXEL
            );
        }

        public static BitmapSource CreateDepthBitmap(byte[] buffer, int width, int height, int stride)
        {
            long bytes = stride * height * DEPTH_BYTES_PER_PIXEL;

            if (buffer.Length != bytes)
                throw new ArgumentException(string.Format("Buffer is incorrect length, expected {0}", bytes));

            //Buffer.BlockCopy(buffer, 0, _smallBuffer, 0, buffer.Length);
            
            //for (int i = 0; i < _smallBuffer.Length; ++i)
            //    _smallBuffer[i] <<= 3;

            //BitmapSource result = BufferCaptureBitmapHelper(_smallBuffer, width, height, DEPTH_BYTES_PER_PIXEL, buffer, stride);


            return BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Gray16,
                null,
                buffer,
                stride * DEPTH_BYTES_PER_PIXEL
            );
        }

        public static Blkd CreateDepthToCameraBlkd(PXCMPoint3DF32[] _depthToCameraMapping, int width, int height)
        {
            var BytesPerPixel = 3 * sizeof(float);

            byte[] buffer = new byte[width * height * BytesPerPixel];

            for (int i = 0; i < _depthToCameraMapping.Length; i++)
            {
                var point = _depthToCameraMapping[i];

                var smallfloatBuffer = new float[] { point.x, point.y, point.z };

                Buffer.BlockCopy(smallfloatBuffer, 0, buffer, i * BytesPerPixel, BytesPerPixel);
            }

            Blkd result = new Blkd
            {
                Width = (UInt16)width,
                Height = (UInt16)height,
                BytesPerPixel = (byte)BytesPerPixel,
                Version = 2,
                Data = buffer
            };

            return result;
        }


    //    public Tuple<BitmapSource, TimeSpan> CaptureDepthFrameBitmap(LiveFrame frame, byte[] buffer)
    //    {
    //        DepthFrame depthFrame = frame.NativeDepthFrame;

    //        int width = depthFrame.FrameDescription.Width;
    //        int height = depthFrame.FrameDescription.Height;

    //        depthFrame.CopyFrameDataToArray(_smallBuffer);

    //        // Multiply all values by 8 to make the frames more previewable
    //        for (int i = 0; i < _smallBuffer.Length; ++i)
    //            _smallBuffer[i] <<= 3;

    //        BitmapSource result = BufferCaptureBitmapHelper(_smallBuffer, width, height, 2, buffer);
    //        return new Tuple<BitmapSource, TimeSpan>(result, depthFrame.RelativeTime);
    //    }

    //    private void ValidateBuffer(byte[] bytes, int width, int height, byte bytesPerPixel)
    //    {
    //        if (bytes.Length != width * height * bytesPerPixel)
    //            throw new ArgumentException(string.Format("Buffer length doesn't match expected {0}x{1}x{2}", width, height, bytesPerPixel));
    //    }

    //    /// <summary>
    //    /// This method is similar to BitmapBuilder.buildColorBitmap. However, that method uses
    //    /// LargeFrameBitmap which encapsulates WriteableBitmap, and a WriteableBitmap can't be
    //    /// used on a different thread from the one which created it. It can't even be cloned,
    //    /// or used to create a new WriteableBitmap on a different thread.
    //    /// 
    //    /// So we provide this separate interface.
    //    /// 
    //    /// TODO: Examine this class and BitmapBuilder for overlaps, and determine if some
    //    /// consolidation is appropriate. Note that the methods here all provide raw data,
    //    /// whereas many of the methods in BitmapBuilder involve some processing.
    //    /// </summary>
    //    /// <param name="frame"></param>
    //    /// <param name="buffer"></param>
    //    /// <returns></returns>
    //    public Tuple<BitmapSource, TimeSpan> CaptureColorFrameBitmap(LiveFrame frame, byte[] buffer)
    //    {
    //        ValidateBuffer(buffer, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT, COLOR_BYTES_PER_PIXEL);

    //        ColorFrame colorFrame = frame.NativeColorFrame;

    //        colorFrame.CopyConvertedFrameDataToArray(buffer, ColorImageFormat.Bgra);

    //        BitmapSource result = CreateColorBitmap(buffer, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT);
    //        return new Tuple<BitmapSource, TimeSpan>(result, colorFrame.RelativeTime);
    //    }

    }
}
