using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LP_Solver.Models
{
    // TODO (Person 1): this is the central data structure — InputParser builds it,
    // CanonicalFormBuilder consumes it, every algorithm and SensitivityAnalysis method
    // takes it as input. Avoid changing property names/types without telling the team,
    // since everyone else's code will reference this directly.
    public class LPModel
    {
        public ObjectiveFunction Objective { get; set; } = new();
        public List<Constraint> Constraints { get; set; } = new();
        public List<VariableType> SignRestrictions { get; set; } = new();

        // Filename it was loaded from, for display in the menu/output file.
        public string SourceFileName { get; set; } = "";
    }
}

