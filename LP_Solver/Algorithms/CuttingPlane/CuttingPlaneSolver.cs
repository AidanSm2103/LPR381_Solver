using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.CuttingPlane
{
    public class CuttingPlaneSolver : ISolver
    {
        private const double Tolerance = 1e-6;
        private const int MaxIterations = 200;
        private const int MaxCuts = 200;

        public string Name => "Cutting Plane";

        public SolverResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.Build(model);
            var result = new SolverResult { AlgorithmName = Name };

            result.IterationLog.Add(TableauFormatter.Format(tableau, 0, "Canonical Form (Initial Tableau)"));

            if (!RunPrimalSimplex(tableau, result, "LP Relaxation"))
                return result;

            int n = model.Objective.Coefficients.Length;
            int artificialCount = CountArtificials(model);
            int artificialStart = tableau.ColCount - 1 - artificialCount;

            for (int r = 0; r < tableau.RowCount - 1; r++)
            {
                if (tableau.BasicVariableIndices[r] >= artificialStart &&
                    tableau.Matrix[r, tableau.RhsColumn] > 1e-6)
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("An artificial variable remained in the basis with a positive value — LP relaxation is infeasible.");
                    return result;
                }
            }

            result.IterationLog.Add(TableauFormatter.Format(tableau, 0, "LP Relaxation Optimal"));

            int cutNumber = 0;

            while (true)
            {
                int sourceRow = FindMostFractionalRow(tableau, model, n);

                if (sourceRow == -1)
                {
                    result.Status = SolverStatus.Optimal;
                    result.VariableValues = ExtractVariableValues(tableau, n);

                    double objectiveValue = tableau.Matrix[tableau.ObjectiveRow, tableau.RhsColumn];
                    result.ObjectiveValue = model.Objective.Type == ObjectiveType.Min ? -objectiveValue : objectiveValue;

                    result.FinalTableau = tableau;
                    result.Model = model;

                    return result;
                }

                cutNumber++;

                if (cutNumber > MaxCuts)
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("Exceeded max Gomory cuts — no integer-feasible optimum found.");
                    return result;
                }

                tableau = AppendGomoryCut(tableau, sourceRow, cutNumber);
                result.IterationLog.Add(TableauFormatter.Format(tableau, cutNumber, $"After Gomory Cut {cutNumber}"));

                if (!RunDualSimplex(tableau, result))
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("Dual Simplex could not restore feasibility after the cut — problem is infeasible.");
                    return result;
                }

                result.IterationLog.Add(TableauFormatter.Format(tableau, cutNumber, $"Optimal After Cut {cutNumber}"));
            }
        }

        private static bool RunPrimalSimplex(Tableau tableau, SolverResult result, string label)
        {
            int iteration = 0;

            while (true)
            {
                int enteringCol = tableau.FindEnteringColumn();
                if (enteringCol == -1) return true;

                int leavingRow = tableau.FindLeavingRow(enteringCol);
                if (leavingRow == -1)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add($"No positive ratio found in the entering column during {label} — problem is unbounded.");
                    return false;
                }

                tableau.Pivot(leavingRow, enteringCol);
                iteration++;
                result.IterationLog.Add(TableauFormatter.Format(tableau, iteration, $"{label} — Iteration {iteration}"));

                if (iteration > MaxIterations)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add($"Exceeded max iterations during {label} — check the model for cycling.");
                    return false;
                }
            }
        }

        private static bool RunDualSimplex(Tableau tableau, SolverResult result)
        {
            int iteration = 0;

            while (true)
            {
                int leavingRow = -1;
                double mostNegative = -Tolerance;

                for (int r = 0; r < tableau.RowCount - 1; r++)
                {
                    if (tableau.Matrix[r, tableau.RhsColumn] < mostNegative)
                    {
                        mostNegative = tableau.Matrix[r, tableau.RhsColumn];
                        leavingRow = r;
                    }
                }

                if (leavingRow == -1) return true;

                int enteringCol = -1;
                double bestRatio = double.PositiveInfinity;

                for (int c = 0; c < tableau.RhsColumn; c++)
                {
                    double coeff = tableau.Matrix[leavingRow, c];
                    if (coeff >= -Tolerance) continue;

                    double ratio = tableau.Matrix[tableau.ObjectiveRow, c] / -coeff;
                    if (ratio < bestRatio - Tolerance)
                    {
                        bestRatio = ratio;
                        enteringCol = c;
                    }
                }

                if (enteringCol == -1) return false;

                tableau.Pivot(leavingRow, enteringCol);
                iteration++;
                result.IterationLog.Add(TableauFormatter.Format(tableau, iteration, $"Dual Simplex — Iteration {iteration}"));

                if (iteration > MaxIterations)
                {
                    result.IterationLog.Add("Exceeded max iterations during Dual Simplex — check the model for cycling.");
                    return false;
                }
            }
        }

        private static int FindMostFractionalRow(Tableau tableau, LPModel model, int n)
        {
            int bestRow = -1;
            int bestVariable = int.MaxValue;
            double bestDistance = double.PositiveInfinity;

            for (int r = 0; r < tableau.RowCount - 1; r++)
            {
                int variable = tableau.BasicVariableIndices[r];
                if (variable >= n) continue;
                if (variable >= model.SignRestrictions.Count) continue;

                var type = model.SignRestrictions[variable];
                if (type != VariableType.Int && type != VariableType.Bin) continue;

                double value = tableau.Matrix[r, tableau.RhsColumn];
                double frac = FractionalPart(value);
                if (frac < Tolerance || frac > 1 - Tolerance) continue;

                double distance = Math.Abs(frac - 0.5);

                if (distance < bestDistance - Tolerance ||
                    (Math.Abs(distance - bestDistance) <= Tolerance && variable < bestVariable))
                {
                    bestDistance = distance;
                    bestVariable = variable;
                    bestRow = r;
                }
            }

            return bestRow;
        }

        private static Tableau AppendGomoryCut(Tableau tableau, int sourceRow, int cutNumber)
        {
            int oldRowCount = tableau.RowCount;
            int oldColCount = tableau.ColCount;
            int oldObjectiveRow = tableau.ObjectiveRow;
            int oldRhsColumn = tableau.RhsColumn;

            int newRowCount = oldRowCount + 1;
            int newColCount = oldColCount + 1;
            int cutRowIndex = oldObjectiveRow;
            int gomoryColumn = oldRhsColumn;
            int newRhsColumn = newColCount - 1;

            var newMatrix = new double[newRowCount, newColCount];
            var newBasic = new int[newRowCount - 1];
            var newLabels = new string[newColCount];

            for (int c = 0; c < oldRhsColumn; c++)
                newLabels[c] = tableau.ColumnLabels[c];
            newLabels[gomoryColumn] = $"g{cutNumber}";
            newLabels[newRhsColumn] = "RHS";

            for (int r = 0; r < oldRowCount; r++)
            {
                int destRow = r < oldObjectiveRow ? r : r + 1;

                for (int c = 0; c < oldRhsColumn; c++)
                    newMatrix[destRow, c] = tableau.Matrix[r, c];

                newMatrix[destRow, newRhsColumn] = tableau.Matrix[r, oldRhsColumn];

                if (r < oldObjectiveRow)
                    newBasic[destRow] = tableau.BasicVariableIndices[r];
            }

            for (int c = 0; c < oldRhsColumn; c++)
                newMatrix[cutRowIndex, c] = -FractionalPart(tableau.Matrix[sourceRow, c]);

            newMatrix[cutRowIndex, gomoryColumn] = 1;
            newMatrix[cutRowIndex, newRhsColumn] = -FractionalPart(tableau.Matrix[sourceRow, oldRhsColumn]);
            newBasic[cutRowIndex] = gomoryColumn;

            var newTableau = new Tableau(newRowCount - 1, newColCount)
            {
                Matrix = newMatrix,
                BasicVariableIndices = newBasic,
                ColumnLabels = newLabels
            };

            return newTableau;
        }

        private static double[] ExtractVariableValues(Tableau tableau, int n)
        {
            var values = new double[n];

            for (int i = 0; i < n; i++)
            {
                int row = Array.IndexOf(tableau.BasicVariableIndices, i);
                values[i] = row >= 0 ? tableau.Matrix[row, tableau.RhsColumn] : 0;
            }

            return values;
        }

        private static int CountArtificials(LPModel model)
        {
            int count = 0;
            foreach (var c in model.Constraints)
                if (c.Relation != ConstraintRelation.LessThanOrEqual) count++;
            return count;
        }

        private static double FractionalPart(double value)
        {
            double frac = value - Math.Floor(value);
            if (frac < 0) frac += 1;
            return frac;
        }
    }
}
