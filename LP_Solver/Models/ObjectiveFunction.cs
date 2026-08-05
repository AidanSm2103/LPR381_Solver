using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    public enum ObjectiveType
    {
        Max,
        Min
    }

    // TODO (Person 1): populate from line 1 of the input file —
    // e.g. "max +2 +3 +3 +5 +2 +4" -> ObjectiveType.Max, Coefficients = [2,3,3,5,2,4]
    public class ObjectiveFunction
    {
        public ObjectiveType Type { get; set; }
        public double[] Coefficients { get; set; } = System.Array.Empty<double>();
    }
}

