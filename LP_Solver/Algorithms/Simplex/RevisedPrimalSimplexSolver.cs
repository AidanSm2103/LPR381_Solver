using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.Simplex
{
    // TODO (Person 1): implement the Revised Primal Simplex Algorithm.
    // Spec requires: display canonical form + all Product Form and Price Out iterations.
    public class RevisedPrimalSimplexSolver : ISolver
    {
        public string Name => "Revised Primal Simplex";

        public SolverResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.Build(model);

            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: product form / price out iteration loop
            result.IterationLog.Add("TODO: Revised Primal Simplex not implemented yet.");

            return result;
        }
    }
}
