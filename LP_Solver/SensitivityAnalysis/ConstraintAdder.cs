using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class ConstraintAdder
    {
        public static string AddNewConstraint(SolverResult result)
        {
            string? error = SensitivityAnalyzer.CheckReady(result);
            if (error != null) return error;

            var tableau = result.FinalTableau!;
            var model = result.Model!;
            int n = model.Objective.Coefficients.Length;

            Console.Write($"Enter {n} coefficients for the new constraint: ");
            var coefficients = SensitivityAnalyzer.ReadDoubleArray(n);

            Console.Write("Enter relation (<=, >=, =): ");
            string relationToken = (Console.ReadLine() ?? "<=").Trim();

            Console.Write("Enter RHS: ");
            double rhs = double.TryParse(Console.ReadLine(), out double parsedRhs) ? parsedRhs : 0;

            var relation = relationToken switch
            {
                ">=" => ConstraintRelation.GreaterThanOrEqual,
                "=" => ConstraintRelation.Equal,
                _ => ConstraintRelation.LessThanOrEqual
            };

            model.Constraints.Add(new Constraint { Coefficients = coefficients, Relation = relation, Rhs = rhs });

            var (expanded, satisfied, log) = Apply(tableau, coefficients, relation, rhs, model.Constraints.Count);

            result.FinalTableau = expanded;
            SensitivityAnalyzer.AppendResultLog(result, log);

            if (!satisfied)
            {
                result.VariableValues = ExtractVariableValues(expanded, n);
                double objectiveValue = expanded.Matrix[expanded.ObjectiveRow, expanded.RhsColumn];
                result.ObjectiveValue = model.Objective.Type == ObjectiveType.Min ? -objectiveValue : objectiveValue;
            }

            return satisfied
                ? $"New constraint added — already satisfied by the current solution.\n{string.Join("\n", log)}"
                : $"New constraint added — Dual Simplex applied to restore feasibility.\n{string.Join("\n", log)}";
        }

        private static (Tableau Tableau, bool AlreadySatisfied, List<string> Log) Apply(
            Tableau tableau, double[] coefficients, ConstraintRelation relation, double rhs, int constraintNumber)
        {
            var log = new List<string>();

            double sign = relation == ConstraintRelation.GreaterThanOrEqual ? -1 : 1;
            var row = new double[tableau.RhsColumn];
            for (int j = 0; j < coefficients.Length; j++)
                row[j] = sign * coefficients[j];
            double rowRhs = sign * rhs;

            for (int j = 0; j < coefficients.Length; j++)
            {
                if (Math.Abs(row[j]) < SensitivityAnalyzer.Tolerance) continue;

                int basicRow = Array.IndexOf(tableau.BasicVariableIndices, j);
                if (basicRow < 0) continue;

                double factor = row[j];
                for (int c = 0; c < tableau.RhsColumn; c++)
                    row[c] -= factor * tableau.Matrix[basicRow, c];
                rowRhs -= factor * tableau.Matrix[basicRow, tableau.RhsColumn];
            }

            var expanded = AppendRow(tableau, row, rowRhs, constraintNumber);
            log.Add(TableauFormatter.Format(expanded, 0, $"After Adding Constraint {constraintNumber}"));

            bool satisfied = rowRhs >= -SensitivityAnalyzer.Tolerance;

            if (!satisfied)
                SensitivityAnalyzer.ContinueDual(expanded, log);

            return (expanded, satisfied, log);
        }

        private static Tableau AppendRow(Tableau tableau, double[] row, double rowRhs, int constraintNumber)
        {
            int oldRowCount = tableau.RowCount;
            int oldColCount = tableau.ColCount;
            int oldObjectiveRow = tableau.ObjectiveRow;
            int oldRhsColumn = tableau.RhsColumn;

            int newRowCount = oldRowCount + 1;
            int newColCount = oldColCount + 1;
            int newRowIndex = oldObjectiveRow;
            int slackColumn = oldRhsColumn;
            int newRhsColumn = newColCount - 1;

            var newMatrix = new double[newRowCount, newColCount];
            var newBasic = new int[newRowCount - 1];
            var newLabels = new string[newColCount];

            for (int c = 0; c < oldRhsColumn; c++)
                newLabels[c] = tableau.ColumnLabels[c];
            newLabels[slackColumn] = $"s_c{constraintNumber}";
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
                newMatrix[newRowIndex, c] = row[c];

            newMatrix[newRowIndex, slackColumn] = 1;
            newMatrix[newRowIndex, newRhsColumn] = rowRhs;
            newBasic[newRowIndex] = slackColumn;

            return new Tableau(newRowCount - 1, newColCount)
            {
                Matrix = newMatrix,
                BasicVariableIndices = newBasic,
                ColumnLabels = newLabels
            };
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
    }
}
