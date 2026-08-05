using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.CuttingPlane
{
    // TODO (Person 4): implement Cutting Plane Algorithm.
    // Spec requires: display canonical form + all Product Form and Price Out iterations.
    public class CuttingPlaneSolver : ISolver
    {
        public string Name => "Cutting Plane";

        public SolverResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.Build(model);

            var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Optimal
            };

            // TODO: solve LP relaxation, generate Gomory cuts, re-solve, repeat until integer
            result.IterationLog.Add("TODO: Cutting Plane not implemented yet.");

            return result;
        }
    }
}
