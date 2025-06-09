using System;
using System.Collections.Generic;
using System.Numerics;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class Solver
	{
		public Solution solution;
        //public JssJob jssJob;
        public Solver(Solution sol)
		{
			solution = sol;
            //jssJob = jss;

        }
		public Solution solve()
		{
            foreach (var tech in solution.technicians)
            {
                // Find latest assigned job to resume after
                if (solution.Assignments.TryGetValue(tech.Id, out var assignedJobs) && assignedJobs.Any())
                {
                    var lastJob = assignedJobs.Values
                        .OrderBy(j => j.StartTime + j.DurationEstimate)
                        .Last();

                    tech.CurrentTime = lastJob.StartTime + lastJob.DurationEstimate;
                    tech.Location = lastJob.Location;
                }
                else
                {
                    tech.CurrentTime = TimeSpan.FromHours(9); // start of day
                    tech.Location = tech.Origin;
                }
                NearestNeighbor(tech);
                //BestFirstSearch(tech);
            
            }

            return solution;
		}
        


        public void NearestNeighbor(Technician tech)
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

        public static LatLng lastJob(Solution solution, Technician technician)
        {
            var lastAssignedJob = solution.Assignments[technician.Id].Values.LastOrDefault();
            var lastLocation = lastAssignedJob != null ? lastAssignedJob.Location : technician.Location;

            return lastLocation;
        }


        public static double hypot(LatLng loc1, LatLng loc2)
        {
            return Math.Sqrt(Math.Pow(loc1.Lat - loc2.Lat, 2) + Math.Pow(loc1.Lng - loc2.Lng, 2));
        }
    }
}

