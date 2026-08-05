using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.NonLinear
{
    // TODO (bonus): solve a non-linear problem, e.g. f(x) = x^2, with any algorithm.
    // Spec requires you to explain the code for this part specifically in the video.
    public class NonLinearSolver : ISolver
    {
        public string Name => "Non-Linear (Bonus)";

        public SolverResult Solve(LPModel model)
        {
            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: implement whichever numerical method you choose (e.g. gradient descent)
            result.IterationLog.Add("TODO: Non-linear bonus solver not implemented yet.");

            return result;
        }
    }
}
