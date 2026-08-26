using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class SensitivityAnalyzer
    {
        internal const double Tolerance = 1e-6;

        public static string RangeNonBasicVariable(SolverResult result, int variableIndex)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            if (Array.IndexOf(tableau.BasicVariableIndices, j) >= 0)
                return $"x{variableIndex} is currently basic — use the basic-variable ranging option instead.";

            double reducedCost = tableau.Matrix[tableau.ObjectiveRow, j];
            double current = model.Objective.Coefficients[j];

            var (low, high) = ToOriginalRange(double.NegativeInfinity, InternalCoefficient(model, current) + reducedCost, model.Objective.Type);

            return $"c{variableIndex} range: [{FormatBound(low)}, {FormatBound(high)}]  (current value: {current:F3})";
        }

        public static string ApplyNonBasicVariableChange(SolverResult result, int variableIndex, double newValue)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            if (Array.IndexOf(tableau.BasicVariableIndices, j) >= 0)
                return $"x{variableIndex} is currently basic — use the basic-variable apply-change option instead.";

            double oldInternal = InternalCoefficient(model, model.Objective.Coefficients[j]);
            double newInternal = InternalCoefficient(model, newValue);
            double delta = newInternal - oldInternal;

            tableau.Matrix[tableau.ObjectiveRow, j] -= delta;
            model.Objective.Coefficients[j] = newValue;

            var log = new List<string>();
            log.Add($"c{variableIndex} changed to {newValue:F3}. New reduced cost: {tableau.Matrix[tableau.ObjectiveRow, j]:F3}");

            bool reoptimized = ContinuePrimal(tableau, log);
            AppendResultLog(result, log);

            return reoptimized
                ? $"Change applied and re-optimized.\n{string.Join("\n", log)}"
                : $"Change applied. Still optimal — no re-optimization needed.\n{string.Join("\n", log)}";
        }

        public static string RangeBasicVariable(SolverResult result, int variableIndex)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            int row = Array.IndexOf(tableau.BasicVariableIndices, j);
            if (row < 0)
                return $"x{variableIndex} is currently non-basic — use the non-basic-variable ranging option instead.";

            double allowedIncrease = double.PositiveInfinity;
            double allowedDecrease = double.PositiveInfinity;

            for (int c = 0; c < tableau.RhsColumn; c++)
            {
                if (Array.IndexOf(tableau.BasicVariableIndices, c) >= 0) continue;

                double a = tableau.Matrix[row, c];
                double rj = tableau.Matrix[tableau.ObjectiveRow, c];

                if (a > Tolerance)
                {
                    double bound = rj / a;
                    if (bound < allowedDecrease) allowedDecrease = bound;
                }
                else if (a < -Tolerance)
                {
                    double bound = rj / -a;
                    if (bound < allowedIncrease) allowedIncrease = bound;
                }
            }

            double current = model.Objective.Coefficients[j];
            double currentInternal = InternalCoefficient(model, current);

            double internalLow = currentInternal - allowedDecrease;
            double internalHigh = currentInternal + allowedIncrease;

            var (low, high) = ToOriginalRange(internalLow, internalHigh, model.Objective.Type);

            return $"c{variableIndex} range: [{FormatBound(low)}, {FormatBound(high)}]  (current value: {current:F3})";
        }

        public static string ApplyBasicVariableChange(SolverResult result, int variableIndex, double newValue)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            int row = Array.IndexOf(tableau.BasicVariableIndices, j);
            if (row < 0)
                return $"x{variableIndex} is currently non-basic — use the non-basic-variable apply-change option instead.";

            double oldInternal = InternalCoefficient(model, model.Objective.Coefficients[j]);
            double newInternal = InternalCoefficient(model, newValue);
            double delta = newInternal - oldInternal;

            for (int c = 0; c < tableau.ColCount; c++)
            {
                if (c == j) continue;
                tableau.Matrix[tableau.ObjectiveRow, c] += delta * tableau.Matrix[row, c];
            }

            model.Objective.Coefficients[j] = newValue;

            var log = new List<string>();
            log.Add($"c{variableIndex} changed to {newValue:F3}.");

            bool reoptimized = ContinuePrimal(tableau, log);
            AppendResultLog(result, log);

            return reoptimized
                ? $"Change applied and re-optimized.\n{string.Join("\n", log)}"
                : $"Change applied. Still optimal — no re-optimization needed.\n{string.Join("\n", log)}";
        }

        public static string RangeConstraintRhs(SolverResult result, int constraintIndex)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            var tableau = result.FinalTableau!;
            var model = result.Model!;
            int i = constraintIndex - 1;

            if (i < 0 || i >= model.Constraints.Count)
                return $"Constraint index {constraintIndex} is out of range.";

            var binvColumn = RecoverBinvColumn(model, tableau, i);

            double allowedIncrease = double.PositiveInfinity;
            double allowedDecrease = double.PositiveInfinity;

            for (int r = 0; r < tableau.RowCount - 1; r++)
            {
                double coeff = binvColumn[r];
                double rhs = tableau.Matrix[r, tableau.RhsColumn];

                if (coeff > Tolerance)
                {
                    double bound = rhs / coeff;
                    if (bound < allowedDecrease) allowedDecrease = bound;
                }
                else if (coeff < -Tolerance)
                {
                    double bound = rhs / -coeff;
                    if (bound < allowedIncrease) allowedIncrease = bound;
                }
            }

            double current = model.Constraints[i].Rhs;
            double low = current - allowedDecrease;
            double high = double.IsPositiveInfinity(allowedIncrease) ? double.PositiveInfinity : current + allowedIncrease;

            return $"b{constraintIndex} range: [{FormatBound(low)}, {FormatBound(high)}]  (current value: {current:F3})";
        }

        public static string ApplyConstraintRhsChange(SolverResult result, int constraintIndex, double newValue)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            var tableau = result.FinalTableau!;
            var model = result.Model!;
            int i = constraintIndex - 1;

            if (i < 0 || i >= model.Constraints.Count)
                return $"Constraint index {constraintIndex} is out of range.";

            double delta = newValue - model.Constraints[i].Rhs;
            var binvColumn = RecoverBinvColumn(model, tableau, i);
            double y = GetShadowPrice(model, tableau, i);

            for (int r = 0; r < tableau.RowCount - 1; r++)
                tableau.Matrix[r, tableau.RhsColumn] += delta * binvColumn[r];

            tableau.Matrix[tableau.ObjectiveRow, tableau.RhsColumn] += delta * y;
            model.Constraints[i].Rhs = newValue;

            var log = new List<string>();
            log.Add($"b{constraintIndex} changed to {newValue:F3}.");

            bool reoptimized = ContinueDual(tableau, log);
            AppendResultLog(result, log);

            if (!reoptimized && log.Any(l => l.Contains("infeasible")))
                return $"Change applied — problem became infeasible.\n{string.Join("\n", log)}";

            return reoptimized
                ? $"Change applied and re-optimized.\n{string.Join("\n", log)}"
                : $"Change applied. Still feasible — no re-optimization needed.\n{string.Join("\n", log)}";
        }

        public static string RangeNonBasicColumn(SolverResult result, int variableIndex)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            if (Array.IndexOf(tableau.BasicVariableIndices, j) >= 0)
                return $"x{variableIndex} is currently basic — column ranging only applies to non-basic variables.";

            var sb = new StringBuilder();
            sb.AppendLine($"Column ranging for x{variableIndex} (coefficient in each constraint):");

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                double y = GetShadowPrice(model, tableau, i);
                double aij = model.Constraints[i].Coefficients[j];
                double reducedCost = tableau.Matrix[tableau.ObjectiveRow, j];

                if (Math.Abs(y) < Tolerance)
                {
                    sb.AppendLine($"  a[{i + 1},{variableIndex}]: shadow price is 0 — no binding restriction from this row.");
                    continue;
                }

                double maxIncrease = reducedCost / Math.Abs(y);
                double newBound = y > 0 ? aij + maxIncrease : aij - maxIncrease;

                sb.AppendLine($"  a[{i + 1},{variableIndex}] can move from {aij:F3} to {(y > 0 ? "at most " + newBound.ToString("F3") : "at least " + newBound.ToString("F3"))} before the reduced cost turns negative.");
            }

            return sb.ToString();
        }

        public static string ApplyNonBasicColumnChange(SolverResult result, int variableIndex)
        {
            string? error = CheckReady(result);
            if (error != null) return error;

            int j = variableIndex - 1;
            var tableau = result.FinalTableau!;
            var model = result.Model!;

            if (j < 0 || j >= model.Objective.Coefficients.Length)
                return $"Variable index {variableIndex} is out of range.";

            if (Array.IndexOf(tableau.BasicVariableIndices, j) >= 0)
                return $"x{variableIndex} is currently basic — column changes only apply to non-basic variables.";

            Console.Write($"Enter {model.Constraints.Count} new coefficients for x{variableIndex}, one per constraint: ");
            var newColumn = ReadDoubleArray(model.Constraints.Count);

            for (int i = 0; i < model.Constraints.Count; i++)
                model.Constraints[i].Coefficients[j] = newColumn[i];

            var priced = PriceOutColumn(model, tableau, newColumn, model.Objective.Coefficients[j]);

            for (int r = 0; r < tableau.RowCount - 1; r++)
                tableau.Matrix[r, j] = priced.Column[r];
            tableau.Matrix[tableau.ObjectiveRow, j] = priced.ReducedCost;

            var log = new List<string>();
            log.Add($"Column for x{variableIndex} replaced. New reduced cost: {priced.ReducedCost:F3}");

            bool reoptimized = ContinuePrimal(tableau, log);
            AppendResultLog(result, log);

            return reoptimized
                ? $"Change applied and re-optimized.\n{string.Join("\n", log)}"
                : $"Change applied. Still optimal — no re-optimization needed.\n{string.Join("\n", log)}";
        }

        internal static string? CheckReady(SolverResult result)
        {
            if (result.FinalTableau == null || result.Model == null)
                return "No optimal tableau available. Run Primal Simplex, Revised Primal Simplex, or Cutting Plane first.";
            if (result.Status != SolverStatus.Optimal)
                return "The last solve was not optimal — sensitivity analysis needs an optimal tableau.";
            return null;
        }

        internal static double InternalCoefficient(LPModel model, double originalCoefficient)
            => model.Objective.Type == ObjectiveType.Min ? -originalCoefficient : originalCoefficient;

        internal static (double Low, double High) ToOriginalRange(double internalLow, double internalHigh, ObjectiveType type)
        {
            if (type == ObjectiveType.Max)
                return (internalLow, internalHigh);

            return (double.IsPositiveInfinity(internalHigh) ? double.NegativeInfinity : -internalHigh,
                    double.IsNegativeInfinity(internalLow) ? double.PositiveInfinity : -internalLow);
        }

        internal static string FormatBound(double value)
        {
            if (double.IsPositiveInfinity(value)) return "+infinity";
            if (double.IsNegativeInfinity(value)) return "-infinity";
            return value.ToString("F3");
        }

        internal static (int Column, ConstraintRelation Relation)[] BuildConstraintColumnMap(LPModel model)
        {
            int n = model.Objective.Coefficients.Length;
            int slackCount = 0, surplusCount = 0;

            foreach (var c in model.Constraints)
            {
                switch (c.Relation)
                {
                    case ConstraintRelation.LessThanOrEqual: slackCount++; break;
                    case ConstraintRelation.GreaterThanOrEqual: surplusCount++; break;
                }
            }

            int slackStart = n;
            int surplusStart = n + slackCount;
            int artificialStart = n + slackCount + surplusCount;

            var map = new (int Column, ConstraintRelation Relation)[model.Constraints.Count];
            int slackIdx = 0, surplusIdx = 0, artificialIdx = 0;

            for (int r = 0; r < model.Constraints.Count; r++)
            {
                var relation = model.Constraints[r].Relation;

                switch (relation)
                {
                    case ConstraintRelation.LessThanOrEqual:
                        map[r] = (slackStart + slackIdx, relation);
                        slackIdx++;
                        break;

                    case ConstraintRelation.GreaterThanOrEqual:
                        map[r] = (surplusStart + surplusIdx, relation);
                        surplusIdx++;
                        artificialIdx++;
                        break;

                    case ConstraintRelation.Equal:
                        map[r] = (artificialStart + artificialIdx, relation);
                        artificialIdx++;
                        break;
                }
            }

            return map;
        }

        internal static double GetShadowPrice(LPModel model, Tableau tableau, int constraintIndex)
        {
            var map = BuildConstraintColumnMap(model);
            var (column, relation) = map[constraintIndex];
            double raw = tableau.Matrix[tableau.ObjectiveRow, column];

            return relation switch
            {
                ConstraintRelation.LessThanOrEqual => raw,
                ConstraintRelation.GreaterThanOrEqual => -raw,
                ConstraintRelation.Equal => raw - CanonicalFormBuilder.BigM,
                _ => raw
            };
        }

        internal static double[] RecoverBinvColumn(LPModel model, Tableau tableau, int constraintIndex)
        {
            var map = BuildConstraintColumnMap(model);
            var (column, relation) = map[constraintIndex];

            int rows = tableau.RowCount - 1;
            var result = new double[rows];
            double sign = relation == ConstraintRelation.GreaterThanOrEqual ? -1 : 1;

            for (int r = 0; r < rows; r++)
                result[r] = sign * tableau.Matrix[r, column];

            return result;
        }

        internal static double[,] RecoverBinv(LPModel model, Tableau tableau)
        {
            int rows = tableau.RowCount - 1;
            var binv = new double[rows, rows];

            for (int i = 0; i < rows; i++)
            {
                var column = RecoverBinvColumn(model, tableau, i);
                for (int r = 0; r < rows; r++)
                    binv[r, i] = column[r];
            }

            return binv;
        }

        internal static double[] GetBasicCosts(LPModel model, Tableau tableau)
        {
            int rows = tableau.RowCount - 1;
            int n = model.Objective.Coefficients.Length;
            var cb = new double[rows];

            for (int r = 0; r < rows; r++)
            {
                int basicVar = tableau.BasicVariableIndices[r];
                cb[r] = basicVar < n ? InternalCoefficient(model, model.Objective.Coefficients[basicVar]) : 0;
            }

            return cb;
        }

        internal static (double[] Column, double ReducedCost) PriceOutColumn(LPModel model, Tableau tableau, double[] originalColumn, double originalObjectiveCoefficient)
        {
            var binv = RecoverBinv(model, tableau);
            int rows = tableau.RowCount - 1;
            var column = new double[rows];

            for (int r = 0; r < rows; r++)
            {
                double sum = 0;
                for (int k = 0; k < rows; k++)
                    sum += binv[r, k] * originalColumn[k];
                column[r] = sum;
            }

            var cb = GetBasicCosts(model, tableau);
            double cbColumn = 0;
            for (int r = 0; r < rows; r++)
                cbColumn += cb[r] * column[r];

            double reducedCost = cbColumn - InternalCoefficient(model, originalObjectiveCoefficient);

            return (column, reducedCost);
        }

        internal static bool ContinuePrimal(Tableau tableau, List<string> log)
        {
            bool pivoted = false;
            int iteration = 0;

            while (true)
            {
                int enteringCol = tableau.FindEnteringColumn();
                if (enteringCol == -1) return pivoted;

                int leavingRow = tableau.FindLeavingRow(enteringCol);
                if (leavingRow == -1)
                {
                    log.Add("Re-optimization is unbounded.");
                    return pivoted;
                }

                tableau.Pivot(leavingRow, enteringCol);
                pivoted = true;
                iteration++;
                log.Add(TableauFormatter.Format(tableau, iteration, $"Re-optimization — Iteration {iteration}"));

                if (iteration > 200)
                {
                    log.Add("Exceeded max iterations during re-optimization.");
                    return pivoted;
                }
            }
        }

        internal static bool ContinueDual(Tableau tableau, List<string> log)
        {
            bool pivoted = false;
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

                if (leavingRow == -1) return pivoted;

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

                if (enteringCol == -1)
                {
                    log.Add("Dual Simplex could not restore feasibility — problem is infeasible.");
                    return pivoted;
                }

                tableau.Pivot(leavingRow, enteringCol);
                pivoted = true;
                iteration++;
                log.Add(TableauFormatter.Format(tableau, iteration, $"Dual Simplex — Iteration {iteration}"));

                if (iteration > 200)
                {
                    log.Add("Exceeded max iterations during Dual Simplex.");
                    return pivoted;
                }
            }
        }

        internal static double[] ReadDoubleArray(int count)
        {
            var values = new double[count];
            string? line = Console.ReadLine();
            var tokens = (line ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < count; i++)
                values[i] = i < tokens.Length && double.TryParse(tokens[i], out double v) ? v : 0;

            return values;
        }

        internal static void AppendResultLog(SolverResult result, List<string> log)
        {
            foreach (var line in log)
                result.IterationLog.Add(line);
        }
    }
}
