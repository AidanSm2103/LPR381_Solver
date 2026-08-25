using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class ShadowPriceCalculator
    {
        public static string Display(SolverResult result)
        {
            string? error = SensitivityAnalyzer.CheckReady(result);
            if (error != null) return error;

            var tableau = result.FinalTableau!;
            var model = result.Model!;

            var sb = new StringBuilder();
            sb.AppendLine("Shadow prices:");

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                double y = SensitivityAnalyzer.GetShadowPrice(model, tableau, i);
                sb.AppendLine($"  y{i + 1} = {y:F3}");
            }

            return sb.ToString();
        }
    }
}
