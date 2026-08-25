using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class ActivityAdder
    {
        public static string AddNewActivity(SolverResult result)
        {
            string? error = SensitivityAnalyzer.CheckReady(result);
            if (error != null) return error;

            var tableau = result.FinalTableau!;
            var model = result.Model!;
            int m = model.Constraints.Count;

            Console.Write($"Enter the new activity's objective coefficient: ");
            double objectiveCoefficient = ReadDouble();

            Console.Write($"Enter {m} constraint coefficients for the new activity, one per constraint: ");
            var originalColumn = SensitivityAnalyzer.ReadDoubleArray(m);

            Console.Write("Enter a name for the new activity (e.g. x{n+1}): ");
            string name = Console.ReadLine() ?? $"x{model.Objective.Coefficients.Length + 1}";

            var (column, reducedCost) = SensitivityAnalyzer.PriceOutColumn(model, tableau, originalColumn, objectiveCoefficient);

            var expanded = AppendColumn(tableau, column, reducedCost, name);

            var newObjectiveCoefficients = model.Objective.Coefficients.Append(objectiveCoefficient).ToArray();
            model.Objective.Coefficients = newObjectiveCoefficients;

            for (int i = 0; i < m; i++)
                model.Constraints[i].Coefficients = model.Constraints[i].Coefficients.Append(originalColumn[i]).ToArray();

            model.SignRestrictions.Add(VariableType.Positive);

            result.FinalTableau = expanded;

            var log = new List<string>();
            log.Add($"New activity {name} added. Priced-out reduced cost: {reducedCost:F3}");

            bool reoptimized = SensitivityAnalyzer.ContinuePrimal(expanded, log);
            SensitivityAnalyzer.AppendResultLog(result, log);

            if (reoptimized)
            {
                int n = model.Objective.Coefficients.Length;
                result.VariableValues = ExtractVariableValues(expanded, n);
                double objectiveValue = expanded.Matrix[expanded.ObjectiveRow, expanded.RhsColumn];
                result.ObjectiveValue = model.Objective.Type == ObjectiveType.Min ? -objectiveValue : objectiveValue;
            }

            return reoptimized
                ? $"New activity added and re-optimized.\n{string.Join("\n", log)}"
                : $"New activity added — not attractive enough to enter the basis.\n{string.Join("\n", log)}";
        }

        private static Tableau AppendColumn(Tableau tableau, double[] column, double reducedCost, string label)
        {
            int oldRowCount = tableau.RowCount;
            int oldColCount = tableau.ColCount;
            int oldRhsColumn = tableau.RhsColumn;

            int newColCount = oldColCount + 1;
            int newColumnIndex = oldRhsColumn;
            int newRhsColumn = newColCount - 1;

            var newMatrix = new double[oldRowCount, newColCount];
            var newLabels = new string[newColCount];

            for (int c = 0; c < oldRhsColumn; c++)
                newLabels[c] = tableau.ColumnLabels[c];
            newLabels[newColumnIndex] = label;
            newLabels[newRhsColumn] = "RHS";

            for (int r = 0; r < oldRowCount; r++)
            {
                for (int c = 0; c < oldRhsColumn; c++)
                    newMatrix[r, c] = tableau.Matrix[r, c];

                newMatrix[r, newRhsColumn] = tableau.Matrix[r, oldRhsColumn];
            }

            for (int r = 0; r < oldRowCount - 1; r++)
                newMatrix[r, newColumnIndex] = column[r];

            newMatrix[tableau.ObjectiveRow, newColumnIndex] = reducedCost;

            return new Tableau(oldRowCount - 1, newColCount)
            {
                Matrix = newMatrix,
                BasicVariableIndices = (int[])tableau.BasicVariableIndices.Clone(),
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

        private static double ReadDouble()
        {
            string? input = Console.ReadLine();
            return double.TryParse(input, out double value) ? value : 0;
        }
    }
}
