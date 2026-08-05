using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LP_Solver.Algorithms;
using LP_Solver.Algorithms.BranchAndBound;
using LP_Solver.Algorithms.CuttingPlane;
using LP_Solver.Algorithms.NonLinear;
using LP_Solver.Algorithms.Simplex;
using LP_Solver.IO;
using LP_Solver.Models;
using LP_Solver.SensitivityAnalysis;

namespace LP_Solver.UI
{
    public class MenuManager
    {
        private LPModel? _currentModel;
        private SolverResult? _lastResult;

        // Wired directly to the real (currently TODO-bodied) classes.
        // As each teammate fills in their Solve() method, this list needs no changes.
        private readonly Dictionary<string, ISolver> _solvers = new()
        {
            ["Primal Simplex"] = new PrimalSimplexSolver(),
            ["Revised Primal Simplex"] = new RevisedPrimalSimplexSolver(),
            ["Branch & Bound Simplex"] = new BranchAndBoundSimplexSolver(),
            ["Branch & Bound Knapsack"] = new BranchAndBoundKnapsackSolver(),
            ["Cutting Plane"] = new CuttingPlaneSolver(),
            ["Non-Linear (Bonus)"] = new NonLinearSolver(),
        };

        public void Run()
        {
            bool exit = false;

            while (!exit)
            {
                var choice = ConsoleHelpers.PrintMenuAndGetChoice("LP381 Solver — Main Menu", new List<string>
                {
                    "Load input file",
                    "Select and run algorithm",
                    "Sensitivity analysis",
                    "View last results",
                    "Save results to output file",
                    "Exit"
                });

                switch (choice)
                {
                    case 1: LoadModel(); break;
                    case 2: RunAlgorithmMenu(); break;
                    case 3: SensitivityAnalysisMenu(); break;
                    case 4: ViewLastResults(); break;
                    case 5: SaveResults(); break;
                    case 6: exit = true; break;
                }
            }

            ConsoleHelpers.PrintInfo("Goodbye.");
        }

        private void LoadModel()
        {
            string path = ConsoleHelpers.ReadNonEmptyLine("Enter path to input file: ");

            try
            {
                _currentModel = InputParser.Parse(path);
                _lastResult = null;
                ConsoleHelpers.PrintSuccess($"Loaded model from '{_currentModel.SourceFileName}'.");
            }
            catch (Exception ex)
            {
                ConsoleHelpers.PrintError(ex.Message);
            }

            ConsoleHelpers.Pause();
        }

        private void RunAlgorithmMenu()
        {
            if (!EnsureModelLoaded()) return;

            var names = new List<string>(_solvers.Keys);
            int choice = ConsoleHelpers.PrintMenuAndGetChoice("Select Algorithm", names);
            string selectedName = names[choice - 1];

            var solver = _solvers[selectedName];

            try
            {
                _lastResult = solver.Solve(_currentModel!);
                ConsoleHelpers.PrintSuccess($"Solved with {solver.Name}. Status: {_lastResult.Status}");
                PrintResultSummary(_lastResult);
            }
            catch (Exception ex)
            {
                ConsoleHelpers.PrintError($"Solve failed: {ex.Message}");
            }

            ConsoleHelpers.Pause();
        }

