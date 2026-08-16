using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    public enum ConstraintRelation
    {
        LessThanOrEqual,
        GreaterThanOrEqual,
        Equal
    }

    // Parsed from one constraint line, e.g.
    // "+11 +8 +6 +14 +10 +10 <=40" -> Coefficients=[11,8,6,14,10,10], Relation=LessThanOrEqual, Rhs=40
    public class Constraint
    {
        public double[] Coefficients { get; set; } = System.Array.Empty<double>();
        public ConstraintRelation Relation { get; set; }
        public double Rhs { get; set; }
    }
}

