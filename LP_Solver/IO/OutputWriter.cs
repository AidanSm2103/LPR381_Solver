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
    public static class OutputWriter
    {
        // IterationLog already includes the canonical form as its first entry
        // (each solver adds it via Core.CanonicalFormBuilder/TableauFormatter
        // before its first pivot), so this just streams everything to file in order.
        public static void Write(string filePath, SolverResult result)
        {
            using var writer = new StreamWriter(filePath);

            writer.WriteLine($"Algorithm: {result.AlgorithmName}");
            writer.WriteLine($"Status: {result.Status}");
            writer.WriteLine();

            foreach (var line in result.IterationLog)
                writer.WriteLine(line);

            writer.WriteLine();
            writer.WriteLine($"Optimal Objective Value: {result.ObjectiveValue:F3}");

            for (int i = 0; i < result.VariableValues.Length; i++)
                writer.WriteLine($"x{i + 1} = {result.VariableValues[i]:F3}");
        }
    }
}

