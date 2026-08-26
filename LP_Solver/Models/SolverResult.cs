using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LP_Solver.Core;

namespace LP_Solver.Models
{
    public enum SolverStatus
    {
        Optimal,
        Infeasible,
        Unbounded
    }

    // Output of any ISolver.
    public class SolverResult
    {
        public SolverStatus Status { get; set; }
        public double ObjectiveValue { get; set; }
        public double[] VariableValues { get; set; } = System.Array.Empty<double>();
        public List<string> IterationLog { get; set; } = new();
        public string AlgorithmName { get; set; } = "";

        public Tableau? FinalTableau { get; set; }
        public LPModel? Model { get; set; }
    }
}
