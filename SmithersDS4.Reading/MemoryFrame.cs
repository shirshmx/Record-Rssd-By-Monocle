using Smithers.Reading.FrameData;
using Smithers.Serialization.Formats;
using System;
using System.Windows.Media.Imaging;

namespace SmithersDS4.Reading
{
    public struct AccelerometerStats
    {
        public double accX;
        public double accY;
        public double accZ;
        public DateTimeOffset timestamp;
    }

    /// <summary>
    /// Represents a serialized frame in memory.
    /// 
    /// A relatively heavyweight object, intended to be reused.
    /// </summary>
    public class MemoryFrame
    {
        public static readonly int COLOR_BYTES_PER_PIXEL = 4;
        public static readonly int DEPTH_BYTES_PER_PIXEL = 2; // uint16

        // Buffer storage
        byte[] _bufferColor = new byte[Frame.COLOR_PIXELS * COLOR_BYTES_PER_PIXEL];
        byte[] _bufferDepth = new byte[Frame.DEPTH_PIXELS * DEPTH_BYTES_PER_PIXEL];

        byte[] _bufferIRLeft = new byte[Frame.DEPTH_PIXELS * DEPTH_BYTES_PER_PIXEL];
        byte[] _bufferIRRight = new byte[Frame.DEPTH_PIXELS * DEPTH_BYTES_PER_PIXEL];

        byte[] _bufferDepthPreview = new byte[Frame.DEPTH_PIXELS * COLOR_BYTES_PER_PIXEL];

        float[] _cameraPose = new float[Frame.CAMERA_POSE_LEN];

        public byte[] BufferColor { get { return _bufferColor; } }
        public byte[] BufferDepth { get { return _bufferDepth; } }

        public byte[] BufferIRLeft { get { return _bufferIRLeft; } }
        public byte[] BufferIRRight { get { return _bufferIRRight; } }

        public byte[] BufferDepthPreview
        {
            get
            {
                return _bufferDepthPreview;
            }
        }

        public float[] CameraPose { get { return _cameraPose;  } }
        public string TrackingAccuracy { get; set; }
        public string VoxelResolution { get; set; }


        public long ColorTimeStamp { get; set; }
        public long DepthTimeStamp { get; set; }

        public AccelerometerStats AcceleroStats { get; set; }

        public UInt16 LowConfidenceDepthValue { get; set; }

        // Bitmap handles (underlying storage uses corresponding buffers above)
        //Tuple<BitmapSource, TimeSpan> _color;
        //Tuple<BitmapSource, TimeSpan> _depth;

        /// <summary>
        /// We call Freeze() so we can write these bitmaps to disk from other threads.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="serializer"></param>
        //public void Update(byte[] color, byte[] depth)
        //{
        //    _color = serializer.CaptureColorFrameBitmap(frame, _bufferColor);
        //    _color.Item1.Freeze();

        //    _depth = serializer.CaptureDepthFrameBitmap(frame, _bufferDepth);
        //    _depth.Item1.Freeze();
        //}


        public Tuple<BitmapSource, TimeSpan> Color {
            get
            {
                return new Tuple<BitmapSource, TimeSpan>(
                    SmithersDS4.Reading.FrameSerializer.CreateColorBitmap(_bufferColor, Frame.COLOR_WIDTH, Frame.COLOR_HEIGHT),
                    new TimeSpan()
                );
            }
        }
        public Tuple<BitmapSource, TimeSpan> Depth
        {
            get
            {
                return new Tuple<BitmapSource, TimeSpan>(
                    SmithersDS4.Reading.FrameSerializer.CreateDepthBitmap(_bufferDepth, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_STRIDE),
                    new TimeSpan()
                );
            }
        }

        public Tuple<BitmapSource, TimeSpan> IRLeft
        {
            get
            {
                return new Tuple<BitmapSource, TimeSpan>(
                    SmithersDS4.Reading.FrameSerializer.CreateDepthBitmap(_bufferIRLeft, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_STRIDE),
                    new TimeSpan()
                );
            }
        }

        public Tuple<BitmapSource, TimeSpan> IRRight
        {
            get
            {
                return new Tuple<BitmapSource, TimeSpan>(
                    SmithersDS4.Reading.FrameSerializer.CreateDepthBitmap(_bufferIRRight, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT, Frame.DEPTH_STRIDE),
                    new TimeSpan()
                );
            }
        }

#if DEBUG
        //Mapping depth-camera
        PXCMPoint3DF32[] _depthToCameraMapping = new PXCMPoint3DF32[Frame.DEPTH_HEIGHT * Frame.DEPTH_WIDTH];
        
        public PXCMPoint3DF32[] DepthToCameraMapping { get { return _depthToCameraMapping;} }

        public Tuple<Blkd, TimeSpan> DepthToCamera
        {
            get
            {
                return new Tuple<Blkd, TimeSpan>(
                    SmithersDS4.Reading.FrameSerializer.CreateDepthToCameraBlkd(_depthToCameraMapping, Frame.DEPTH_WIDTH, Frame.DEPTH_HEIGHT),
                    new TimeSpan()
                );
            }
        }
#endif
    }
}
