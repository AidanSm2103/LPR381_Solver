using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Core
{
    public static class TableauFormatter
    {
        public static string Format(Tableau tableau, int iterationNumber, string? label = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(label ?? $"Iteration {iterationNumber}");

            sb.Append("Basic".PadRight(8));
            foreach (var colLabel in tableau.ColumnLabels)
                sb.Append(colLabel.PadLeft(10));
            sb.AppendLine();

            for (int r = 0; r < tableau.RowCount; r++)
            {
                string rowLabel = r == tableau.ObjectiveRow
                    ? "z"
                    : tableau.ColumnLabels[tableau.BasicVariableIndices[r]];

                sb.Append(rowLabel.PadRight(8));

                for (int c = 0; c < tableau.ColCount; c++)
                    sb.Append(tableau.Matrix[r, c].ToString("F3").PadLeft(10));

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
