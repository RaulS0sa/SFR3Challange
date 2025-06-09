using System;
using System.Collections.Generic;
using System.Numerics;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class BestFirstSearch
	{
        Solution solution;
        public BestFirstSearch(Solution _sol) {
            solution = _sol;
        }
        record PathNode(Job? LastJob, TimeSpan Time, LatLng Loc, List<Job> Path, double Cost);

        // Heuristic function (used for sorting jobs in the priority queue)
        double HeuristicCost(Job job, LatLng currentLoc, TimeSpan currentTime)
        {
            var distance = Solver.hypot(currentLoc, job.Location);
            var travelTime = TimeSpan.FromMinutes(distance * 111.1);
            var arrivalTime = currentTime + travelTime;
            var finishTime = arrivalTime + job.DurationEstimate;

            if (finishTime > job.SLA)
                return double.MaxValue; // Infeasible job

            var slack = (job.SLA - finishTime).TotalMinutes;
            return (distance) + (slack * 0.07) + (3 - job.priority) * 10; // Lower is better
        }

        public void BestFirstS(Technician tech)
        {
            var pendingJobs = solution.UnassignedJobs.Values
                .Where(job => job.RequiredSkill == tech.Skill && job.jobState == JobState.pending)
                .ToList();

            if (!pendingJobs.Any())
                return;

            var openSet = new PriorityQueue<PathNode, (double priority, double accumulatedCost)>();
            openSet.Enqueue(new PathNode(null, tech.CurrentTime, tech.Location, new(), 0), (0, 0)); // (priority, cost)

            PathNode? bestNode = null;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                // Track best path seen so far (longest valid)
                if (bestNode == null || current.Path.Count > bestNode.Path.Count)
                {
                    bestNode = current;
                }

                foreach (var job in pendingJobs)
                {
                    if (current.Path.Any(j => j.Id == job.Id)) continue; // Avoid reassigning jobs

                    var distance = Solver.hypot(current.Loc, job.Location);
                    var travelTime = TimeSpan.FromMinutes(distance * 111.1);
                    var arrivalTime = current.Time + travelTime;
                    var finishTime = arrivalTime + job.DurationEstimate;

                    if (finishTime > job.SLA) continue; // Skip jobs that violate SLA

                    var newPath = new List<Job>(current.Path) { job };
                    var accumulatedCost = current.Cost + distance; // Add actual travel cost
                    var priority = HeuristicCost(job, current.Loc, current.Time); // Use heuristic cost as priority

                    // Add the tie-breaking logic (shorter path if heuristics are the same)
                    openSet.Enqueue(new PathNode(job, finishTime, job.Location, newPath, accumulatedCost),
                                    (priority, accumulatedCost));
                }
            }

            // If best path is found (even partial), assign it
            if (bestNode != null && bestNode.Path.Count > 0)
            {
                foreach (var job in bestNode.Path)
                {
                    var travelTime = TimeSpan.FromMinutes(Solver.hypot(tech.Location, job.Location) * 111.1);
                    job.StartTime = tech.CurrentTime + travelTime;

                    tech.CurrentTime = job.StartTime + job.DurationEstimate;
                    tech.Location = job.Location;

                    solution.AssignJob(tech.Id, job);
                    solution.UnassignedJobs.Remove(job.Id);
                }
            }
        }

    }
}

