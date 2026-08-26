using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LP_Solver.Models;

namespace LP_Solver.Algorithms.BranchAndBound
{
    public class BranchAndBoundKnapsackSolver : ISolver
    {
            private const double Tolerance = 1e-6;

            private double _bestObjective;
            private double[] _bestSolution = Array.Empty<double>();
            private bool _hasBestCandidate;

            public string Name => "Branch & Bound Knapsack";

            public SolverResult Solve(LPModel model)
            {
                var result = new SolverResult
                {
                    AlgorithmName = Name,
                    Status = SolverStatus.Infeasible
                };

                // 1. Initialization
                _bestObjective = model.Objective.Type == ObjectiveType.Max
                    ? double.NegativeInfinity
                    : double.PositiveInfinity;

                _bestSolution = new double[model.Objective.Coefficients.Length];
                _hasBestCandidate = false;

                result.IterationLog.Add("==================================================");
                result.IterationLog.Add(Name);
                result.IterationLog.Add("==================================================");
                result.IterationLog.Add("Initializing root Sub-Problem...");

                // 2. Setup Root Node
                var root = new SubProblem
                {
                    Model = model,
                    Parent = null,
                    Depth = 0,
                    BranchVariableIndex = -1,
                    Status = SubProblemStatus.Active
                };

                // 3. Begin Recursive Branch & Bound Search
                Branch(root, result);

                // 4. Finalize Output
                if (!_hasBestCandidate)
                {
                    result.Status = SolverStatus.Infeasible;
                    result.IterationLog.Add("No integer solution found.");
                    return result;
                }

                result.Status = SolverStatus.Optimal;
                result.ObjectiveValue = _bestObjective;
                result.VariableValues = _bestSolution;

                result.IterationLog.Add("");
                result.IterationLog.Add("==================================================");
                result.IterationLog.Add("BEST CANDIDATE");
                result.IterationLog.Add("==================================================");

                for (int i = 0; i < _bestSolution.Length; i++)
                {
                    result.IterationLog.Add($"x{i + 1} = {_bestSolution[i]:0.###}");
                }

                result.IterationLog.Add($"Objective Value = {_bestObjective:0.###}");

                return result;
            }

