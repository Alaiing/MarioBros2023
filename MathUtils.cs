using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oudidon
{
    public static class MathUtils
    {
        public static float NormalizedParabolicPosition(float t)
        {
            return 4 * t * (1 - t);
        }
    }
}
