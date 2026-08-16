using LP_Solver.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LP_Solver.IO
{
    public static class InputParser
    {
        public static LPModel Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Input file not found: {filePath}");

            var lines = File.ReadAllLines(filePath)
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .ToArray();

            if (lines.Length < 3)
                throw new InvalidDataException(
                    "Input file needs an objective line, at least one constraint line, and a sign restriction line.");

            var model = new LPModel { SourceFileName = Path.GetFileName(filePath) };

            model.Objective = ParseObjective(lines[0]);
            int n = model.Objective.Coefficients.Length;

            for (int i = 1; i < lines.Length - 1; i++)
                model.Constraints.Add(ParseConstraint(lines[i], n));

            model.SignRestrictions = ParseSignRestrictions(lines[^1], n);

            return model;
        }

        private static ObjectiveFunction ParseObjective(string line)
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 2)
                throw new InvalidDataException("Objective line must contain 'max'/'min' followed by coefficients.");

            var type = tokens[0].ToLowerInvariant() switch
            {
                "max" => ObjectiveType.Max,
                "min" => ObjectiveType.Min,
                _ => throw new InvalidDataException($"Objective line must start with 'max' or 'min', found '{tokens[0]}'.")
            };

            var coefficients = tokens.Skip(1).Select(ParseSignedNumber).ToArray();

            return new ObjectiveFunction { Type = type, Coefficients = coefficients };
        }

        private static Constraint ParseConstraint(string line, int expectedVariableCount)
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != expectedVariableCount + 1)
                throw new InvalidDataException(
                    $"Constraint line '{line}' should have {expectedVariableCount} coefficients plus a relation/RHS token, found {tokens.Length} tokens.");

            var coefficients = tokens.Take(expectedVariableCount).Select(ParseSignedNumber).ToArray();
            var (relation, rhs) = ParseRelationToken(tokens[^1]);

            return new Constraint { Coefficients = coefficients, Relation = relation, Rhs = rhs };
        }

        private static List<VariableType> ParseSignRestrictions(string line, int expectedCount)
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != expectedCount)
                throw new InvalidDataException($"Sign restriction line should have {expectedCount} entries, found {tokens.Length}.");

            return tokens.Select(t => t.ToLowerInvariant() switch
            {
                "+" => VariableType.Positive,
                "-" => VariableType.Negative,
                "urs" => VariableType.Urs,
                "int" => VariableType.Int,
                "bin" => VariableType.Bin,
                _ => throw new InvalidDataException($"Unrecognized sign restriction '{t}'.")
            }).ToList();
        }

        // Handles a signed number token like "+2" or "-3.5".
        private static double ParseSignedNumber(string token)
        {
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw new InvalidDataException($"Could not parse '{token}' as a number.");
            return value;
        }

        // Handles a combined relation+RHS token like "<=40", ">=10", or "=5".
        private static (ConstraintRelation relation, double rhs) ParseRelationToken(string token)
        {
            if (token.StartsWith("<="))
                return (ConstraintRelation.LessThanOrEqual, ParseSignedNumber(token[2..]));

            if (token.StartsWith(">="))
                return (ConstraintRelation.GreaterThanOrEqual, ParseSignedNumber(token[2..]));

            if (token.StartsWith("="))
                return (ConstraintRelation.Equal, ParseSignedNumber(token[1..]));

            throw new InvalidDataException($"Could not find a relation (<=, >=, =) at the start of '{token}'.");
        }
    }
}

