using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.NonLinear
{
    // Bonus solver: demonstrates a simple non-linear optimisation method.
    // It builds a quadratic function from the loaded objective coefficients and
    // solves it with gradient descent/ascent, which is easy to explain step-by-step.
    public class NonLinearSolver : ISolver
    {
        public string Name => "Non-Linear (Bonus)";

        private const int MaxIterations = 100;
        private const double StepSize = 0.10;
        private const double Tolerance = 0.0001;

        public SolverResult Solve(LPModel model)
        {
            var result = new SolverResult
            {
                AlgorithmName = Name
            };

            int variableCount = model.Objective.Coefficients.Length;

            if (variableCount == 0)
            {
                result.Status = SolverStatus.Infeasible;
                result.IterationLog.Add("No objective coefficients were found, so the non-linear problem cannot be created.");
                return result;
            }

            if (TryGetBoundedQuartic(model, out double a, out double b, out double c, out double lowerBound, out double upperBound))
                return SolveBoundedQuartic(model, a, b, c, lowerBound, upperBound);

            var values = new double[variableCount];

            result.IterationLog.Add("=============================================");
            result.IterationLog.Add("Non-Linear Bonus Solver");
            result.IterationLog.Add("=============================================");
            result.IterationLog.Add("This solver uses a quadratic non-linear objective built from the loaded coefficients.");

            if (model.Objective.Type == ObjectiveType.Max)
            {
                result.IterationLog.Add("Max problem: maximise sum(c_i*x_i - x_i^2).");
                result.IterationLog.Add("Gradient ascent update: x_i = x_i + step * (c_i - 2*x_i).");
            }
            else
            {
                result.IterationLog.Add("Min problem: minimise sum(x_i^2 + c_i*x_i).");
                result.IterationLog.Add("Gradient descent update: x_i = x_i - step * (2*x_i + c_i).");
            }

            result.IterationLog.Add($"Step size = {FormatNumber(StepSize)}");
            result.IterationLog.Add($"Stopping tolerance = {FormatSetting(Tolerance)}");
            result.IterationLog.Add("");
            result.IterationLog.Add(FormatIteration(0, values, EvaluateObjective(model, values), 0));

            bool converged = false;

            for (int iteration = 1; iteration <= MaxIterations; iteration++)
            {
                double largestChange = ApplyGradientStep(model, values);
                double objectiveValue = EvaluateObjective(model, values);

                result.IterationLog.Add(FormatIteration(iteration, values, objectiveValue, largestChange));

                if (largestChange < Tolerance)
                {
                    converged = true;
                    result.IterationLog.Add($"Converged after {iteration} iterations.");
                    break;
                }
            }

            if (!converged)
            {
                result.IterationLog.Add("Reached the maximum number of iterations; using the best approximation found.");
            }

            result.Status = SolverStatus.Optimal;
            result.VariableValues = values;
            result.ObjectiveValue = EvaluateObjective(model, values);

            result.IterationLog.Add("");
            result.IterationLog.Add("Final non-linear solution:");
            result.IterationLog.Add(FormatVector(values));
            result.IterationLog.Add($"Objective Value = {FormatNumber(result.ObjectiveValue)}");

            return result;
        }

        private static SolverResult SolveBoundedQuartic(
            LPModel model,
            double a,
            double b,
            double c,
            double lowerBound,
            double upperBound)
        {
            var result = new SolverResult
            {
                AlgorithmName = "Non-Linear (Bonus)",
                Status = SolverStatus.Optimal
            };

            var candidates = new List<double> { lowerBound, upperBound };

            if (lowerBound <= 0 && upperBound >= 0)
                candidates.Add(0);

            double stationaryValue = -b / (2 * a);
            if (stationaryValue >= 0)
            {
                double root = Math.Sqrt(stationaryValue);

                if (root >= lowerBound && root <= upperBound)
                    candidates.Add(root);

                if (-root >= lowerBound && -root <= upperBound)
                    candidates.Add(-root);
            }

            candidates = candidates
                .DistinctBy(value => Math.Round(value, 6))
                .OrderBy(value => value)
                .ToList();

            double bestX = candidates[0];
            double bestValue = EvaluateQuartic(a, b, c, bestX);

            foreach (double candidate in candidates)
            {
                double value = EvaluateQuartic(a, b, c, candidate);
                bool isBetter = model.Objective.Type == ObjectiveType.Min
                    ? value < bestValue
                    : value > bestValue;

                if (isBetter)
                {
                    bestX = candidate;
                    bestValue = value;
                }
            }

            result.VariableValues = new[] { bestX };
            result.ObjectiveValue = bestValue;

            result.IterationLog.Add("=============================================");
            result.IterationLog.Add("Non-Linear Bonus Solver");
            result.IterationLog.Add("=============================================");
            result.IterationLog.Add("Detected bounded quartic format.");
            result.IterationLog.Add("Objective coefficients are read as: a, b, c");
            result.IterationLog.Add($"f(x) = {FormatNumber(a)}x^4 + {FormatNumber(b)}x^2 + {FormatNumber(c)}");
            result.IterationLog.Add($"Bounds: {FormatNumber(lowerBound)} <= x <= {FormatNumber(upperBound)}");
            result.IterationLog.Add("");
            result.IterationLog.Add("Derivative:");
            result.IterationLog.Add("f'(x) = 4ax^3 + 2bx");
            result.IterationLog.Add("Critical points come from x = 0 and x^2 = -b / (2a).");
            result.IterationLog.Add("");
            result.IterationLog.Add("Candidate points checked:");

            foreach (double candidate in candidates)
                result.IterationLog.Add($"x = {FormatNumber(candidate)}, f(x) = {FormatNumber(EvaluateQuartic(a, b, c, candidate))}");

            result.IterationLog.Add("");
            result.IterationLog.Add("Best point found:");
            result.IterationLog.Add($"x = {FormatNumber(bestX)}");
            result.IterationLog.Add($"Objective Value = {FormatNumber(bestValue)}");

            return result;
        }

        private static bool TryGetBoundedQuartic(
            LPModel model,
            out double a,
            out double b,
            out double c,
            out double lowerBound,
            out double upperBound)
        {
            a = 0;
            b = 0;
            c = 0;
            lowerBound = double.NegativeInfinity;
            upperBound = double.PositiveInfinity;

            if (model.Objective.Coefficients.Length != 3)
                return false;

            a = model.Objective.Coefficients[0];
            b = model.Objective.Coefficients[1];
            c = model.Objective.Coefficients[2];

            if (Math.Abs(a) < 1e-9)
                return false;

            foreach (var constraint in model.Constraints)
            {
                if (constraint.Coefficients.Length != 3)
                    return false;

                if (Math.Abs(constraint.Coefficients[1]) > 1e-9 ||
                    Math.Abs(constraint.Coefficients[2]) > 1e-9)
                {
                    return false;
                }

                double coefficient = constraint.Coefficients[0];
                if (Math.Abs(coefficient) < 1e-9)
                    continue;

                double bound = constraint.Rhs / coefficient;

                if (constraint.Relation == ConstraintRelation.LessThanOrEqual)
                {
                    if (coefficient > 0)
                        upperBound = Math.Min(upperBound, bound);
                    else
                        lowerBound = Math.Max(lowerBound, bound);
                }
                else if (constraint.Relation == ConstraintRelation.GreaterThanOrEqual)
                {
                    if (coefficient > 0)
                        lowerBound = Math.Max(lowerBound, bound);
                    else
                        upperBound = Math.Min(upperBound, bound);
                }
                else
                {
                    lowerBound = Math.Max(lowerBound, bound);
                    upperBound = Math.Min(upperBound, bound);
                }
            }

            return !double.IsInfinity(lowerBound) &&
                   !double.IsInfinity(upperBound) &&
                   lowerBound <= upperBound;
        }

        private static double EvaluateQuartic(double a, double b, double c, double value)
        {
            return (a * Math.Pow(value, 4)) + (b * value * value) + c;
        }

        private static double ApplyGradientStep(LPModel model, double[] values)
        {
            double largestChange = 0;

            for (int i = 0; i < values.Length; i++)
            {
                double gradient = CalculateGradient(model, values[i], i);
                double change = StepSize * gradient;

                if (model.Objective.Type == ObjectiveType.Max)
                    values[i] += change;
                else
                    values[i] -= change;

                largestChange = Math.Max(largestChange, Math.Abs(change));
            }

            return largestChange;
        }

        private static double CalculateGradient(LPModel model, double value, int index)
        {
            double coefficient = model.Objective.Coefficients[index];

            if (model.Objective.Type == ObjectiveType.Max)
                return coefficient - (2 * value);

            return (2 * value) + coefficient;
        }

        private static double EvaluateObjective(LPModel model, double[] values)
        {
            double total = 0;

            for (int i = 0; i < values.Length; i++)
            {
                double coefficient = model.Objective.Coefficients[i];
                double value = values[i];

                if (model.Objective.Type == ObjectiveType.Max)
                    total += (coefficient * value) - (value * value);
                else
                    total += (value * value) + (coefficient * value);
            }

            return total;
        }

        private static string FormatIteration(int iteration, double[] values, double objectiveValue, double largestChange)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Iteration {iteration}");
            sb.AppendLine(FormatVector(values));
            sb.AppendLine($"Objective Value = {FormatNumber(objectiveValue)}");
            sb.AppendLine($"Largest Change = {FormatNumber(largestChange)}");

            return sb.ToString();
        }

        private static string FormatVector(double[] values)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append($"x{i + 1} = {FormatNumber(values[i])}");
            }

            return sb.ToString();
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static string FormatSetting(double value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
