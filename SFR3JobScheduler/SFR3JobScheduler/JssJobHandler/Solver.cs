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
            
            var nn = new NearestNeightbor(solution);
            
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

                nn.Run(tech);
                // bfs.BestFirstS(tech);
                

            }
            // Compare KPIs
            // KPIReport.Compare(solutionNN, solutionBFS);
            return solution;
		}
        


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

