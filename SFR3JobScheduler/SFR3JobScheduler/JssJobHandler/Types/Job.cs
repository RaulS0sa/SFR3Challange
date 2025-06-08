using System;
using System.Numerics;

namespace SFR3JobScheduler.JssJobHandler.Types
{
    public enum JobState
    {
        pending,
        ongoing,
        done
    }
    
    public class Job
    {
        public static Dictionary<string, JobState> mapping = new Dictionary<string, JobState>()
    {

        { "Pending" , JobState.pending },
        { "Ongoing" , JobState.ongoing},
        { "Done", JobState.done }
    };
        public string Id { get; set; }
        public LatLng Location { get; set; } // lat/lon as x/y
        public TimeSpan DurationEstimate { get; set; }
        public TimeSpan SLA { get; set; }
        public TimeSpan StartTime { get; set; }
        //public TimeSpan TransportationTime { get; set; }
        public string RequiredSkill { get; set; }
        public JobState jobState { get; set; }

        public int priority { get; set; }

        private static readonly string[] Skills = {
            "HVAC", "Plumbing", "Electrical", "General", "Locksmith"
        };

        private static readonly Random rand = new();

        public static Job GenerateRandom()
        {
            // Roughly within Los Angeles (lat 33.9–34.2, lon -118.6 to -118.1)
            float lat = 33.9f + (float)rand.NextDouble() * 0.2f;
            float lon = -118f + (float)rand.NextDouble() * 0.2f;

            return new Job
            {
                Id = Guid.NewGuid().ToString()[..8],
                Location = new LatLng(lat, lon),
                DurationEstimate = TimeSpan.FromMinutes(rand.Next(30, 120)), // 30 mins to 2 hours
                priority = rand.Next(1, 4), // 3 levels of priority
                //DurationEstimate = TimeSpan.FromMinutes(60), // 30 mins to 2 hours
                StartTime = TimeSpan.FromHours(9), // due in 9–12 hours from day start
                SLA = TimeSpan.FromHours(17), // due in 9–12 hours from day start
                RequiredSkill = Skills[rand.Next(Skills.Length)]
            };
        }
    }

    public class LatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        public LatLng() { }
        public LatLng(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }
    }

}

