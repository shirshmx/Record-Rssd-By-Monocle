using System;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    class Projection : IDisposable
    {
        private PXCMProjection projection = null;
        private UInt16 invalid_value; /* invalid depth values */
        private PXCMPointF32[] uvmap;
        private PXCMPointF32[] invuvmap;

        public UInt16 InvalidDepthValue { get { return invalid_value; } }

        public Projection(PXCMSession session, PXCMCapture.Device device, PXCMImage.ImageInfo dinfo, PXCMImage.ImageInfo cinfo)
        {
            /* retrieve the invalid depth pixel values */
            invalid_value = device.QueryDepthLowConfidenceValue();

            /* Create the projection instance */
            projection = device.CreateProjection();

            uvmap = new PXCMPointF32[dinfo.width * dinfo.height];
            invuvmap = new PXCMPointF32[cinfo.width * cinfo.height];
        }

        public void Dispose()
        {
            if (projection == null) return;
            projection.Dispose();
            projection = null;
        }

        public void DepthToCameraCoordinates(PXCMImage depth, PXCMPoint3DF32[] cameraSpacePts) 
        {
            /* Retrieve the depth pixels and uvmap */
            PXCMImage.ImageData ddata;
            UInt16[] dpixels;

            bool isdepth = (depth.info.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH);
            if (depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, out ddata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Int32 dpitch = ddata.pitches[0] / sizeof(Int16); /* aligned width */
                Int32 dwidth = (Int32)depth.info.width;
                Int32 dheight = (Int32)depth.info.height;
                dpixels = ddata.ToUShortArray(0, isdepth ? dpitch * dheight : dpitch * dheight * 3);
                depth.ReleaseAccess(ddata);

                /* Projection Calculation */
                PXCMPoint3DF32[] dcords = new PXCMPoint3DF32[dwidth * dheight];
                for (Int32 y = 0, k = 0; y < dheight; y++)
                {
                    for (Int32 x = 0; x < dwidth; x++, k++)
                    {
                        dcords[k].x = x;
                        dcords[k].y = y;
                        dcords[k].z = isdepth ? dpixels[y * dpitch + x] : dpixels[3 * (y * dpitch + x) + 2];
                    }
                }


                pxcmStatus status = projection.ProjectDepthToCamera(dcords, cameraSpacePts);
                if (status != pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    throw new InvalidOperationException("Projection depth to camera failed");
                }
            }

        }

        private void PlotXY(byte[] cpixels, int xx, int yy, int cwidth, int cheight, int dots, int color)
        {
            if (xx < 0 || xx >= cwidth || yy < 0 || yy >= cheight) return;

            int lyy = yy * cwidth;
            int xxm1 = (xx > 0 ? xx - 1 : xx), xxp1 = (xx < (int)cwidth - 1 ? xx + 1 : xx);
            int lyym1 = yy > 0 ? lyy - cwidth : lyy, lyyp1 = yy < (int)cheight - 1 ? lyy + cwidth : lyy;

            if (dots >= 9)  /* 9 dots */
            {
                cpixels[(lyym1 + xxm1) * 4 + color] = 0xFF;
                cpixels[(lyym1 + xxp1) * 4 + color] = 0xFF;
                cpixels[(lyyp1 + xxm1) * 4 + color] = 0xFF;
                cpixels[(lyyp1 + xxp1) * 4 + color] = 0xFF;
            }
            if (dots >= 5)  /* 5 dots */
            {
                cpixels[(lyym1 + xx) * 4 + color] = 0xFF;
                cpixels[(lyy + xxm1) * 4 + color] = 0xFF;
                cpixels[(lyy + xxp1) * 4 + color] = 0xFF;
                cpixels[(lyyp1 + xx) * 4 + color] = 0xFF;
            }
            cpixels[(lyy + xx) * 4 + color] = 0xFF; /* 1 dot */
        }

        public byte[] DepthToColorCoordinatesByUVMAP(PXCMImage color, PXCMImage depth, int dots, out int cwidth, out int cheight)
        {
            /* Retrieve the color pixels */
            byte[] cpixels = color.GetRGB32Pixels(out cwidth, out cheight);
            if (cpixels == null) return cpixels;

            /* Retrieve the depth pixels and uvmap */
            PXCMImage.ImageData ddata;
            UInt16[] dpixels;
            // float[] uvmap;
            bool isdepth = (depth.info.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH);
            if (depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, out ddata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Int32 dpitch = ddata.pitches[0] / sizeof(short); /* aligned width */
                Int32 dwidth = (Int32)depth.info.width;
                Int32 dheight = (Int32)depth.info.height;
                dpixels = ddata.ToUShortArray(0, isdepth ? dpitch * dheight : dpitch * dheight * 3);

                projection.QueryUVMap(depth, uvmap);
                int uvpitch = depth.QueryInfo().width;
                depth.ReleaseAccess(ddata);

                /* Draw dots onto the color pixels */
                for (Int32 y = 0; y < dheight; y++)
                {
                    for (Int32 x = 0; x < dwidth; x++)
                    {
                        UInt16 d = isdepth ? dpixels[y * dpitch + x] : dpixels[3 * (y * dpitch + x) + 2];
                        if (d == invalid_value) continue; // no mapping based on unreliable depth values

                        float uvx = uvmap[y * uvpitch + x].x, uvy = uvmap[y * uvpitch + x].y;
                        Int32 xx = (Int32)(uvx * cwidth), yy = (Int32)(uvy * cheight);
                        PlotXY(cpixels, xx, yy, cwidth, cheight, dots, 1);
                    }
                }
            }
            return cpixels;
        }

        public byte[] DepthToColorCoordinatesByFunction(PXCMImage color, PXCMImage depth, int dots, out int cwidth, out int cheight)
        {
            /* Retrieve the color pixels */
            byte[] cpixels = color.GetRGB32Pixels(out cwidth, out cheight);
            if (projection == null || cpixels == null) return cpixels;

            /* Retrieve the depth pixels and uvmap */
            PXCMImage.ImageData ddata;
            UInt16[] dpixels;
            bool isdepth = (depth.info.format == PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH);
            if (depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, out ddata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Int32 dpitch = ddata.pitches[0] / sizeof(Int16); /* aligned width */
                Int32 dwidth = (Int32)depth.info.width;
                Int32 dheight = (Int32)depth.info.height;
                dpixels = ddata.ToUShortArray(0, isdepth ? dpitch * dheight : dpitch * dheight * 3);
                depth.ReleaseAccess(ddata);

                /* Projection Calculation */
                PXCMPoint3DF32[] dcords = new PXCMPoint3DF32[dwidth * dheight];
                for (Int32 y = 0, k = 0; y < dheight; y++)
                {
                    for (Int32 x = 0; x < dwidth; x++, k++)
                    {
                        dcords[k].x = x;
                        dcords[k].y = y;
                        dcords[k].z = isdepth ? dpixels[y * dpitch + x] : dpixels[3 * (y * dpitch + x) + 2];
                    }
                }
                PXCMPointF32[] ccords = new PXCMPointF32[dwidth * dheight];
                projection.MapDepthToColor(dcords, ccords);

                /* Draw dots onto the color pixels */
                for (Int32 y = 0, k = 0; y < dheight; y++)
                {
                    for (Int32 x = 0; x < dwidth; x++, k++)
                    {
                        UInt16 d = isdepth ? dpixels[y * dpitch + x] : dpixels[3 * (y * dpitch + x) + 2];
                        if (d == invalid_value) continue; // no mapping based on unreliable depth values

                        Int32 xx = (Int32)ccords[k].x, yy = (Int32)ccords[k].y;
                        PlotXY(cpixels, xx, yy, cwidth, cheight, dots, 2);
                    }
                }
            }
            return cpixels;
        }

        public byte[] ColorToDepthCoordinatesByInvUVMap(PXCMImage color, PXCMImage depth, int dots, out int cwidth, out int cheight)
        {
            /* Retrieve the color pixels */
            byte[] cpixels = color.GetRGB32Pixels(out cwidth, out cheight);

            if (projection == null || cpixels == null) return cpixels;

            if (dots >= 9)
            { // A sample for CreateDepthImageMappedToColor output visualization
                PXCMImage.ImageData d2cDat;
                PXCMImage d2c = projection.CreateDepthImageMappedToColor(depth, color);
                if (d2c == null)
                {
                    return cpixels;
                }

                UInt16[] d2cpixels;
                if (d2c.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH, out d2cDat) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    Int32 d2cwidth = d2cDat.pitches[0] / sizeof(Int16); /* aligned width */
                    Int32 d2cheight = (Int32)d2c.info.height;
                    d2cpixels = d2cDat.ToUShortArray(0, d2cwidth * d2cheight);

                    for (Int32 y = 0; y < cheight; y++)
                    {
                        for (Int32 x = 0; x < cwidth; x++)
                        {
                            if (d2cpixels[y * d2cwidth + x] == invalid_value) continue; // no mapping based on unreliable depth values
                            cpixels[(y * cwidth + x) * 4] = 0xFF;
                        }
                    }

                    d2c.ReleaseAccess(d2cDat);
                }
                d2c.Dispose();
                return cpixels;
            }

            /* Retrieve the depth pixels and uvmap */
            PXCMImage.ImageData ddata;
            Int16[] dpixels;
            if (depth.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_DEPTH, out ddata) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                Int32 dpitch = ddata.pitches[0] / sizeof(Int16); /* aligned width */
                Int32 dwidth = (Int32)depth.info.width;
                Int32 dheight = (Int32)depth.info.height;
                dpixels = ddata.ToShortArray(0, dpitch * dheight);
                pxcmStatus sts = projection.QueryInvUVMap(depth, invuvmap);
                Int32 invuvpitch = color.QueryInfo().width;
                depth.ReleaseAccess(ddata);

                if (dots > 1)
                { // If Depth data is valid just set a blue pixel
                    /* Draw dots onto the color pixels */
                    for (Int32 y = 0; y < cheight; y++)
                    {
                        for (Int32 x = 0; x < cwidth; x++)
                        {
                            Int32 xx = (Int32)(invuvmap[y * cwidth + x].x * dwidth);
                            Int32 yy = (Int32)(invuvmap[y * cwidth + x].y * dheight);
                            if (xx >= 0 && yy >= 0)
                            {
                                if (dpixels[yy * dpitch + xx] > 0)
                                {
                                    cpixels[(y * cwidth + x) * 4] = 0xFF;
                                }
                            }
                        }
                    }
                }
                else
                { // If Depth data is valid just set a blue pixel with briteness depends on Depth value
                    Int32 MAX_LOCAL_DEPTH_VALUE = 4000;
                    Int32[] depth_hist = new Int32[MAX_LOCAL_DEPTH_VALUE];
                    Array.Clear(depth_hist, 0, depth_hist.Length);
                    Int32 num_depth_points = 0;
                    for (Int32 y = 0; y < dheight; y++)
                    {
                        for (Int32 x = 0; x < dwidth; x++)
                        {
                            Int16 d = dpixels[y * dpitch + x];
                            if (d > 0 && d < MAX_LOCAL_DEPTH_VALUE)
                            {
                                depth_hist[d]++;
                                num_depth_points++;
                            }
                        }
                    }

                    if (num_depth_points > 0)
                    {
                        for (Int32 i = 1; i < MAX_LOCAL_DEPTH_VALUE; i++)
                        {
                            depth_hist[i] += depth_hist[i - 1];
                        }
                        for (Int32 i = 1; i < MAX_LOCAL_DEPTH_VALUE; i++)
                        {
                            depth_hist[i] = 255 - (Int32)((float)255 * (float)depth_hist[i] / (float)num_depth_points);
                        }

                        /* Draw dots onto the color pixels */
                        for (Int32 y = 0; y < cheight; y++)
                        {
                            for (Int32 x = 0; x < cwidth; x++)
                            {
                                Int32 xx = (Int32)(invuvmap[y * cwidth + x].x * dwidth);
                                Int32 yy = (Int32)(invuvmap[y * cwidth + x].y * dheight);
                                if (xx >= 0 && yy >= 0)
                                {
                                    Int16 d = dpixels[yy * dpitch + xx];
                                    if (d > 0 && d < MAX_LOCAL_DEPTH_VALUE)
                                    {
                                        cpixels[(y * cwidth + x) * 4] = (byte)depth_hist[d];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return cpixels;
        }
    }

}
