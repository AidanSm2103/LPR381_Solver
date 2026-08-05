using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Core
{
    public static class CanonicalFormBuilder
    {
        // TODO (Person 1): add slack/surplus/artificial variables as needed per
        // constraint relation, build the initial Tableau, and produce a
        // human-readable canonical form string for OutputWriter/console display.
        public static Tableau Build(LPModel model)
        {
            return new Tableau();
        }

        // TODO: string representation of the canonical form (spec requires this
        // to be displayed before the algorithm's tableau iterations)
        public static string ToDisplayString(LPModel model)
        {
            return "TODO: canonical form display not implemented yet.";
        }
    }
}
