using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class DualityAnalyzer
    {
        // TODO (Person 4): transform the primal model into its dual form.
        public static string ApplyDuality(LPModel model)
            => "TODO: duality transform not implemented yet.";

        // TODO: solve the dual model (can reuse an ISolver, e.g. PrimalSimplexSolver,
        // once ApplyDuality produces a valid LPModel for the dual)
        public static string SolveDual(LPModel model)
            => "TODO: dual solve not implemented yet.";

        // TODO: compare primal and dual objective values to confirm
        // strong duality (equal) vs weak duality
        public static string VerifyDuality(LPModel model, SolverResult primalResult, SolverResult dualResult)
            => "TODO: strong/weak duality check not implemented yet.";
    }
}
