using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public static class PXCMImageExtensions
    {
        public static byte[] GetRGB32Pixels(this PXCMImage image, out int cwidth, out int cheight)
        {
            PXCMImage.ImageData cdata;
            byte[] cpixels = null;
            cwidth = cheight = 0;
            if (image.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out cdata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                cwidth = (int)cdata.pitches[0] / sizeof(Int32);
                cheight = (int)image.info.height;
                cpixels = cdata.ToByteArray(0, (int)cdata.pitches[0] * cheight);
                image.ReleaseAccess(cdata);
            }
            return cpixels;
        }

        public static void CopyToByteArray(this PXCMImage image, byte[] data, bool for_preview=false)
        {
            PXCMImage.ImageData cdata;
            PXCMImage.PixelFormat pixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32;

            if (!for_preview && (image.streamType == PXCMCapture.StreamType.STREAM_TYPE_DEPTH))
            {
                pixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH;
            }
            else if(image.streamType == PXCMCapture.StreamType.STREAM_TYPE_LEFT || image.streamType == PXCMCapture.StreamType.STREAM_TYPE_RIGHT)
            {
                pixelFormat = PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH;
            }


            pxcmStatus status = image.AcquireAccess(PXCMImage.Access.ACCESS_READ, pixelFormat, out cdata);
            if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                throw new InvalidOperationException("AcquireAccess failed");
            }
            //cdata.ToByteArray(0, data);
            //Pitches is the real stride width the sensor returns. it's 640 for the current resolution
            byte[] rawData= cdata.ToByteArray(0, cdata.pitches[0] * image.info.height);

            Buffer.BlockCopy(rawData, 0, data, 0, rawData.Length);

            image.ReleaseAccess(cdata);
        }
    }
}
