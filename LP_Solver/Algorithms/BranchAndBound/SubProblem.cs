using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Core;
using LP_Solver.Models;

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
        //The LP model belonging to this particular node
        public LPModel? Model { get; set; }

        //Tableau that is produced when the model is solved         
        public Tableau? Tableau { get; set; }

        //Parent node in the branch and bound tree
        public SubProblem? Parent { get; set; }

        //Current state of this node
        public SubProblemStatus Status { get; set; } = SubProblemStatus.Active;

        //LP relxation objective value
        public double Bound { get; set; }

        //Information about the branch that created this node
        public int BranchVariableIndex { get; set; } = -1;

        public ConstraintRelation? BranchRelation {get; set;}

        public double BranchValue {get; set;}

        //Depth in the branch and bound three
        public int Depth {get;  set;}

        // TODO: add whatever branching constraint info is needed (e.g. which
        // variable was branched on, and the added <= / >= bound)
    }
}
