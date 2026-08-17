using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LP_Solver.Core;
using LP_Solver.Models;
using LP_Solver.Algorithms.Simplex;

namespace LP_Solver.Algorithms.BranchAndBound
{
    // TODO (Person 2): implement Branch & Bound Simplex.
    // Spec requires: backtracking, create ALL possible sub-problems to branch on,
    // fathom all possible nodes, display all table iterations of every sub-problem,
    // and display the best candidate found.
    public class BranchAndBoundSimplexSolver : ISolver
    {
        private const double Tolerance = 1e-6;

        private double bestObjective;

        private double[] bestSolution = Array.Empty<double>();

        private bool hasBestCandidate;

        public string Name => "Branch & Bound Simplex";

        public SolverResult Solve(LPModel model)
        {
             var result = new SolverResult
            {
                AlgorithmName = Name,
                Status = SolverStatus.Infeasible
            };

            bestObjective = model.Objective.Type == ObjectiveType.Max
                ? double.NegativeInfinity
                : double.PositiveInfinity;

            bestSolution = Array.Empty<double>();
            hasBestCandidate = false;

            result.IterationLog.Add("=============================================");
            result.IterationLog.Add("Branch and Bound Simplex");
            result.IterationLog.Add("=============================================");

            result.IterationLog.Add("Creating root LP relaxation...");

            //Create a copy of the model so that the original model is not modified
            var rootModel = CreateRelaxationModel(model);

            var root = new SubProblem
            {
                Model = rootModel,
                Parent = null,
                Depth = 0,
                Status = SubProblemStatus.Active
            };

            //Start the recursive Branch and Bound process
            Branch(root,result);

            //No integer solution was found
            if(!hasBestCandidate)
            {
                result.Status = SolverStatus.Infeasible;
                result.IterationLog.Add("No integer solution found.");
                return result;
            }

            // Store the best candidate.
            result.Status = SolverStatus.Optimal;
            result.ObjectiveValue = bestObjective;
            result.VariableValues = bestSolution;

            result.IterationLog.Add("");
            result.IterationLog.Add("==================================================");
            result.IterationLog.Add(
                "BEST CANDIDATE");
            result.IterationLog.Add("==================================================");

            for(int i = 0; i < bestSolution.Length; i++)
            {
                result.IterationLog.Add($"x{i + 1} = {bestSolution[i]:0.###}");
            }

            result.IterationLog.Add($"Objective Value = {bestObjective:0.###}");

            // TODO: branch/bound/fathom loop over a stack or queue of SubProblem,
            // logging each sub-problem's tableau iterations, tracking best candidate
            result.IterationLog.Add("TODO: Branch & Bound Simplex not implemented yet.");

            return result;
        }

