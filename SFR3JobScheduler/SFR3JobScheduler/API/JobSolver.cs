using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFR3JobScheduler.JssJobHandler;
using SFR3JobScheduler.JssJobHandler.Types;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SFR3JobScheduler.API
{
    [Route("api/JobSolver")]
    public class JobSolver : ControllerBase
    {
        private readonly JssSessionService jssSessionService;
        Random rand = new Random();
        public JobSolver(JssSessionService _jssSessionService)
        {
            jssSessionService = _jssSessionService;
        }

        //GenerateRandomSample
        //public async Task<IActionResult> GenerateRandomSample([FromBody] GameStart request)
        [HttpPost("GenerateRandomSample")]
        public async Task<IActionResult> GenerateRandomSample()
        {
            //var New_operation = JssJob.createRandomJob(technicians_ammount :10);
            var New_operation = JssJob.createRandomJob();
            var New_solution = new Solution();
            New_solution.UnassignedJobs = New_operation.jobs;
            foreach(var techni in New_operation.technicians)
            {
                New_solution.Assignments.Add(techni.Id, new Dictionary<string, Job>());
            }
            New_solution.technicians = New_operation.technicians;
            var solv = new Solver(New_solution);
            var actual_sol = solv.solve();
            await jssSessionService.StoreRoom(actual_sol.ID, actual_sol);
            return Ok(new
            {
                actual_sol.Assignments,
                actual_sol.UnassignedJobs,
                actual_sol.ID,
                //roomId = tmp_room.RoomId,

                technicians = New_solution.technicians
            });

        }
        [HttpPost("AddJob")]
        public async Task<IActionResult> AddJob([FromBody] CreateJssJob request)
        {
            var og_problem = await jssSessionService.GetSingleSessions(request.problemID);
            TimeSpan duration = TimeSpan.Parse(request.durationEstimate);

            // Create new job with random location
            float lat = 33.9f + (float)rand.NextDouble() * 0.25f;
            float lon = -118f + (float)rand.NextDouble() * 0.25f;

            var newJob = new Job
            {
                Id = request.id,
                Location = new LatLng(lat, lon),
                DurationEstimate = duration,
                StartTime = TimeSpan.FromHours(9),
                SLA = TimeSpan.FromHours(17),
                RequiredSkill = request.requiredSkill
            };

            // ⛔ Unassign all pending jobs from all technicians
            foreach (var technician in og_problem.technicians)
            {
                if (!og_problem.Assignments.ContainsKey(technician.Id))
                    continue;

                var jobList = og_problem.Assignments[technician.Id];
                var toUnassign = jobList.Values
                    .Where(j => j.jobState == JobState.pending)
                    .ToList();

                foreach (var job in toUnassign)
                {
                    jobList.Remove(job.Id);
                    technician.Schedule.Remove(job.Id);
                    og_problem.UnassignedJobs[job.Id] = job;
                }

                // Reset technician state
                technician.CurrentTime = TimeSpan.FromHours(9);
                technician.Location = technician.Origin;
            }

            // ✅ Add new job to UnassignedJobs
            og_problem.UnassignedJobs[newJob.Id] = newJob;

            // ♻️ Re-solve
            var solv = new Solver(og_problem);
            var actual_sol = solv.solve();

            return Ok(new
            {
                id = actual_sol.ID,
                assignments = actual_sol.Assignments,
                unassignedJobs = actual_sol.UnassignedJobs,
                technicians = actual_sol.technicians
            });
        }
        [HttpPost("DeleteJob")]
        public async Task<IActionResult> DeleteJob([FromBody] EditJssJob request)
        {
            var og_problem = await jssSessionService.GetSingleSessions(request.problemID);
            string technicianId = null;
            Job jobToDelete = null;

            // 1. Find job in technician assignments
            foreach (var (techId, jobDict) in og_problem.Assignments)
            {
                if (jobDict.TryGetValue(request.id, out var job))
                {
                    technicianId = techId;
                    jobToDelete = job;
                    break;
                }
            }

            // 2. If assigned, unassign it and pending jobs for reoptimization
            if (technicianId != null && jobToDelete != null)
            {
                var tech = og_problem.technicians.FirstOrDefault(t => t.Id == technicianId);
                var assignments = og_problem.Assignments[technicianId];

                var jobsToUnassign = assignments
                    .Where(kvp => kvp.Value.jobState == JobState.pending)
                    .Select(kvp => kvp.Value)
                    .ToList();

                foreach (var job in jobsToUnassign)
                {
                    assignments.Remove(job.Id);
                    tech?.Schedule.Remove(job.Id);
                    og_problem.UnassignedJobs[job.Id] = job;
                }

                // Rebuild technician state with fixed jobs
                tech.CurrentTime = TimeSpan.FromHours(9);
                tech.Location = tech.Origin;

                var fixedJobs = assignments.Values
                    .Where(j => j.jobState != JobState.pending)
                    .OrderBy(j => j.StartTime); // Just in case

                foreach (var job in fixedJobs)
                {
                    var dist = Solver.hypot(tech.Location, job.Location);
                    var travelTime = TimeSpan.FromMinutes(dist * 111.1);
                    tech.CurrentTime += travelTime + job.DurationEstimate;
                    tech.Location = job.Location;
                }
            }

            // 3. If it's in unassigned, just remove it
            if (og_problem.UnassignedJobs.ContainsKey(request.id))
            {
                og_problem.UnassignedJobs.Remove(request.id);
            }

            // ♻️ Re-solve the problem with remaining unassigned jobs
            var solv = new Solver(og_problem);
            var actual_sol = solv.solve();

            return Ok(new
            {
                id = actual_sol.ID,
                assignments = actual_sol.Assignments,
                unassignedJobs = actual_sol.UnassignedJobs,
                technicians = actual_sol.technicians
            });
        }
        [HttpPost("EditJob")]
        public async Task<IActionResult> EditJob([FromBody] EditJssJob request)
        {
            var og_problem = await jssSessionService.GetSingleSessions(request.problemID);

            
            TimeSpan duration = TimeSpan.Parse(request.durationEstimate);
            Job jobCopy = null;
            string technician = "";
            foreach(var (technician_key, job_list) in og_problem.Assignments)
            {
                if (job_list.ContainsKey(request.id))
                {
                    jobCopy = job_list[request.id];
                    technician = technician_key;
                    break;
                }
            }

            if (jobCopy != null)
            {
                var new_state = Job.mapping.ContainsKey(request.state)
                    ? Job.mapping[request.state]
                    : jobCopy.jobState;

                var temp_tech = og_problem.technicians.FirstOrDefault(x => x.Id == technician);

                if (new_state == JobState.pending)
                {
                    // If setting to pending, unassign and reoptimize
                    var jobsToUnassign = og_problem.Assignments[technician]
                        .Where(kvp => kvp.Value.jobState == JobState.pending || kvp.Key == request.id)
                        .Select(kvp => kvp.Value)
                        .ToList();

                    foreach (var job in jobsToUnassign)
                    {
                        og_problem.Assignments[technician].Remove(job.Id);
                        temp_tech?.Schedule.Remove(job.Id);

                        if (job.Id == request.id)
                        {
                            job.DurationEstimate = duration;
                            job.jobState = JobState.pending;
                            if (request.priority != null)
                            {
                                job.priority = request.priority.Value;
                            }
                        }

                        og_problem.UnassignedJobs[job.Id] = job;
                    }

                    // Rebuild fixed route for this tech (non-pending jobs)
                    temp_tech.CurrentTime = TimeSpan.FromHours(9);
                    temp_tech.Location = temp_tech.Origin;

                    var fixedJobs = og_problem.Assignments[technician]
                        .Values
                        .Where(j => j.jobState != JobState.pending)
                        .ToList();

                    foreach (var job in fixedJobs)
                    {
                        var dist = Solver.hypot(Solver.lastJob(og_problem, temp_tech), job.Location);
                        var transport = TimeSpan.FromMinutes(dist * 111.1);
                        temp_tech.CurrentTime += transport + job.DurationEstimate;
                        temp_tech.Location = job.Location;
                    }
                }
                else
                {
                    var first_job = og_problem.Assignments[technician]
                        .Where(kvp => kvp.Value.jobState == JobState.pending)
                        .FirstOrDefault().Value;
                    if (first_job != jobCopy)
                    {
                        return BadRequest(new
                        {
                            message = "only the first job can change its state"
                        });
                    }
                    // If NOT pending, just update the job directly
                    jobCopy.DurationEstimate = duration;
                    jobCopy.jobState = new_state;
                    // Rebuild that tech's timeline from scratch
                    if (og_problem.Assignments.TryGetValue(technician, out var jobs))
                    {
                        var tempTech = og_problem.technicians.FirstOrDefault(x => x.Id == technician);
                        if (tempTech != null)
                        {
                            tempTech.CurrentTime = TimeSpan.FromHours(9);
                            tempTech.Location = tempTech.Origin;

                            // Reorder jobs chronologically (or preserve insertion order if that’s your logic)
                            var orderedJobs = jobs.Values.OrderBy(j => j.StartTime).ToList();

                            var prevLocation = tempTech.Origin;

                            foreach (var job in orderedJobs)
                            {
                                var dist = Solver.hypot(prevLocation, job.Location);
                                var transport = TimeSpan.FromMinutes(dist * 111.1);

                                job.StartTime = tempTech.CurrentTime + transport;

                                tempTech.CurrentTime = job.StartTime + job.DurationEstimate;
                                tempTech.Location = job.Location;

                                prevLocation = job.Location;
                            }
                        }
                    }
                }
            }

            var solv = new Solver(og_problem);
            var actual_sol = solv.solve();
            return Ok(new
            {
                actual_sol.Assignments,
                actual_sol.UnassignedJobs,
                actual_sol.ID,
                technicians = actual_sol.technicians

            });

        }

        [HttpPost("EditUnassignedJob")]
        public async Task<IActionResult> EditUnassignedJob([FromBody] EditJssJob request)
        {
            var og_problem = await jssSessionService.GetSingleSessions(request.problemID);

            if (!og_problem.UnassignedJobs.TryGetValue(request.id, out var job))
            {
                return NotFound($"Job with ID {request.id} not found in unassigned jobs.");
            }

            // Parse and apply edits
            job.DurationEstimate = TimeSpan.Parse(request.durationEstimate);

            // If it's pending again, try re-optimizing
            var solv = new Solver(og_problem);
            var actual_sol = solv.solve();

            return Ok(new
            {
                actual_sol.Assignments,
                actual_sol.UnassignedJobs,
                actual_sol.ID,
                technicians = actual_sol.technicians
            });
        }

        [HttpPost("DeleteUnnasignedJob")]
        public async Task<IActionResult> DeleteUnnasignedJob([FromBody] EditJssJob request)
        {
            var og_problem = await jssSessionService.GetSingleSessions(request.problemID);

            if (!og_problem.UnassignedJobs.TryGetValue(request.id, out var job))
            {
                return NotFound($"Job with ID {request.id} not found in unassigned jobs.");
            }

            // 3. If it's in unassigned, just remove it
            if (og_problem.UnassignedJobs.ContainsKey(request.id))
            {
                og_problem.UnassignedJobs.Remove(request.id);
            }

            // ♻️ Re-solve the problem with remaining unassigned jobs
            var solv = new Solver(og_problem);
            var actual_sol = solv.solve();

            return Ok(new
            {
                id = actual_sol.ID,
                assignments = actual_sol.Assignments,
                unassignedJobs = actual_sol.UnassignedJobs,
                technicians = actual_sol.technicians
            });
        }

        public class CreateJssJob
        {

            public string id { get; set; }
            public string problemID { get; set; }

            public string durationEstimate { get; set; }
            public string requiredSkill { get; set; }
            public LatLng location { get; set; }

        }

        public class EditJssJob
        {

            public string id { get; set; }
            public string problemID { get; set; }

            public string? state { get; set; }
            public int? priority { get; set; }

            public string? durationEstimate { get; set; }
            

        }

        

    }
   
}

