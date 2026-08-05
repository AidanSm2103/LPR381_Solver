using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;

namespace LP_Solver.Algorithms.BranchAndBound
{
    public enum SubProblemStatus
    {
        Active,
        Fathomed,
        Integer,
        Infeasible
    }

    // TODO (Person 2 & Person 3): shared node structure — align on this together
    // since Branch & Bound Simplex and Branch & Bound Knapsack both branch/fathom
    // the same way, just over different underlying models.
    public class SubProblem
    {
        public Tableau? Tableau { get; set; }
        public SubProblem? Parent { get; set; }
        public SubProblemStatus Status { get; set; } = SubProblemStatus.Active;
        public double Bound { get; set; }

        // TODO: add whatever branching constraint info is needed (e.g. which
        // variable was branched on, and the added <= / >= bound)
    }
}
