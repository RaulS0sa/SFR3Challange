using System;
using System.Collections.Generic;
using System.Numerics;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class KPIResult
{
    public int JobsAssigned { get; set; }
    public double AvgStartDelay { get; set; }
    public double TotalDistance { get; set; }
    public double TotalIdleTime { get; set; }
    public double SlaComplianceRate { get; set; }

    public void Print(string label)
    {
        Console.WriteLine($"=== {label} ===");
        Console.WriteLine($"Jobs Assigned:      {JobsAssigned}");
        Console.WriteLine($"Avg Start Delay:    {AvgStartDelay:F2} min");
        Console.WriteLine($"Total Travel:       {TotalDistance:F2} km");
        Console.WriteLine($"Total Idle Time:    {TotalIdleTime:F2} min");
        Console.WriteLine($"SLA Compliance:     {SlaComplianceRate:F2}%");
        Console.WriteLine();
    }
}

public static class KPIReport
{
    public static KPIResult Compute(Solution solution)
    {
        var result = new KPIResult();
        var startOfDay = TimeSpan.FromHours(9);

        int totalJobs = 0, compliantJobs = 0;
        double totalStartDelay = 0;
        double totalDistance = 0;
        double totalIdle = 0;

        foreach (var tech in solution.technicians)
        {
            if (!solution.Assignments.TryGetValue(tech.Id, out var jobMap) || jobMap.Count == 0)
                continue;

            var jobs = jobMap.Values.OrderBy(j => j.StartTime).ToList();

            LatLng lastLocation = tech.Origin;
            TimeSpan lastFinish = startOfDay;

            foreach (var job in jobs)
            {
                totalJobs++;
                if (job.StartTime + job.DurationEstimate <= job.SLA)
                    compliantJobs++;

                totalStartDelay += (job.StartTime - startOfDay).TotalMinutes;
                totalDistance += Solver.hypot(lastLocation, job.Location);
                totalIdle += (job.StartTime - lastFinish).TotalMinutes;

                lastFinish = job.StartTime + job.DurationEstimate;
                lastLocation = job.Location;
            }
        }

        result.JobsAssigned = totalJobs;
        result.AvgStartDelay = totalJobs > 0 ? totalStartDelay / totalJobs : 0;
        result.TotalDistance = totalDistance;
        result.TotalIdleTime = totalIdle;
        result.SlaComplianceRate = totalJobs > 0 ? (double)compliantJobs / totalJobs * 100 : 0;

        return result;
    }

    public static void Compare(Solution nn, Solution bfs)
    {
        var nnResult = Compute(nn);
        var bfsResult = Compute(bfs);

        nnResult.Print("Nearest Neighbor");
        bfsResult.Print("Best-First Search");
    }
}

}

