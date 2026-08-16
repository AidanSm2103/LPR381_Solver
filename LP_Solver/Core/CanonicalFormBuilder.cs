using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Core
{
    // Converts an LPModel into the initial Big-M simplex tableau:
    // <= constraints get a slack column, >= constraints get a surplus + artificial
    // column, = constraints get an artificial column. Internally always solved as a
    // maximization (min problems have their objective coefficients negated here,
    // and the solver negates the final objective value back).
    public static class CanonicalFormBuilder
    {
        public const double BigM = 1_000_000;

        public static Tableau Build(LPModel model)
        {
            int n = model.Objective.Coefficients.Length;
            int m = model.Constraints.Count;

            int slackCount = 0, surplusCount = 0, artificialCount = 0;
            foreach (var c in model.Constraints)
            {
                switch (c.Relation)
                {
                    case ConstraintRelation.LessThanOrEqual: slackCount++; break;
                    case ConstraintRelation.GreaterThanOrEqual: surplusCount++; artificialCount++; break;
                    case ConstraintRelation.Equal: artificialCount++; break;
                }
            }

            int totalVars = n + slackCount + surplusCount + artificialCount;
            var tableau = new Tableau(m, totalVars + 1);

            int col = 0;
            for (int i = 0; i < n; i++) tableau.ColumnLabels[col++] = $"x{i + 1}";
            for (int i = 0; i < slackCount; i++) tableau.ColumnLabels[col++] = $"s{i + 1}";
            for (int i = 0; i < surplusCount; i++) tableau.ColumnLabels[col++] = $"e{i + 1}";
            for (int i = 0; i < artificialCount; i++) tableau.ColumnLabels[col++] = $"a{i + 1}";
            tableau.ColumnLabels[col] = "RHS";

            int slackStart = n;
            int surplusStart = n + slackCount;
            int artificialStart = n + slackCount + surplusCount;

            int slackIdx = 0, surplusIdx = 0, artificialIdx = 0;

            for (int r = 0; r < m; r++)
            {
                var c = model.Constraints[r];

                for (int i = 0; i < n; i++)
                    tableau.Matrix[r, i] = c.Coefficients[i];

                switch (c.Relation)
                {
                    case ConstraintRelation.LessThanOrEqual:
                        tableau.Matrix[r, slackStart + slackIdx] = 1;
                        tableau.BasicVariableIndices[r] = slackStart + slackIdx;
                        slackIdx++;
                        break;

                    case ConstraintRelation.GreaterThanOrEqual:
                        tableau.Matrix[r, surplusStart + surplusIdx] = -1;
                        surplusIdx++;
                        tableau.Matrix[r, artificialStart + artificialIdx] = 1;
                        tableau.BasicVariableIndices[r] = artificialStart + artificialIdx;
                        artificialIdx++;
                        break;

                    case ConstraintRelation.Equal:
                        tableau.Matrix[r, artificialStart + artificialIdx] = 1;
                        tableau.BasicVariableIndices[r] = artificialStart + artificialIdx;
                        artificialIdx++;
                        break;
                }

                tableau.Matrix[r, tableau.RhsColumn] = c.Rhs;
            }

            double sign = model.Objective.Type == ObjectiveType.Min ? -1 : 1;

            for (int i = 0; i < n; i++)
                tableau.Matrix[m, i] = -sign * model.Objective.Coefficients[i];

            for (int i = 0; i < artificialCount; i++)
                tableau.Matrix[m, artificialStart + i] = BigM;

            // Zero out the artificial columns' reduced cost in the objective row,
            // since they start out basic (row-reduce the Big-M penalty out).
            for (int r = 0; r < m; r++)
            {
                int basicCol = tableau.BasicVariableIndices[r];
                if (basicCol >= artificialStart)
                {
                    for (int c2 = 0; c2 < tableau.ColCount; c2++)
                        tableau.Matrix[m, c2] -= BigM * tableau.Matrix[r, c2];
                }
            }

            return tableau;
        }

        public static string ToDisplayString(LPModel model)
        {
            return TableauFormatter.Format(Build(model), 0, "Canonical Form (Initial Tableau)");
        }
    }
}
