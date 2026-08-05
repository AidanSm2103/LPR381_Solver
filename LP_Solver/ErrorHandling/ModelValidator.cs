using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

namespace LP_Solver.ErrorHandling
{
    public static class ModelValidator
    {
        // TODO (whoever owns each algorithm, or a shared owner): called during/after
        // the simplex pivot loop — no valid ratio-test row = unbounded; an artificial
        // variable stuck in the basis at the end = infeasible.
        public static bool IsUnbounded(Tableau tableau)
        {
            return false;
        }

        public static bool IsInfeasible(Tableau tableau)
        {
            return false;
        }
    }
}