        private void SensitivityAnalysisMenu()
        {
            if (!EnsureModelLoaded()) return;

            if (_lastResult is null)
            {
                ConsoleHelpers.PrintError("Run an algorithm first — sensitivity analysis needs an optimal tableau.");
                ConsoleHelpers.Pause();
                return;
            }

            var options = new List<string>
            {
                "Range of a non-basic variable",
                "Apply change to a non-basic variable",
                "Range of a basic variable",
                "Apply change to a basic variable",
                "Range of a constraint RHS",
                "Apply change to a constraint RHS",
                "Range of a non-basic variable column",
                "Apply change to a non-basic variable column",
                "Add new activity",
                "Add new constraint",
                "Display shadow prices",
                "Apply duality",
                "Solve dual model",
                "Verify strong/weak duality",
                "Back to main menu"
            };

            int choice = ConsoleHelpers.PrintMenuAndGetChoice("Sensitivity Analysis", options);
            string output;

            switch (choice)
            {
                case 1:
                    int nbIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    output = SensitivityAnalyzer.RangeNonBasicVariable(_lastResult, nbIndex);
                    break;
                case 2:
                    int nbChangeIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    double nbNewValue = ConsoleHelpers.ReadDouble("New value: ");
                    output = SensitivityAnalyzer.ApplyNonBasicVariableChange(_lastResult, nbChangeIndex, nbNewValue);
                    break;
                case 3:
                    int bIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    output = SensitivityAnalyzer.RangeBasicVariable(_lastResult, bIndex);
                    break;
                case 4:
                    int bChangeIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    double bNewValue = ConsoleHelpers.ReadDouble("New value: ");
                    output = SensitivityAnalyzer.ApplyBasicVariableChange(_lastResult, bChangeIndex, bNewValue);
                    break;
                case 5:
                    int rhsIndex = ConsoleHelpers.ReadInt("Constraint index: ", 1, 100);
                    output = SensitivityAnalyzer.RangeConstraintRhs(_lastResult, rhsIndex);
                    break;
                case 6:
                    int rhsChangeIndex = ConsoleHelpers.ReadInt("Constraint index: ", 1, 100);
                    double rhsNewValue = ConsoleHelpers.ReadDouble("New RHS value: ");
                    output = SensitivityAnalyzer.ApplyConstraintRhsChange(_lastResult, rhsChangeIndex, rhsNewValue);
                    break;
                case 7:
                    int colIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    output = SensitivityAnalyzer.RangeNonBasicColumn(_lastResult, colIndex);
                    break;
                case 8:
                    int colChangeIndex = ConsoleHelpers.ReadInt("Variable index: ", 1, 100);
                    output = SensitivityAnalyzer.ApplyNonBasicColumnChange(_lastResult, colChangeIndex);
                    break;
                case 9:
                    output = ActivityAdder.AddNewActivity(_lastResult);
                    break;
                case 10:
                    output = ConstraintAdder.AddNewConstraint(_lastResult);
                    break;
                case 11:
                    output = ShadowPriceCalculator.Display(_lastResult);
                    break;
                case 12:
                    output = DualityAnalyzer.ApplyDuality(_currentModel!);
                    break;
                case 13:
                    output = DualityAnalyzer.SolveDual(_currentModel!);
                    break;
                case 14:
                    output = DualityAnalyzer.VerifyDuality(_currentModel!, _lastResult, _lastResult);
                    break;
                default:
                    return; // back to main menu
            }

            ConsoleHelpers.PrintInfo(output);
            ConsoleHelpers.Pause();
        }

        private void ViewLastResults()
        {
            if (_lastResult is null)
            {
                ConsoleHelpers.PrintError("No results yet — run an algorithm first.");
            }
            else
            {
                PrintResultSummary(_lastResult);
            }

            ConsoleHelpers.Pause();
        }

        private void SaveResults()
        {
            if (_lastResult is null)
            {
                ConsoleHelpers.PrintError("No results yet — run an algorithm first.");
                ConsoleHelpers.Pause();
                return;
            }

            string path = ConsoleHelpers.ReadNonEmptyLine("Enter path for output file: ");

            try
            {
                OutputWriter.Write(path, _lastResult);
                ConsoleHelpers.PrintSuccess($"Results written to '{path}'.");
            }
            catch (Exception ex)
            {
                ConsoleHelpers.PrintError($"Could not write output file: {ex.Message}");
            }

            ConsoleHelpers.Pause();
        }

        private bool EnsureModelLoaded()
        {
            if (_currentModel is not null) return true;

            ConsoleHelpers.PrintError("Load an input file first (Main Menu → Load input file).");
            ConsoleHelpers.Pause();
            return false;
        }

        private static void PrintResultSummary(SolverResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"Algorithm: {result.AlgorithmName}");
            Console.WriteLine($"Status: {result.Status}");
            Console.WriteLine($"Objective Value: {result.ObjectiveValue:F3}");

            for (int i = 0; i < result.VariableValues.Length; i++)
            {
                Console.WriteLine($"x{i + 1} = {result.VariableValues[i]:F3}");
            }
        }
    }
}

