// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandCreationService
{
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;
    using System.IO;
    using System.Threading.Tasks;

    public class BandActorGenerator
    {
        private static CryptoRandom r = new CryptoRandom();
        public Dictionary<CountyRecord, List<Guid>> doctorsPerCounty = new Dictionary<CountyRecord, List<Guid>>();
        private List<string> peopleNames = new List<string>();
        private string pathToCountyFile = null;
        private string pathToNamesFile = null;
        private int baseDoctorsPerCounty;
        private int doctorsPerPopulation; //example: to say that there are 3 doctors per 10000 people
        private int populationFactor; //set doctorsPerPopulation to 3 and populationFactor to 10000.

        public BandActorGenerator(ConfigurationSettings settings, string dataPath)
        {
            KeyedCollection<string, ConfigurationProperty> serviceParameters = settings.Sections["HealthMetrics.BandCreationService.Settings"].Parameters;

            this.pathToCountyFile = Path.Combine(dataPath, serviceParameters["CountyFileName"].Value);
            this.pathToNamesFile = Path.Combine(dataPath, serviceParameters["PeopleFileName"].Value);
            this.baseDoctorsPerCounty = int.Parse(serviceParameters["BaseDoctorsPerCounty"].Value);
            this.doctorsPerPopulation = int.Parse(serviceParameters["DoctorsPerPopulation"].Value);
            this.populationFactor = int.Parse(serviceParameters["PopulationFactor"].Value);
        }

        public void Prepare()
        {
            Task.WhenAll(this.BuildCountyInfo(), this.BuildPeopleNames());
        }

        public string GetRandomName(CryptoRandom r)
        {
            int index = r.Next(0, this.peopleNames.Count);
            return this.peopleNames[index];
        }

        public BandInfo GetRandomHealthStatus(CountyRecord county, CryptoRandom random)
        {
            BandInfo b = new BandInfo();
            b.CountyInfo = county;
            b.PersonName = this.GetRandomName(random);
            double healthDistribution = GetRandomNormalDistributedWithGivenMeanAndStdev(r, b.CountyInfo.CountyHealth, .75, 3);
            b.HealthIndex = NormalizeHealthDistribution(healthDistribution);
            b.DoctorId = this.doctorsPerCounty[county][random.Next(0, this.doctorsPerCounty[county].Count)];
            return b;
        }

        private Task BuildCountyInfo()
        {
            StreamReader countyReader = new StreamReader(File.OpenRead(this.pathToCountyFile));

            while (!countyReader.EndOfStream)
            {
                List<Guid> doctorList = new List<Guid>();

                string line = countyReader.ReadLine();
                string[] values = line.Split(',');
                int population = int.Parse(values[3]);
                double healthBonus = double.Parse(values[4]);

                string countyName = string.Format("{0}, {1}", values[1], values[2].Replace(" ", ""));

                int totalDoctors = this.baseDoctorsPerCounty + (this.doctorsPerPopulation * (int)(Math.Round((double)population / this.populationFactor, 0)));

                for (int doctorCount = 0; doctorCount < totalDoctors; doctorCount++)
                {
                    doctorList.Add(Guid.NewGuid());
                }

                this.doctorsPerCounty.Add(new CountyRecord(countyName, int.Parse(values[0]), healthBonus), doctorList);
            }

            return Task.FromResult(true);
        }

        private Task BuildPeopleNames()
        {
            StreamReader nameReader = new StreamReader(File.OpenRead(this.pathToNamesFile));

            while (!nameReader.EndOfStream)
            {
                string line = nameReader.ReadLine();
                string[] values = line.Split(',');
                string personName = string.Format("{0} {1}", values[0], values[1]);
                this.peopleNames.Add(personName);
            }

            return Task.FromResult(true);
        }

        private static double GetRandomNormalDistributedWithGivenMeanAndStdev(CryptoRandom rand, double mean, double stddev, int precision)
        {
            //https://en.wikipedia.org/wiki/Box%E2%80%93Muller_transform
            //http://stackoverflow.com/questions/218060/random-gaussian-variables

            double u1 = rand.NextDouble();
            double u2 = rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double randNormal = mean + stddev * randStdNormal;
            double finalSampledVal = Math.Round(randNormal, precision);
            return finalSampledVal;
        }

        private static HealthIndex NormalizeHealthDistribution(double healthDistribution)
        {
            //trim the tails
            if (healthDistribution < -3)
            {
                healthDistribution = -3;
            }
            else if (healthDistribution > 3)
            {
                healthDistribution = 3;
            }

            //move to a scale of 0-100
            return (HealthIndex)Math.Round((healthDistribution + 3) * 16.7, 0);

            //Where did 16.7 come from?
            //healthDistribution = healthDistribution * 100; //-3 = -300, -1.27 = -127, 3 = 300, 2.20 = 220
            //healthDistribution = healthDistribution + 300; // -300 = 0, 300 = 600, -127 = 173 220 = 520
            //healthDistribution = Math.Round(healthDistribution / 6, 0); // 0 = 0, 600 = 100, 173 = 29, 520 = 87
        }
    }
}