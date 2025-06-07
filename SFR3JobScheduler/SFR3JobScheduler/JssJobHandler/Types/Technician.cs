using System;
using System.Numerics;

namespace SFR3JobScheduler.JssJobHandler.Types
{

    public class Technician
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public Dictionary<string, Job> Schedule { get; set; } = new();
        public TimeSpan CurrentTime { get; set; }
        public LatLng Location { get; set; }
        public LatLng Origin { get; set; }
        public string Skill { get; set; }

        private static readonly string[] FirstNames = {
            "Alex", "Jordan", "Taylor", "Casey", "Morgan", "Riley", "Skyler", "Avery",
            "Jamie", "Parker", "Drew", "Reese", "Jesse", "Hayden", "Kai", "Logan",
            "Emery", "Cameron", "Dakota", "Elliot"
        };

        private static readonly string[] LastNames = {
            "Smith", "Johnson", "Lee", "Walker", "Garcia", "Nguyen", "Brown", "Martinez",
            "Clark", "Anderson", "Thomas", "Hernandez", "Lopez", "Gonzalez", "Robinson",
            "Morris", "Hall", "Rivera", "Stewart", "Bennett"
        };

        private static readonly string[] Skills = {
        "HVAC", "Plumbing", "Electrical", "General", "Locksmith"
    };

        private static readonly string[] Locations = {
        "Burbank", "Santa Monica", "Pasadena", "Long Beach", "Downtown LA", "Glendale"
    };

        private static readonly Random rand = new();

        public static Technician GenerateRandom()
        {
            float lat = 33.9f;
            float lon = -118f;
            //string fullName = $"{FirstNames[rand.Next(FirstNames.Length)]} {LastNames[rand.Next(LastNames.Length)]}";
            string fullName = $"{FirstNames[rand.Next(FirstNames.Length)]}";


            return new Technician
            {
                Id = Guid.NewGuid().ToString(),
                Name = fullName,
                Location = new LatLng(lat, lon),
                Skill = Skills[rand.Next(Skills.Length)],

                CurrentTime = TimeSpan.FromHours(9) // Start between 6-9 AM
            };
        }
        public static Technician GenerateSkiledTecnician(int i)
        {
            float lat = 33.9f;
            float lon = -118f;
            //string fullName = $"{FirstNames[rand.Next(FirstNames.Length)]} {LastNames[rand.Next(LastNames.Length)]}";
            string fullName = $"{FirstNames[rand.Next(FirstNames.Length)]}";

            return new Technician
            {
                Id = Guid.NewGuid().ToString(),
                Name = fullName,
                Location = new LatLng(lat, lon),
                Origin = new LatLng(lat, lon),
                Skill = Skills[ i % Skills.Length],

                CurrentTime = TimeSpan.FromHours(9) // Start between 6-9 AM
            };
        }
    }

}