            private void Branch(SubProblem node, SolverResult finalResult)
            {
                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add("--------------------------------------------------");
                finalResult.IterationLog.Add($"Node Depth: {node.Depth}");
                finalResult.IterationLog.Add("--------------------------------------------------");

                if (node.BranchVariableIndex >= 0)
                {
                    string relation = node.BranchRelation == ConstraintRelation.LessThanOrEqual ? "<=" : ">=";
                    finalResult.IterationLog.Add($"Branch constraint: x{node.BranchVariableIndex + 1} {relation} {node.BranchValue:0.###}");
                }
                else
                {
                    finalResult.IterationLog.Add("Root node");
                }

                if (node.Model == null)
                {
                    node.Status = SubProblemStatus.Infeasible;
                    finalResult.IterationLog.Add("Node has no model. Marking as infeasible.");
                    return;
                }

                // Calculate theoretical maximum via Knapsack Greedy Ratio-Based Bound
                node.Bound = CalculateRatioBound(node.Model, out bool isFeasible, out bool isInteger, out double currentProfit, out double[] candidateSolution);

                finalResult.IterationLog.Add($"Calculated Node Bound: {node.Bound:0.###}");

                // --------------------------------------------------
                // FATHOM 1: INFEASIBLE
                // --------------------------------------------------
                if (!isFeasible)
                {
                    node.Status = SubProblemStatus.Infeasible;
                    finalResult.IterationLog.Add("Node violates capacity constraints. Marking as fathomed.");
                    return;
                }

                // --------------------------------------------------
                // FATHOM 2: BOUND
                // --------------------------------------------------
                if (_hasBestCandidate && CannotImproveBest(node.Bound, node.Model.Objective.Type))
                {
                    node.Status = SubProblemStatus.Fathomed;
                    finalResult.IterationLog.Add($"Node Bound ({node.Bound:0.###}) cannot improve Best Objective ({_bestObjective:0.###}). Marking as fathomed.");
                    return;
                }

                // --------------------------------------------------
                // CHECK FOR INTEGER SOLUTION (Leaf Node)
                // --------------------------------------------------
                if (isInteger)
                {
                    node.Status = SubProblemStatus.Integer;
                    finalResult.IterationLog.Add("Node is integer feasible (Leaf reached).");
                    finalResult.IterationLog.Add($"Candidate objective = {currentProfit:0.###}");

                    UpdateBestCandidate(currentProfit, candidateSolution, node.Model.Objective.Type, finalResult);

                    node.Status = SubProblemStatus.Fathomed;
                    finalResult.IterationLog.Add("Integer Node is fathomed.");
                    return;
                }

                // --------------------------------------------------
                // BRANCH
                // --------------------------------------------------
                int fractionalVariable = FindNextUnfixedVariable(node.Model);

                if (fractionalVariable == -1)
                {
                    finalResult.IterationLog.Add("No remaining variables to branch on. Fathoming.");
                    return;
                }

                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add($"Branching on variable x{fractionalVariable + 1}");
                finalResult.IterationLog.Add("Creating two child sub-problems");

                // Greedily explore the inclusion branch (>= 1) first to establish a high best objective quickly
                finalResult.IterationLog.Add($"Child 1: x{fractionalVariable + 1} >= 1");
                finalResult.IterationLog.Add($"Child 2: x{fractionalVariable + 1} <= 0");

                var upperChild = CreateChild(node, fractionalVariable, ConstraintRelation.GreaterThanOrEqual, 1.0);
                var lowerChild = CreateChild(node, fractionalVariable, ConstraintRelation.LessThanOrEqual, 0.0);

                // --------------------------------------------------
                // Backtracking (Depth-First Search)
                // --------------------------------------------------
                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add("Exploring upper branch (x = 1)...");
                Branch(upperChild, finalResult);

                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add($"Backtracking to depth {node.Depth}.");
                finalResult.IterationLog.Add("Exploring lower branch (x = 0)...");
                Branch(lowerChild, finalResult);

                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add($"Finished all branches below depth {node.Depth}.");
            }

            private SubProblem CreateChild(SubProblem parent, int variableIndex, ConstraintRelation relation, double value)
            {
                var childModel = CloneModel(parent.Model);
                var coefficients = new double[childModel.Objective.Coefficients.Length];
                coefficients[variableIndex] = 1.0;

                childModel.Constraints.Add(new Constraint { Coefficients = coefficients, Relation = relation, Rhs = value });

                return new SubProblem
                {
                    Model = childModel,
                    Parent = parent,
                    Depth = parent.Depth + 1,
                    BranchVariableIndex = variableIndex,
                    BranchRelation = relation,
                    BranchValue = value,
                    Status = SubProblemStatus.Active
                };
            }

