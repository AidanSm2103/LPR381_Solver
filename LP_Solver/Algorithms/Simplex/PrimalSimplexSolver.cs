using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.Simplex
{
    // TODO (Person 1): implement the Primal Simplex Algorithm.
    // Spec requires: display canonical form + all tableau iterations.
    public class PrimalSimplexSolver : ISolver
    {
        public string Name => "Primal Simplex";

        public SolverResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.Build(model);

            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: pivot loop — select entering/leaving variables, pivot,
            // log each iteration via TableauFormatter.Format(), repeat until optimal
            result.IterationLog.Add("TODO: Primal Simplex not implemented yet.");

            return result;
        }
    }
}
