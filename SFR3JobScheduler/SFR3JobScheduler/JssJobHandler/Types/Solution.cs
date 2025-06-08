using System;
namespace SFR3JobScheduler.JssJobHandler.Types
{
    public class Solution
    {
        public string ID = Guid.NewGuid().ToString();

        public Dictionary<string, Dictionary<string, Job>> Assignments { get; set; } = new();

        public Dictionary<string,Job> UnassignedJobs { get; set; } = new();

        public List<Technician> technicians { get; set; } = new List<Technician>();

        public void AssignJob(string technicianName, Job job)
        {
            if (!Assignments.ContainsKey(technicianName))
            {
                Assignments[technicianName] = new Dictionary<string,Job>();
            }
            Assignments[technicianName].Add(job.Id, job);

            UnassignedJobs.Remove(job.Id);
        }

        public Dictionary<string,Job> GetJobs(string technicianName)
        {
            return Assignments.TryGetValue(technicianName, out var jobs) ? jobs : new Dictionary<string, Job>();
        }

        public override string ToString()
        {
            var result = "";
            foreach (var kvp in Assignments)
            {
                result += $"Technician: {kvp.Key}\n";
                foreach (var (jobKey, job) in kvp.Value)
                {
                    result += $"  - Job {job.Id} ({job.RequiredSkill}) at {job.Location} (Duration: {job.DurationEstimate})\n";
                }
            }
            return result;
        }
        public bool RemoveJob(string technicianName, string jobId)
        {
            if (Assignments.TryGetValue(technicianName, out var jobs))
            {
                var jobToRemove = jobs[jobId];
                if (jobToRemove != null)
                {
                    jobs.Remove(jobToRemove.Id);

                    return true;
                }
            }
            return false;
        }
    }

}