        //Recursively explores a Branch and Bound node
        private void Branch(SubProblem node, SolverResult finalResult)
        {
            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add("--------------------------------------------------");
            finalResult.IterationLog.Add($"Node Depth: {node.Depth}");
            finalResult.IterationLog.Add("--------------------------------------------------");

            if(node.BranchVariableIndex >= 0)
            {
                string relation = node.BranchRelation == ConstraintRelation.LessThanOrEqual ? "<=" : ">=";

                finalResult.IterationLog.Add(
                    $"Branch constraint: x{node.BranchVariableIndex +1} " + $"{relation} {node.BranchValue:0.###}");
            }
            else
            {
                finalResult.IterationLog.Add("Root node");
            }

            // The node must have a model
            if(node.Model == null)
            {
                node.Status = SubProblemStatus.Infeasible;
                finalResult.IterationLog.Add("Node has no model. Marking as infeasible.");

                return;
            }

            //Solve the LP relaxation using the existing Primal Simplex implementation
            var simplexSolver = new PrimalSimplexSolver(); 

            var relaxationResult = simplexSolver.Solve(node.Model);

            //copy every simplex tableau iteration into the branch and bound output log
            foreach(var logEntry in relaxationResult.IterationLog)
            {
                finalResult.IterationLog.Add($"[Node {node.Depth}] {logEntry}");
            }

            node.Bound = relaxationResult.ObjectiveValue;

            // --------------------------------------------------
            // FATHOM 1: INFEASIBLE
            // --------------------------------------------------

            if(relaxationResult.Status == SolverStatus.Infeasible)
            {
                node.Status = SubProblemStatus.Infeasible;
                finalResult.IterationLog.Add($"Node is infeasible. Marking as fathomed."); 
                return;
            }

            // --------------------------------------------------
            // FATHOM 2: UNBOUNDED
            // --------------------------------------------------

             if(relaxationResult.Status == SolverStatus.Unbounded)
            {
                finalResult.IterationLog.Add($"Node is unbounded. Marking as fathomed.");
                node.Status = SubProblemStatus.Fathomed;
                return;
            }

            // --------------------------------------------------
            // FATHOM 3: Bound
            // --------------------------------------------------
            if(hasBestCandidate && CannotImproveBest(node.Bound, node.Model.Objective.Type))
            {
                node.Status = SubProblemStatus.Fathomed;
                finalResult.IterationLog.Add($"Node Bound = {node.Bound:0.###}");
                finalResult.IterationLog.Add($"Best Objective = {bestObjective:0.###}");
                finalResult.IterationLog.Add($"Node cannot improve best candidate. Marking as fathomed.");
                return;
            }

            // --------------------------------------------------
            // Cbeck for integer solution
            // --------------------------------------------------

            int fractionalVariable = FindFractionalIntegerVariable(node.Model, relaxationResult.VariableValues);

            //No fractional integer - restricted variables
            if(fractionalVariable == -1)
            {
                node.Status = SubProblemStatus.Integer;
                finalResult.IterationLog.Add($"Node is integer feasible");
                finalResult.IterationLog.Add($"Candidate objective = " +$"{relaxationResult.ObjectiveValue:0.###}");

                UpdateBestCandidate(relaxationResult, node.Model, finalResult);

                node.Status = SubProblemStatus.Fathomed;

                finalResult.IterationLog.Add($"Integer Node is fathomed.");

                return;
            }

            // --------------------------------------------------
            // BRANCH
            // --------------------------------------------------

            double fractionalValue = relaxationResult.VariableValues[fractionalVariable];

            double lowerValue = Math.Floor(fractionalValue);
            double upperValue = Math.Ceiling(fractionalValue);

            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add($"Fractional variable selected: " + $"x{fractionalVariable + 1} = {fractionalValue:0.###}");

            finalResult.IterationLog.Add("Creating two child sub-problems");

            finalResult.IterationLog.Add($"Child 1: x{fractionalVariable + 1} <= {lowerValue:0.###}");

            finalResult.IterationLog.Add($"Child 2: x{fractionalVariable + 1} >= {upperValue:0.###}");

            //create both branches
            var lowerChild = CreateChild(node, fractionalVariable, ConstraintRelation.LessThanOrEqual, lowerValue);
            var upperChild = CreateChild(node, fractionalVariable, ConstraintRelation.GreaterThanOrEqual, upperValue);

            // --------------------------------------------------
            // Backtracking
            // --------------------------------------------------

            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add("Exploring lower branch...");

            Branch(lowerChild, finalResult);

            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add($"Backtracking to depth {node.Depth}.");

            finalResult.IterationLog.Add("Exploring upper branch...");

            Branch(upperChild, finalResult);

            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add($"Finished all branches below depth {node.Depth}.");

        }

        //Creates a child LP model by copying the parent model and adding one branching constraint
        private SubProblem CreateChild( SubProblem parent, int variableIndex, ConstraintRelation relation, double value)
        {
             if (parent.Model == null)
                throw new InvalidOperationException("Cannot create a child without a parent model.");

            var childModel = CloneModel(parent.Model);

            int variableCount = childModel.Objective.Coefficients.Length;

            var coefficients = new double[variableCount];

            coefficients[variableIndex] = 1.0;

            childModel.Constraints.Add(new Constraint{Coefficients = coefficients, Relation = relation, Rhs = value});

            return new SubProblem{Model = childModel, Parent = parent, Depth = parent.Depth + 1, BranchVariableIndex = variableIndex, BranchRelation = relation, BranchValue = value, Status = SubProblemStatus.Active};
        }

