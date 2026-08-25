using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Algorithms.Simplex;
using LP_Solver.Models;

namespace LP_Solver.SensitivityAnalysis
{
    public static class DualityAnalyzer
    {
        public static string ApplyDuality(LPModel model)
        {
            var dual = BuildDualModel(model);
            return FormatModel(dual);
        }

        public static string SolveDual(LPModel model)
        {
            var dual = BuildDualModel(model);
            var solver = new PrimalSimplexSolver();
            var dualResult = solver.Solve(dual);

            var sb = new StringBuilder();
            sb.AppendLine("Dual model:");
            sb.AppendLine(FormatModel(dual));
            sb.AppendLine($"Status: {dualResult.Status}");
            sb.AppendLine($"Dual objective value: {dualResult.ObjectiveValue:F3}");

            for (int i = 0; i < dualResult.VariableValues.Length; i++)
                sb.AppendLine($"y{i + 1} = {dualResult.VariableValues[i]:F3}");

            return sb.ToString();
        }

        public static string VerifyDuality(LPModel model, SolverResult primalResult, SolverResult dualResult)
        {
            if (primalResult.Status != SolverStatus.Optimal)
                return "Primal result is not optimal — cannot verify duality.";

            var dual = BuildDualModel(model);
            var solver = new PrimalSimplexSolver();
            var solvedDual = solver.Solve(dual);

            var sb = new StringBuilder();
            sb.AppendLine($"Primal z* = {primalResult.ObjectiveValue:F3}");
            sb.AppendLine($"Dual z*   = {solvedDual.ObjectiveValue:F3}");

            bool strong = solvedDual.Status == SolverStatus.Optimal &&
                          Math.Abs(primalResult.ObjectiveValue - solvedDual.ObjectiveValue) < 1e-4;

            sb.AppendLine(strong
                ? "Strong duality holds: primal and dual optimal objective values are equal."
                : "Objective values differ — either the dual has not reached optimality, or one of the results supplied is stale.");

            return sb.ToString();
        }

        public static LPModel BuildDualModel(LPModel primal)
        {
            int n = primal.Objective.Coefficients.Length;
            int m = primal.Constraints.Count;

            var dual = new LPModel
            {
                Objective = new ObjectiveFunction
                {
                    Type = primal.Objective.Type == ObjectiveType.Max ? ObjectiveType.Min : ObjectiveType.Max,
                    Coefficients = primal.Constraints.Select(c => c.Rhs).ToArray()
                },
                SourceFileName = "dual"
            };

            for (int j = 0; j < n; j++)
            {
                var coefficients = new double[m];
                for (int i = 0; i < m; i++)
                    coefficients[i] = primal.Constraints[i].Coefficients[j];

                var primalSign = j < primal.SignRestrictions.Count ? primal.SignRestrictions[j] : VariableType.Positive;

                dual.Constraints.Add(new Constraint
                {
                    Coefficients = coefficients,
                    Relation = DualConstraintRelation(primalSign, primal.Objective.Type),
                    Rhs = primal.Objective.Coefficients[j]
                });
            }

            for (int i = 0; i < m; i++)
                dual.SignRestrictions.Add(DualVariableSign(primal.Constraints[i].Relation, primal.Objective.Type));

            return dual;
        }

        private static VariableType DualVariableSign(ConstraintRelation primalRelation, ObjectiveType primalType)
        {
            if (primalType == ObjectiveType.Max)
            {
                return primalRelation switch
                {
                    ConstraintRelation.LessThanOrEqual => VariableType.Positive,
                    ConstraintRelation.GreaterThanOrEqual => VariableType.Negative,
                    _ => VariableType.Urs
                };
            }

            return primalRelation switch
            {
                ConstraintRelation.GreaterThanOrEqual => VariableType.Positive,
                ConstraintRelation.LessThanOrEqual => VariableType.Negative,
                _ => VariableType.Urs
            };
        }

        private static ConstraintRelation DualConstraintRelation(VariableType primalSign, ObjectiveType primalType)
        {
            if (primalType == ObjectiveType.Max)
            {
                return primalSign switch
                {
                    VariableType.Negative => ConstraintRelation.LessThanOrEqual,
                    VariableType.Urs => ConstraintRelation.Equal,
                    _ => ConstraintRelation.GreaterThanOrEqual
                };
            }

            return primalSign switch
            {
                VariableType.Negative => ConstraintRelation.GreaterThanOrEqual,
                VariableType.Urs => ConstraintRelation.Equal,
                _ => ConstraintRelation.LessThanOrEqual
            };
        }

        private static string FormatModel(LPModel model)
        {
            var sb = new StringBuilder();
            string type = model.Objective.Type == ObjectiveType.Max ? "max" : "min";
            sb.AppendLine($"{type} z = " + string.Join(" ", model.Objective.Coefficients.Select((c, i) => $"{(c >= 0 ? "+" : "")}{c:F3}y{i + 1}")));

            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var c = model.Constraints[i];
                string relation = c.Relation switch
                {
                    ConstraintRelation.LessThanOrEqual => "<=",
                    ConstraintRelation.GreaterThanOrEqual => ">=",
                    _ => "="
                };
                sb.AppendLine(string.Join(" ", c.Coefficients.Select((v, j) => $"{(v >= 0 ? "+" : "")}{v:F3}y{j + 1}")) + $" {relation} {c.Rhs:F3}");
            }

            sb.AppendLine(string.Join(" ", model.SignRestrictions.Select((s, i) => $"y{i + 1}: {s}")));

            return sb.ToString();
        }
    }
}
