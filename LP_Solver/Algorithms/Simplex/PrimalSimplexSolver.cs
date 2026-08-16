using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.Simplex
{
    // Primal Simplex using the Big-M method — handles <=, >=, and = constraints
    // in one pass without a separate Phase 1 / Phase 2 split.
    public class PrimalSimplexSolver : ISolver
    {
        public string Name => "Primal Simplex";

        private const int MaxIterations = 200;

        public SolverResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.Build(model);
            var result = new SolverResult { AlgorithmName = Name };

            result.IterationLog.Add(TableauFormatter.Format(tableau, 0, "Canonical Form (Initial Tableau)"));

            int iteration = 0;
            while (true)
            {
                int enteringCol = tableau.FindEnteringColumn();
                if (enteringCol == -1) break; // optimal

                int leavingRow = tableau.FindLeavingRow(enteringCol);
                if (leavingRow == -1)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add("No positive ratio found in the entering column — problem is unbounded.");
                    return result;
                }

                tableau.Pivot(leavingRow, enteringCol);
                iteration++;
                result.IterationLog.Add(TableauFormatter.Format(tableau, iteration));

                if (iteration > MaxIterations)
                {
                    result.Status = SolverStatus.Unbounded;
                    result.IterationLog.Add("Exceeded max iterations — check the model for cycling.");
                    return result;
                }
            }

            int n = model.Objective.Coefficients.Length;
            int artificialCount = CountArtificials(model);
            int artificialStart = tableau.ColCount - 1 - artificialCount;

            for (int r = 0; r < tableau.RowCount - 1; r++)
            {
                if (tableau.BasicVariableIndices[r] >= artificialStart &&
                    tableau.Matrix[r, tableau.RhsColumn] > 1e-6)
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("An artificial variable remained in the basis with a positive value — problem is infeasible.");
                    return result;
                }
            }

            result.Status = SolverStatus.Optimal;
            result.VariableValues = ExtractVariableValues(tableau, n);

            double objectiveValue = tableau.Matrix[tableau.ObjectiveRow, tableau.RhsColumn];
            result.ObjectiveValue = model.Objective.Type == ObjectiveType.Min ? -objectiveValue : objectiveValue;

            return result;
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
    }
}