        //Determines wether an LP bound cannot improve the best integer solution already found
        private bool CannotImproveBest(double bound, ObjectiveType objectiveType)
        {
            if(objectiveType == ObjectiveType.Max)
            {
                return bound <= bestObjective + Tolerance;
            }

            return bound >= bestObjective - Tolerance;
        }

        //Finds the first variable that is required to be integer but currently has fractional value
        private int FindFractionalIntegerVariable(LPModel model, double[] values)
        {
            int count = Math.Min(model.SignRestrictions.Count, values.Length );

            for(int i = 0; i < count; i++)
            {
                var type = model.SignRestrictions[i];

                if(type != VariableType.Int && type != VariableType.Bin)
                {
                    continue;
                }

                double value = values[i];

                if(Math.Abs(value - Math.Round(value)) > Tolerance)
                {
                    return i;
                }
            }

            return -1;
        }

        //Updates the incumbet/best integer solution
        private void UpdateBestCandidate(SolverResult candidate, LPModel model, SolverResult finalResult)
        {
            bool better;

            if(!hasBestCandidate)
            {
                better = true;
            }
            else if(model.Objective.Type == ObjectiveType.Max)
            {
                better = candidate.ObjectiveValue > bestObjective + Tolerance;
            }
            else
            {
                better = candidate.ObjectiveValue < bestObjective - Tolerance;
            }

            if(!better)
               return;
            
            hasBestCandidate = true;

            bestObjective = candidate.ObjectiveValue;

            bestSolution = (double[])candidate.VariableValues.Clone();

            finalResult.IterationLog.Add("");
            finalResult.IterationLog.Add("NEW BEST CANDIDATE");
            finalResult.IterationLog.Add($"Objective = {bestObjective:0.###}");
        }

        //Creates a model suitable for the LP relazation

        //Binary variables must have the upper bound x <= 1
        //Their integrality is relaxed byt the bunary upper bound remains in the LP relaxation
        private LPModel CreateRelaxationModel(LPModel original)
        {
            var model = CloneModel(original);

            int variableCount = model.Objective.Coefficients.Length;

            for(int i = 0; i < variableCount; i++)
            {
                if (i >= model.SignRestrictions.Count)
                    continue;

                if(model.SignRestrictions[i] != VariableType.Bin)
                   continue;

                var coefficients = new double[variableCount];

                coefficients[i] = 1.0;

                bool alreadyExists = model.Constraints.Any
                (c => c.Relation == ConstraintRelation.LessThanOrEqual && Math.Abs(c.Rhs - 1.0) <= Tolerance && 
                Math.Abs(c.Coefficients[i] - 1.0) <= Tolerance &&c.Coefficients.Select((value, index) => index == i? 0.0
                : Math.Abs(value)).Sum() <= Tolerance);

                if(!alreadyExists)
                {
                    model.Constraints.Add(new Constraint
                        {Coefficients = coefficients, Relation = ConstraintRelation.LessThanOrEqual, Rhs = 1.0});
                    
                }               
            }

            return model;
        }

        //Makes a copy of the LP model so that adding branch constraints never changes the original input model
        private LPModel CloneModel(LPModel original)
        {
            var clone = new LPModel{Objective = original.Objective, SignRestrictions = new List<VariableType>(original.SignRestrictions), SourceFileName = original.SourceFileName};

            foreach(var constraint in original.Constraints)
            {
                clone.Constraints.Add(new Constraint{Coefficients = (double[])constraint.Coefficients.Clone(), Relation = constraint.Relation, Rhs = constraint.Rhs});
            }
            return clone;
        }

    }
}
