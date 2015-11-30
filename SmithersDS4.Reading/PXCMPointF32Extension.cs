using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public static class PXCMPointF32Extension
    {

        public static float[] toFloatArray(this PXCMPointF32 pf32)
        {
            return new float[] { pf32.x, pf32.y };
        }
    }
}
