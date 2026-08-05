using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    // TODO (Person 4): implement each method against the final optimal Tableau.
    // Adjust parameters once Tableau.cs is filled in — just keep method names
    // matching what MenuManager calls.
    public static class SensitivityAnalyzer
    {
        public static string RangeNonBasicVariable(SolverResult result, int variableIndex)
            => "TODO: range of non-basic variable not implemented yet.";

        public static string ApplyNonBasicVariableChange(SolverResult result, int variableIndex, double newValue)
            => "TODO: apply non-basic variable change not implemented yet.";

        public static string RangeBasicVariable(SolverResult result, int variableIndex)
            => "TODO: range of basic variable not implemented yet.";

        public static string ApplyBasicVariableChange(SolverResult result, int variableIndex, double newValue)
            => "TODO: apply basic variable change not implemented yet.";

        public static string RangeConstraintRhs(SolverResult result, int constraintIndex)
            => "TODO: range of constraint RHS not implemented yet.";

        public static string ApplyConstraintRhsChange(SolverResult result, int constraintIndex, double newValue)
            => "TODO: apply constraint RHS change not implemented yet.";

        public static string RangeNonBasicColumn(SolverResult result, int variableIndex)
            => "TODO: range of a non-basic variable column not implemented yet.";

        public static string ApplyNonBasicColumnChange(SolverResult result, int variableIndex)
            => "TODO: apply non-basic column change not implemented yet.";
    }
}
