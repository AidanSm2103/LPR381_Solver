using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LP_Solver.Models;

namespace LP_Solver.IO
{
    public static class InputParser
    {
        // TODO (Person 1): implement per the input file spec —
        // Line 1: "max"/"min" + signed objective coefficients (e.g. "max +2 +3 +5")
        // Middle lines: signed constraint coefficients + relation (<=, >=, =) + RHS
        // Last line: sign restrictions, one token per decision variable (+, -, urs, int, bin)
        public static LPModel Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Input file not found: {filePath}");

            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length < 3)
                throw new InvalidDataException("Input file needs an objective line, at least one constraint line, and a sign restriction line.");

            var model = new LPModel
            {
                SourceFileName = Path.GetFileName(filePath)
            };

            // TODO: parse lines[0] into model.Objective
            // TODO: parse lines[1..^1] into model.Constraints
            // TODO: parse lines[^1] into model.SignRestrictions

            return model;
        }
    }
}
