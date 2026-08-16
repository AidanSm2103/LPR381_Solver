using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LP_Solver.Models
{
    public enum SolverStatus
    {
        Optimal,
        Infeasible,
        Unbounded
    }

    // Output of any ISolver. IterationLog holds one already-formatted string per
    // tableau/iteration state (via Core.TableauFormatter), so OutputWriter and the
    // console can both just print each entry in order without algorithm-specific logic.
    public class SolverResult
    {
        public SolverStatus Status { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; } = System.Array.Empty<double>();
        public List<string> IterationLog { get; set; } = new();
        public string AlgorithmName { get; set; } = "";
    }
}
