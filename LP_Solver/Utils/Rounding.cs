using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Utils
{
    public static class Rounding
    {
        // Spec: "All decimal values should be rounded to three points."
        // Use this everywhere instead of ad-hoc ToString("F3") calls so
        // rounding behavior stays consistent across every algorithm.
        public static double Round3(double value)
        {
            return System.Math.Round(value, 3);
        }

        public static string Format3(double value)
        {
            return value.ToString("F3");
        }
    }
}
