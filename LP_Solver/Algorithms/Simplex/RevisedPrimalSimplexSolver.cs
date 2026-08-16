using LP_Solver.Core;
using LP_Solver.Models;
using LP_Solver.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Algorithms.Simplex
{
    // Revised Primal Simplex: works with the basis inverse (B^-1) each iteration
    // rather than carrying the full tableau. B^-1 is recomputed each iteration via
    // Gauss-Jordan (Utils.MathHelpers.Invert) and used to "price out" reduced costs —
    // this gets the same Product Form / Price Out numbers the spec asks for, though a
    // faster implementation would update B^-1 incrementally instead of re-inverting.
    public class RevisedPrimalSimplexSolver : ISolver
    {
        public string Name => "Revised Primal Simplex";

        private const double BigM = 1_000_000;
        private const int MaxIterations = 200;

        public SolverResult Solve(LPModel model)
        {
            var result = new SolverResult { AlgorithmName = Name };
            var (A, b, c, labels, basis, artificialStart, _) = BuildStandardForm(model);

            int m = A.GetLength(0);
            int totalCols = A.GetLength(1);
            int n = model.Objective.Coefficients.Length;

            int iteration = 0;

            while (true)
            {
                double[,] Binv = MathHelpers.Invert(ExtractColumns(A, basis));

                double[] cb = new double[m];
                for (int i = 0; i < m; i++) cb[i] = c[basis[i]];

                // Price vector y = cb * Binv
                double[] y = new double[m];
                for (int j = 0; j < m; j++)
                {
                    double sum = 0;
                    for (int i = 0; i < m; i++) sum += cb[i] * Binv[i, j];
                    y[j] = sum;
                }

                double[] xB = new double[m];
                for (int i = 0; i < m; i++)
                {
                    double sum = 0;
                    for (int k = 0; k < m; k++) sum += Binv[i, k] * b[k];
                    xB[i] = sum;
                }

                result.IterationLog.Add(FormatIteration(iteration, labels, basis, Binv, y, xB));

                // Reduced cost for each non-basic column: (y . A_j) - c_j.
                // Most negative = entering column (matches Primal Simplex's convention).
                int enteringCol = -1;
                double mostNegative = -1e-9;

                for (int j = 0; j < totalCols; j++)
                {
                    if (Array.IndexOf(basis, j) >= 0) continue;

                    double reduced = -c[j];
                    for (int i = 0; i < m; i++) reduced += y[i] * A[i, j];

                    if (reduced < mostNegative)
                    {
                        mostNegative = reduced;
                        enteringCol = j;
                    }
                }

                if (enteringCol == -1) break; // optimal

                double[] d = new double[m];
                for (int i = 0; i < m; i++)
                {
                    double sum = 0;
                    for (int k = 0; k < m; k++) sum += Binv[i, k] * A[k, enteringCol];
                    d[i] = sum;
                }

                int leavingRow = -1;
                double bestRatio = double.PositiveInfinity;

                for (int i = 0; i < m; i++)
                {
                    if (d[i] <= 1e-9) continue;
                    double ratio = xB[i] / d[i];
                    if (ratio < bestRatio - 1e-9)
                    {
                        bestRatio = ratio;
                        leavingRow = i;
                    }
                }

                if (leavingRow == -1)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add("No positive entry in the direction vector — problem is unbounded.");
                    return result;
                }

                basis[leavingRow] = enteringCol;
                iteration++;

                if (iteration > MaxIterations)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add("Exceeded max iterations — check the model for cycling.");
                    return result;
                }
            }

            double[,] finalBinv = MathHelpers.Invert(ExtractColumns(A, basis));
            double[] finalXB = new double[m];
            for (int i = 0; i < m; i++)
            {
                double sum = 0;
                for (int k = 0; k < m; k++) sum += finalBinv[i, k] * b[k];
                finalXB[i] = sum;
            }

            for (int i = 0; i < m; i++)
            {
                if (basis[i] >= artificialStart && finalXB[i] > 1e-6)
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("An artificial variable remained in the basis with a positive value — problem is infeasible.");
                    return result;
                }
            }

            result.Status = SolverStatus.Optimal;

            var values = new double[n];
            for (int j = 0; j < n; j++)
            {
                int row = Array.IndexOf(basis, j);
                values[j] = row >= 0 ? finalXB[row] : 0;
            }
            result.VariableValues = values;

            double objective = 0;
            for (int i = 0; i < m; i++) objective += c[basis[i]] * finalXB[i];
            result.ObjectiveValue = model.Objective.Type == ObjectiveType.Min ? -objective : objective;

            return result;
        }

        private static double[,] ExtractColumns(double[,] A, int[] basis)
        {
            int m = A.GetLength(0);
            var B = new double[m, m];
            for (int col = 0; col < basis.Length; col++)
                for (int row = 0; row < m; row++)
                    B[row, col] = A[row, basis[col]];
            return B;
        }

        private static string FormatIteration(int iteration, string[] labels, int[] basis, double[,] Binv, double[] y, double[] xB)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Iteration {iteration} (Product Form / Price Out)");

            sb.Append("Basis: ");
            foreach (var bIdx in basis) sb.Append(labels[bIdx] + " ");
            sb.AppendLine();

            sb.AppendLine("B^-1:");
            int m = Binv.GetLength(0);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                    sb.Append(Binv[i, j].ToString("F3").PadLeft(10));
                sb.AppendLine();
            }

            sb.Append("Price vector y:");
            foreach (var v in y) sb.Append(v.ToString("F3").PadLeft(10));
            sb.AppendLine();

            sb.Append("xB:");
            foreach (var v in xB) sb.Append(v.ToString("F3").PadLeft(10));
            sb.AppendLine();

            return sb.ToString();
        }

        // Same constraint handling as CanonicalFormBuilder, but returns raw matrices
        // (A, b, c) instead of a combined tableau, since Revised Simplex works with
        // B^-1 directly rather than row-reducing one shared matrix.
        private static (double[,] A, double[] b, double[] c, string[] labels, int[] basis, int artificialStart, int artificialCount)
            BuildStandardForm(LPModel model)
        {
            int n = model.Objective.Coefficients.Length;
            int m = model.Constraints.Count;

            int slackCount = 0, surplusCount = 0, artificialCount = 0;
            foreach (var con in model.Constraints)
            {
                switch (con.Relation)
                {
                    case ConstraintRelation.LessThanOrEqual: slackCount++; break;
                    case ConstraintRelation.GreaterThanOrEqual: surplusCount++; artificialCount++; break;
                    case ConstraintRelation.Equal: artificialCount++; break;
                }
            }

            int totalVars = n + slackCount + surplusCount + artificialCount;
            var A = new double[m, totalVars];
            var b = new double[m];
            var c = new double[totalVars];
            var labels = new string[totalVars];
            var basis = new int[m];

            int slackStart = n;
            int surplusStart = n + slackCount;
            int artificialStart = n + slackCount + surplusCount;

            int col = 0;
            for (int i = 0; i < n; i++) labels[col++] = $"x{i + 1}";
            for (int i = 0; i < slackCount; i++) labels[col++] = $"s{i + 1}";
            for (int i = 0; i < surplusCount; i++) labels[col++] = $"e{i + 1}";
            for (int i = 0; i < artificialCount; i++) labels[col++] = $"a{i + 1}";

            double sign = model.Objective.Type == ObjectiveType.Min ? -1 : 1;
            for (int i = 0; i < n; i++) c[i] = sign * model.Objective.Coefficients[i];
            for (int i = artificialStart; i < totalVars; i++) c[i] = -BigM;

            int slackIdx = 0, surplusIdx = 0, artificialIdx = 0;

            for (int r = 0; r < m; r++)
            {
                var con = model.Constraints[r];
                for (int i = 0; i < n; i++) A[r, i] = con.Coefficients[i];

                switch (con.Relation)
                {
                    case ConstraintRelation.LessThanOrEqual:
                        A[r, slackStart + slackIdx] = 1;
                        basis[r] = slackStart + slackIdx;
                        slackIdx++;
                        break;

                    case ConstraintRelation.GreaterThanOrEqual:
                        A[r, surplusStart + surplusIdx] = -1;
                        surplusIdx++;
                        A[r, artificialStart + artificialIdx] = 1;
                        basis[r] = artificialStart + artificialIdx;
                        artificialIdx++;
                        break;

                    case ConstraintRelation.Equal:
                        A[r, artificialStart + artificialIdx] = 1;
                        basis[r] = artificialStart + artificialIdx;
                        artificialIdx++;
                        break;
                }

                b[r] = con.Rhs;
            }

            return (A, b, c, labels, basis, artificialStart, artificialCount);
        }
    }
}
