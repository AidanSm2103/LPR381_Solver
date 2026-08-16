using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Utils
{
    public static class MathHelpers
    {
        public static double[,] Multiply(double[,] a, double[,] b)
        {
            int rows = a.GetLength(0);
            int inner = a.GetLength(1);
            int cols = b.GetLength(1);

            if (inner != b.GetLength(0))
                throw new InvalidOperationException("Matrix dimensions do not match for multiplication.");

            var result = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < inner; k++)
                        sum += a[i, k] * b[k, j];
                    result[i, j] = sum;
                }

            return result;
        }

        // Gauss-Jordan inversion with partial pivoting. Assumes a square, non-singular matrix —
        // used by Revised Simplex to recompute B^-1 each iteration.
        public static double[,] Invert(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            var augmented = new double[n, 2 * n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    augmented[i, j] = matrix[i, j];
                augmented[i, n + i] = 1;
            }

            for (int col = 0; col < n; col++)
            {
                int pivotRow = col;
                double maxVal = Math.Abs(augmented[col, col]);
                for (int r = col + 1; r < n; r++)
                {
                    if (Math.Abs(augmented[r, col]) > maxVal)
                    {
                        maxVal = Math.Abs(augmented[r, col]);
                        pivotRow = r;
                    }
                }

                if (maxVal < 1e-12)
                    throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

                if (pivotRow != col)
                {
                    for (int c = 0; c < 2 * n; c++)
                        (augmented[col, c], augmented[pivotRow, c]) = (augmented[pivotRow, c], augmented[col, c]);
                }

                double pivotVal = augmented[col, col];
                for (int c = 0; c < 2 * n; c++)
                    augmented[col, c] /= pivotVal;

                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double factor = augmented[r, col];
                    if (factor == 0) continue;
                    for (int c = 0; c < 2 * n; c++)
                        augmented[r, c] -= factor * augmented[col, c];
                }
            }

            var inverse = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    inverse[i, j] = augmented[i, n + j];

            return inverse;
        }
    }
}
