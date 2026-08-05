using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.BranchAndBound
{
    // TODO (Person 3): implement Branch & Bound Knapsack algorithm.
    // Spec requires: backtracking, create ALL possible sub-problems, fathom all nodes,
    // display all table iterations, display best candidate. Coordinate with
    // BranchAndBoundSimplexSolver on shared branch/fathom structure (see SubProblem.cs).
    public class BranchAndBoundKnapsackSolver : ISolver
    {
        public string Name => "Branch & Bound Knapsack";

        public SolverResult Solve(LPModel model)
        {
            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: knapsack-specific branch and bound (bounding via LP relaxation
            // or ratio-based bound, branch on binary variables)
            result.IterationLog.Add("TODO: Branch & Bound Knapsack not implemented yet.");

            return result;
        }
    }
}
