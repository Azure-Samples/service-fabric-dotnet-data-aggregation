// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Newtonsoft.Json;

    [StatePersistence(StatePersistence.Volatile)]
    internal class DoctorActor : Actor, IDoctorActor, IRemindable
    {
        private const string GenerateHealthDataAsyncReminder = "SendHealthDataToCountyAsync";
        private Uri countyServiceInstanceUri;
        private Uri bandActorServiceInstanceUri;
        private CryptoRandom random;
        private HealthIndexCalculator indexCalculator;

        private HttpCommunicationClientFactory clientFactory = new HttpCommunicationClientFactory(
            ServicePartitionResolver.GetDefault(),
            "DoctorCommunicationEndpoint",
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(3));

        public DoctorActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task NewAsync(string name, CountyRecord countyRecord)
        {
            await this.StateManager.SetStateAsync<string>("Name", name);
            await this.StateManager.SetStateAsync<CountyRecord>("CountyRecord", countyRecord);
            await this.StateManager.SetStateAsync<long>("HealthReportCount", 0);
            await this.StateManager.SetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses", new Dictionary<Guid, DoctorPatientState>());
            await this.RegisterReminderAsync(GenerateHealthDataAsyncReminder, null, TimeSpan.FromSeconds(this.random.Next(5, 15)), TimeSpan.FromSeconds(10));

            ActorEventSource.Current.ActorMessage(this, "Doctor created. ID: {0}. Name: {1}", this.Id.GetGuidId(), name);
        }

        public async Task ReportHealthAsync(Guid personId, string personName, HealthIndex healthIndex)
        {
            ConditionalValue<Dictionary<Guid, DoctorPatientState>> patientReportResult =
                await this.StateManager.TryGetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses");

            ConditionalValue<long> healthReportCountResult = await this.StateManager.TryGetStateAsync<long>("HealthReportCount");

            if (patientReportResult.HasValue)
            {
                patientReportResult.Value[personId] = new DoctorPatientState(personId, personName, healthIndex);

                await this.StateManager.SetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses", patientReportResult.Value);
                await this.StateManager.SetStateAsync<long>("HealthReportCount", healthReportCountResult.Value + 1);

                ActorEventSource.Current.Message(
                    "DoctorActor {0} Recieved health report from band {1} with value {2}",
                    this.Id.GetGuidId(),
                    personId,
                    healthIndex);
            }

            return;
        }

        //public async Task<DoctorDataViewModel> GetPatientsAsync()
        //{
        //    try
        //    {
        //        ConditionalValue<long> healthReportCountResult = await this.StateManager.TryGetStateAsync<long>("HealthReportCount");

        //        if (healthReportCountResult.HasValue)
        //        {

        //            var name = await this.StateManager.GetStateAsync<string>("Name");
        //            var countyRecord = await this.StateManager.GetStateAsync<CountyRecord>("CountyRecord");
        //            var healthReportCount = await this.StateManager.GetStateAsync<long>("HealthReportCount");
        //            var patientHealthReports = await this.StateManager.GetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses");

        //            if (healthReportCountResult.Value == 0)
        //            {
        //                return new DoctorDataViewModel(name, this.indexCalculator.ComputeIndex(-1), countyRecord, Enumerable.Empty<PatientDataViewModel>());
        //            }

        //            HealthIndex patientAverage = await this.GetAveragePatientHealthInfoAsync();

        //            ActorEventSource.Current.ActorMessage(
        //                this,
        //                "Doctor {0} sending doctor view for county {1} with average {2}",
        //                this.Id.GetGuidId(),
        //                countyRecord,
        //                patientAverage);

        //            return new DoctorDataViewModel(
        //                name,
        //                patientAverage,
        //                countyRecord,
        //                patientHealthReports.Select(
        //                    x =>
        //                        new PatientDataViewModel(
        //                            x.Key,
        //                            x.Value.Name,
        //                            x.Value.HealthIndex)));
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new ArgumentException(string.Format("Exception inside doctor actor {0}|{1}|{2}", this.Id, this.Id.Kind, e));
        //    }

        //    throw new ArgumentException(string.Format("No actor state in actor {0}", this.Id));
        //}

        public async Task<Tuple<CountyRecord, string>> GetInfoAndNameAsync()
        {
            ConditionalValue<long> healthReportCountResult = await this.StateManager.TryGetStateAsync<long>("HealthReportCount");

            if (healthReportCountResult.HasValue)
            {
                string name = await this.StateManager.GetStateAsync<string>("Name");
                CountyRecord countyRecord = await this.StateManager.GetStateAsync<CountyRecord>("CountyRecord");
                return new Tuple<CountyRecord, string>(countyRecord, name);
            }

            throw new ArgumentException(string.Format("No actor state in actor {0}", this.Id));
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case GenerateHealthDataAsyncReminder:
                    await this.SendHealthReportToCountyAsync();
                    break;

                default:
                    ActorEventSource.Current.Message("Reminder {0} is not implemented on DoctorActor.", reminderName);
                    break;
            }

            return;
        }

        public async Task SendHealthReportToCountyAsync()
        {
            try
            {
                ConditionalValue<long> healthReportCountResult = await this.StateManager.TryGetStateAsync<long>("HealthReportCount");

                if (healthReportCountResult.HasValue)
                {
                    string name = await this.StateManager.GetStateAsync<string>("Name");
                    CountyRecord countyRecord = await this.StateManager.GetStateAsync<CountyRecord>("CountyRecord");
                    long healthReportCount = healthReportCountResult.Value;
                    Dictionary<Guid, DoctorPatientState> patientHealthReports =
                        await this.StateManager.GetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses");

                    if (healthReportCount > 0)
                    {
                        DoctorStatsViewModel payload = new DoctorStatsViewModel(
                            patientHealthReports.Count,
                            healthReportCount,
                            await this.GetAveragePatientHealthInfoAsync(),
                            name);

                        ServicePartitionKey partitionKey = new ServicePartitionKey(countyRecord.CountyId);
                        Guid id = this.Id.GetGuidId();

                        ServicePartitionClient<HttpCommunicationClient> servicePartitionClient =
                            new ServicePartitionClient<HttpCommunicationClient>(
                                this.clientFactory,
                                this.countyServiceInstanceUri,
                                partitionKey);

                        await servicePartitionClient.InvokeWithRetryAsync(
                            client =>
                            {
                                Uri serviceAddress = new Uri(
                                    client.BaseAddress,
                                    string.Format(
                                        "county/health/{0}/{1}",
                                        partitionKey.Value.ToString(),
                                        id));

                                HttpWebRequest request = WebRequest.CreateHttp(serviceAddress);
                                request.Method = "POST";
                                request.ContentType = "application/json";
                                request.KeepAlive = false;
                                request.Timeout = (int) client.OperationTimeout.TotalMilliseconds;
                                request.ReadWriteTimeout = (int) client.ReadWriteTimeout.TotalMilliseconds;

                                using (Stream requestStream = request.GetRequestStream())
                                {
                                    using (BufferedStream buffer = new BufferedStream(requestStream))
                                    {
                                        using (StreamWriter writer = new StreamWriter(buffer))
                                        {
                                            JsonSerializer serializer = new JsonSerializer();
                                            serializer.Serialize(writer, payload);
                                            buffer.Flush();
                                        }

                                        using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
                                        {
                                            ActorEventSource.Current.Message("Doctor Sent Data to County: {0}", serviceAddress);
                                            return Task.FromResult(true);
                                        }
                                    }
                                }
                            }
                        );
                    }
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.Message("DoctorActor failed to send health report to county service Outer Exception: {0}", e.ToString());
            }
        }

        protected override Task OnActivateAsync()
        {
            this.random = new CryptoRandom();
            ConfigurationPackage configPackage = this.ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            this.indexCalculator = new HealthIndexCalculator(this.ActorService.Context);
            this.UpdateConfigSettings(configPackage.Settings);
            this.ActorService.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;

            ActorEventSource.Current.ActorMessage(this, "Doctor activated. ID: {0}.", this.Id.GetGuidId());

            return Task.FromResult(true);
        }


        private async Task<HealthIndex> GetAveragePatientHealthInfoAsync()
        {
            ConditionalValue<long> healthReportCountResult = await this.StateManager.TryGetStateAsync<long>("HealthReportCount");

            if (healthReportCountResult.HasValue && healthReportCountResult.Value > 0)
            {
                Dictionary<Guid, DoctorPatientState> patientHealthReports =
                    await this.StateManager.GetStateAsync<Dictionary<Guid, DoctorPatientState>>("PersonHealthStatuses");
                HealthIndex avgHealth = this.indexCalculator.ComputeAverageIndex(patientHealthReports.Select(x => x.Value.HealthIndex));
                ActorEventSource.Current.ActorMessage(this, "Average patient Health Calculated: {0}", avgHealth);
                return avgHealth;
            }
            else
            {
                ActorEventSource.Current.ActorMessage(this, "No patient health available");
                return this.indexCalculator.ComputeIndex(-1);
            }
        }

        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            KeyedCollection<string, ConfigurationProperty> parameters = configSettings.Sections["HealthMetrics.DoctorActor.Settings"].Parameters;
            this.countyServiceInstanceUri = new ServiceUriBuilder(parameters["CountyServiceInstanceName"].Value).ToUri();
            this.bandActorServiceInstanceUri = new ServiceUriBuilder(parameters["BandActorServiceInstanceName"].Value).ToUri();
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }
    }
}