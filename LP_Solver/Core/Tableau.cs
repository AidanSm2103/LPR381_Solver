using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Core
{
    // TODO (Person 1): design this once CanonicalFormBuilder is being written —
    // needs the coefficient matrix, RHS column, basic variable indices per row,
    // and objective row (for pivoting / optimality checks). Every algorithm
    // (Primal Simplex, Revised Primal Simplex, B&B, Cutting Plane) operates on this.
    public class Tableau
    {
        public double[,] Matrix { get; set; } = new double[0, 0];
        public int[] BasicVariableIndices { get; set; } = System.Array.Empty<int>();
        public string[] ColumnLabels { get; set; } = System.Array.Empty<string>();

        // TODO: add whatever pivot/ratio-test helper methods the simplex algorithms need
    }
}
