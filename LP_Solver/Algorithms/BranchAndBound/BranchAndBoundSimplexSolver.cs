using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using LP_Solver.Core;
using LP_Solver.Models;

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
        }
    }
}
