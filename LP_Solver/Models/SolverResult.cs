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

    // TODO (whoever owns each algorithm): populate this at the end of Solve().
    // IterationLog should hold one already-formatted string per tableau iteration
    // (use Core/TableauFormatter for that), so OutputWriter and the console
    // can both just print each entry in order without knowing algorithm internals.
    public class SolverResult
    {
        public SolverStatus Status { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; } = System.Array.Empty<double>();
        public List<string> IterationLog { get; set; } = new();
        public string AlgorithmName { get; set; } = "";
    }
}
