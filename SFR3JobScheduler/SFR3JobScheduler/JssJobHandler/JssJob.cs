using System;
using System.Collections.Generic;
using System.Numerics;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class JssJob
	{
        public List<Technician> technicians = new List<Technician>();
        public Dictionary<string, Job> jobs = new Dictionary<string, Job>();
        public TransportMatrix transportMatrix = new TransportMatrix();
        public JssJob()
        {
        }

        public static JssJob createRandomJob(int technicians_ammount = 5, int job_lb = 14, int job_ub = 24)
        {
            var daily_job = new JssJob();
            var rnd = new Random();
            foreach(var i in Enumerable.Range(0, technicians_ammount)){
                var random_tecnician = Technician.GenerateSkiledTecnician(i);
                daily_job.technicians.Add(random_tecnician);
            }

            foreach (var i in Enumerable.Range(0, rnd.Next(job_lb, job_ub)))
            {
                var random_job = Job.GenerateRandom();
                daily_job.jobs.Add(random_job.Id, random_job);
            }
            foreach(var (j_key, j) in daily_job.jobs)
            {
                var tmp_list = new List<TravelCost>();
                foreach (var (i_key, i) in daily_job.jobs)
                {
                    var tmp_dist = Solver.hypot(j.Location, i.Location);
                    tmp_list.Add(new TravelCost() {
                        Distance = tmp_dist,
                        Time = tmp_dist + rnd.NextDouble()
                    });
                }
                daily_job.transportMatrix.Matrix.Add(tmp_list);
            }

            return daily_job;
        }
       
    }

    
    
    public class TravelCost
    {
        
        public double Distance { get; set; }

        
        public double Time { get; set; }
    }

    public class TransportMatrix
    {
        public List<List<TravelCost>> Matrix { get; set; }
        public TransportMatrix()
        {
            Matrix = new List<List<TravelCost>>();
        }
    }
}

