using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Core
{
    // Simplex tableau: m constraint rows + 1 objective row, n variable columns + 1 RHS column.
    public class Tableau
    {
        public double[,] Matrix { get; set; }
        public int[] BasicVariableIndices { get; set; }
        public string[] ColumnLabels { get; set; }

        public int RowCount => Matrix.GetLength(0);
        public int ColCount => Matrix.GetLength(1);
        public int ObjectiveRow => RowCount - 1;
        public int RhsColumn => ColCount - 1;

        public Tableau(int constraintCount, int totalColumnsIncludingRhs)
        {
            Matrix = new double[constraintCount + 1, totalColumnsIncludingRhs];
            BasicVariableIndices = new int[constraintCount];
            ColumnLabels = new string[totalColumnsIncludingRhs];
        }

        // Standard Gauss-Jordan pivot: normalize pivotRow so the pivot element = 1,
        // then eliminate pivotCol out of every other row.
        public void Pivot(int pivotRow, int pivotCol)
        {
            double pivotValue = Matrix[pivotRow, pivotCol];

            for (int c = 0; c < ColCount; c++)
                Matrix[pivotRow, c] /= pivotValue;

            for (int r = 0; r < RowCount; r++)
            {
                if (r == pivotRow) continue;
                double factor = Matrix[r, pivotCol];
                if (factor == 0) continue;

                for (int c = 0; c < ColCount; c++)
                    Matrix[r, c] -= factor * Matrix[pivotRow, c];
            }

            BasicVariableIndices[pivotRow] = pivotCol;
        }

        // Most negative coefficient in the objective row = entering column.
        // Returns -1 when none are negative (optimal reached).
        public int FindEnteringColumn()
        {
            int best = -1;
            double bestValue = -1e-9;

            for (int c = 0; c < RhsColumn; c++)
            {
                if (Matrix[ObjectiveRow, c] < bestValue)
                {
                    bestValue = Matrix[ObjectiveRow, c];
                    best = c;
                }
            }

            return best;
        }

        // Minimum ratio test. Returns -1 when no positive entry exists in the
        // entering column (problem is unbounded).
        public int FindLeavingRow(int enteringCol)
        {
            int best = -1;
            double bestRatio = double.PositiveInfinity;

            for (int r = 0; r < RowCount - 1; r++)
            {
                double coeff = Matrix[r, enteringCol];
                if (coeff <= 1e-9) continue;

                double ratio = Matrix[r, RhsColumn] / coeff;
                if (ratio < bestRatio - 1e-9)
                {
                    bestRatio = ratio;
                    best = r;
                }
            }

            return best;
        }
    }
}
