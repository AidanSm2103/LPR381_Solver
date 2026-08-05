using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Algorithms
{
    // Every algorithm implements this so MenuManager can call any of them
    // the same way, without knowing which one it's running.
    public interface ISolver
    {
        string Name { get; }
        SolverResult Solve(LPModel model);
    }
}
