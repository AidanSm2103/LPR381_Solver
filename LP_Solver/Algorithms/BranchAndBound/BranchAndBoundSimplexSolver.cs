using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.BranchAndBound
{
    // TODO (Person 2): implement Branch & Bound Simplex.
    // Spec requires: backtracking, create ALL possible sub-problems to branch on,
    // fathom all possible nodes, display all table iterations of every sub-problem,
    // and display the best candidate found.
    public class BranchAndBoundSimplexSolver : ISolver
    {
        public string Name => "Branch & Bound Simplex";

        public SolverResult Solve(LPModel model)
        {
            var root = new SubProblem
            {
                Tableau = CanonicalFormBuilder.Build(model)
            };

            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: branch/bound/fathom loop over a stack or queue of SubProblem,
            // logging each sub-problem's tableau iterations, tracking best candidate
            result.IterationLog.Add("TODO: Branch & Bound Simplex not implemented yet.");

            return result;
        }
    }
}
