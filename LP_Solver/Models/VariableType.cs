using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.Models
{
    // Sign restriction per decision variable, from the last line of the input file:
    // +, -, urs, int, bin
    public enum VariableType
    {
        Positive,
        Negative,
        Urs,
        Int,
        Bin
    }
}
