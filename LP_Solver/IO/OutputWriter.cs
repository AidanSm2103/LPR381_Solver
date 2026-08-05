using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LP_Solver.Models;

namespace LP_Solver.IO
{
    public static class OutputWriter
    {
        // TODO (Person 1): also write the canonical form once CanonicalFormBuilder exists —
        // spec requires canonical form + all tableau iterations + rounded (3dp) final values.
        public static void Write(string filePath, SolverResult result)
        {
            using var writer = new StreamWriter(filePath);

            writer.WriteLine($"Algorithm: {result.AlgorithmName}");
            writer.WriteLine($"Status: {result.Status}");
            writer.WriteLine();

            // TODO: write canonical form here before the iteration log

            foreach (var line in result.IterationLog)
            {
                writer.WriteLine(line);
            }

            writer.WriteLine();
            writer.WriteLine($"Optimal Objective Value: {result.ObjectiveValue:F3}");

            for (int i = 0; i < result.VariableValues.Length; i++)
            {
                writer.WriteLine($"x{i + 1} = {result.VariableValues[i]:F3}");
            }
        }
    }
}
