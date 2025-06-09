using System;
using System.Collections.Generic;
using System.Numerics;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class NearestNeightbor
	{
        Solution solution;
        public NearestNeightbor(Solution _sol) {
            solution = _sol;
        }
        
        public void Run(Technician tech)
        {
            while (true)
            {
                var pendingJobs = solution.UnassignedJobs.Values
                    .Where(job => job.RequiredSkill == tech.Skill && job.jobState == JobState.pending)
                    .ToList();

                Job? bestJob = null;
                double bestCost = double.MaxValue;

                foreach (var job in pendingJobs)
                {
                    var distance = Solver.hypot(tech.Location, job.Location);
                    var travelTime = TimeSpan.FromMinutes(distance * 111.1);
                    var arrivalTime = tech.CurrentTime + travelTime;
                    var finishTime = arrivalTime + job.DurationEstimate;

                    if (finishTime > job.SLA)
                        continue; // violates SLA

                    var slack = 0;// (job.SLA - finishTime).TotalMinutes;
                    var cost = distance +
                                (slack * 0.1) +
                                (3 - job.priority) * 10;

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestJob = job;
                    }
                }

                if (bestJob == null)
                    break;

                // Assign best job
                var travelTimeToJob = TimeSpan.FromMinutes(Solver.hypot(tech.Location, bestJob.Location) * 111.1);
                bestJob.StartTime = tech.CurrentTime + travelTimeToJob;

                tech.CurrentTime = bestJob.StartTime + bestJob.DurationEstimate;
                tech.Location = bestJob.Location;

                solution.AssignJob(tech.Id, bestJob);
                solution.UnassignedJobs.Remove(bestJob.Id);
            }

        }
        // Priority queue item


    }
}