            private double CalculateRatioBound(LPModel model, out bool isFeasible, out bool isInteger, out double currentProfit, out double[] candidateSolution)
            {
                int varCount = model.Objective.Coefficients.Length;
                candidateSolution = new double[varCount];

                // Assume Knapsack standard: Constraint 0 is the capacity limit
                var weights = model.Constraints[0].Coefficients;
                double capacity = model.Constraints[0].Rhs;
                var values = model.Objective.Coefficients;

                double accumulatedWeight = 0;
                currentProfit = 0;

                // Track state of variables based on accumulated constraints
                var states = new int?[varCount];
                foreach (var c in model.Constraints.Skip(1)) // Skip main capacity constraint
                {
                    int varIndex = Array.FindIndex(c.Coefficients, val => val > Tolerance);
                    if (varIndex >= 0)
                    {
                        if (c.Relation == ConstraintRelation.GreaterThanOrEqual && c.Rhs > Tolerance) states[varIndex] = 1; // Forced Include
                        if (c.Relation == ConstraintRelation.LessThanOrEqual && c.Rhs < Tolerance) states[varIndex] = 0;    // Forced Exclude
                        if (c.Relation == ConstraintRelation.Equal && c.Rhs < Tolerance) states[varIndex] = 0;              // Exact Exclude 
                    }
                }

                // 1. Process forced inclusions first
                for (int i = 0; i < varCount; i++)
                {
                    if (states[i] == 1)
                    {
                        accumulatedWeight += weights[i];
                        currentProfit += values[i];
                        candidateSolution[i] = 1.0;
                    }
                }

                isFeasible = accumulatedWeight <= capacity + Tolerance;
                if (!isFeasible)
                {
                    isInteger = false;
                    return currentProfit;
                }

                // 2. Calculate Greedy Upper Bound for remaining capacity
                double profitBound = currentProfit;
                double spaceLeft = capacity - accumulatedWeight;

                // Generate list of remaining unfixed items, sorted by Value/Weight ratio
                var remainingItems = new List<(int Index, double Ratio)>();
                for (int i = 0; i < varCount; i++)
                {
                    if (states[i] == null && weights[i] > Tolerance)
                    {
                        remainingItems.Add((i, values[i] / weights[i]));
                    }
                }

                remainingItems = remainingItems.OrderByDescending(x => x.Ratio).ToList();

                isInteger = true; // Assume true until we are forced to take a fraction

                foreach (var item in remainingItems)
                {
                    if (weights[item.Index] <= spaceLeft + Tolerance)
                    {
                        // Item fits entirely
                        spaceLeft -= weights[item.Index];
                        profitBound += values[item.Index];
                        candidateSolution[item.Index] = 1.0;
                        currentProfit += values[item.Index]; // It's a whole item, so it counts toward raw profit
                    }
                    else if (spaceLeft > Tolerance)
                    {
                        // Fractional inclusion for upper bound
                        profitBound += spaceLeft * item.Ratio;
                        candidateSolution[item.Index] = spaceLeft / weights[item.Index];
                        isInteger = false; // We took a fraction, so this is NOT an integer solution
                        break;
                    }
                }

                return profitBound;
            }

            private int FindNextUnfixedVariable(LPModel model)
            {
                int varCount = model.Objective.Coefficients.Length;
                var isFixed = new bool[varCount];

                foreach (var c in model.Constraints.Skip(1))
                {
                    int varIndex = Array.FindIndex(c.Coefficients, val => val > Tolerance);
                    if (varIndex >= 0) isFixed[varIndex] = true;
                }

                for (int i = 0; i < varCount; i++)
                {
                    if (!isFixed[i] && model.SignRestrictions[i] == VariableType.Bin)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private bool CannotImproveBest(double bound, ObjectiveType objectiveType)
            {
                return objectiveType == ObjectiveType.Max
                    ? bound <= _bestObjective + Tolerance
                    : bound >= _bestObjective - Tolerance;
            }

            private void UpdateBestCandidate(double candidateProfit, double[] candidateSolution, ObjectiveType objectiveType, SolverResult finalResult)
            {
                bool better = !_hasBestCandidate || (objectiveType == ObjectiveType.Max
                    ? candidateProfit > _bestObjective + Tolerance
                    : candidateProfit < _bestObjective - Tolerance);

                if (!better) return;

                _hasBestCandidate = true;
                _bestObjective = candidateProfit;
                _bestSolution = (double[])candidateSolution.Clone();

                finalResult.IterationLog.Add("");
                finalResult.IterationLog.Add("NEW BEST CANDIDATE");
                finalResult.IterationLog.Add($"Objective = {_bestObjective:0.###}");
            }

            private LPModel CloneModel(LPModel original)
            {
                var clone = new LPModel
                {
                    Objective = original.Objective,
                    SignRestrictions = new List<VariableType>(original.SignRestrictions),
                    SourceFileName = original.SourceFileName
                };

                foreach (var constraint in original.Constraints)
                {
                    clone.Constraints.Add(new Constraint
                    {
                        Coefficients = (double[])constraint.Coefficients.Clone(),
                        Relation = constraint.Relation,
                        Rhs = constraint.Rhs
                    });
                }
                return clone;
            }
    }
}
