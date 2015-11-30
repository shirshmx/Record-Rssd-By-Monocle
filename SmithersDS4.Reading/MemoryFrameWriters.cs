using Smithers.Serialization.Formats;
using Smithers.Serialization.Writers;
using Smithers.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SmithersDS4.Reading
{

    class DS4SavedItemType : SavedItemType
    {
        public static readonly SavedItemType DEPTH_TO_CAMERA_MAPPING = new SavedItemType("DepthToCamera");
        public static readonly SavedItemType IR_RIGHT_IMAGE = new SavedItemType("IR_RIGHT");
        public static readonly SavedItemType IR_LEFT_IMAGE = new SavedItemType("IR_LEFT");
    
        DS4SavedItemType(string name) : base(name) { }
    }

    public class DepthFrameWriter : PngBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public DepthFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override Smithers.Serialization.SavedItemType Type { get { return Smithers.Serialization.SavedItemType.DEPTH_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Depth.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Depth.Item1; } }
    }

    public class ColorFrameWriter : JpegBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public ColorFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override Smithers.Serialization.SavedItemType Type { get { return Smithers.Serialization.SavedItemType.COLOR_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.Color.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.Color.Item1; } }
    }

    public class IRLeftFrameWriter : DepthFrameWriter
    {
        public IRLeftFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override Smithers.Serialization.SavedItemType Type { get { return DS4SavedItemType.IR_LEFT_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.IRLeft.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.IRLeft.Item1; } }
    }

    public class IRRightFrameWriter : PngBitmapWriter<MemoryFrame, FrameSerializer>
    {
        public IRRightFrameWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }
        public override Smithers.Serialization.SavedItemType Type { get { return DS4SavedItemType.IR_RIGHT_IMAGE; } }

        public override TimeSpan? Timestamp { get { return _frame.IRRight.Item2; } }

        protected override BitmapSource BitmapSource { get { return _frame.IRRight.Item1; } }
    }

#if DEBUG
    public class PXCMPoint3DF32ListWriter : BlkdFrameWriter<MemoryFrame, FrameSerializer>
    {
        public PXCMPoint3DF32ListWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }

        public override Smithers.Serialization.SavedItemType Type { get { return DS4SavedItemType.DEPTH_TO_CAMERA_MAPPING; } }

        public override TimeSpan? Timestamp { get { return _frame.DepthToCamera.Item2; } }


        protected override Blkd BlkdSource { get { return _frame.DepthToCamera.Item1; } }

    }

    public class PXCMPoint3DF32ObjWriter : MemoryFrameWriter<MemoryFrame, FrameSerializer>
    {
        public PXCMPoint3DF32ObjWriter(MemoryFrame frame, FrameSerializer serializer) : base(frame, serializer) { }
        public override Smithers.Serialization.SavedItemType Type { get { return DS4SavedItemType.DEPTH_TO_CAMERA_MAPPING; } }

        public override TimeSpan? Timestamp { get { return _frame.DepthToCamera.Item2; } }

        public override string FileExtension { get { return ".obj"; } }

        public override void Write(Stream stream)
        {
            CultureInfo cultureUS = CultureInfo.GetCultureInfo("en-US");

            PXCMPoint3DF32[] cameraPoints = _frame.DepthToCameraMapping;

            using (TextWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("#depth to camera space point cloud");
                foreach (PXCMPoint3DF32 cpoint in cameraPoints)
                {
                    writer.WriteLine(
                        string.Format(cultureUS, "v {0} {1} {2}",
                        cpoint.x, cpoint.y, cpoint.z)
                    );
                }
            }
        }

    }


#endif

}
