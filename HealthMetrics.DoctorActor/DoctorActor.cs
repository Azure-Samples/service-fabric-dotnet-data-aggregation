// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor
{
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Newtonsoft.Json;
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    [StatePersistence(StatePersistence.Persisted)]
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
        { }

        public async Task NewAsync(string name, CountyRecord countyRecord)
        {
            ConditionalValue<DoctorActorState> result = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");

            if (!result.HasValue)
            {
                DoctorActorState state = new DoctorActorState();

                state = new DoctorActorState();
                state.Name = "Dr. " + name;
                state.CountyInfo = countyRecord;
                state.HealthReportCount = 0;
                await this.RegisterReminderAsync(GenerateHealthDataAsyncReminder, null, TimeSpan.FromSeconds(this.random.Next(5, 15)), TimeSpan.FromSeconds(5));
                ActorEventSource.Current.ActorMessage(this, "Doctor created. ID: {0}. Name: {1}", this.Id.GetGuidId(), name);
                await this.StateManager.SetStateAsync("DoctorActorState", state);
            }
        }

        public async Task ReportHealthAsync(Guid personId, string personName, HealthIndex healthIndex, HealthIndex heartRateIndex)
        {
            ConditionalValue<DoctorActorState> doctorActorStateResult = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");

            if (doctorActorStateResult.HasValue)
            {
                DoctorActorState state = doctorActorStateResult.Value;

                state.PersonHealthStatuses[personId] = new DoctorPatientState(personId, personName, healthIndex, heartRateIndex);
                state.HealthReportCount++;

                await this.StateManager.SetStateAsync<DoctorActorState>("DoctorActorState", state);

                ActorEventSource.Current.Message(
                    "DoctorActor {0} Recieved health report from band {1} with value {2}",
                    this.Id.GetGuidId(),
                    personId,
                    healthIndex);
            }

            return;
        }

        public async Task<DoctorDataViewModel> GetPatientsAsync()
        {
            try
            {
                ConditionalValue<DoctorActorState> doctorActorStateResult = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");

                if (doctorActorStateResult.HasValue)
                {
                    DoctorActorState state = doctorActorStateResult.Value;

                    if (state.HealthReportCount == 0)
                    {
                        return new DoctorDataViewModel(state.Name, 0, state.CountyInfo, Enumerable.Empty<PatientDataViewModel>());
                    }

                    int patientAverage = await this.GetAveragePatientHealthInfoAsync();

                    ActorEventSource.Current.ActorMessage(
                        this,
                        "Doctor {0} sending doctor view for county {1} with average {2}",
                        this.Id.GetGuidId(),
                        state.CountyInfo,
                        patientAverage);

                    return new DoctorDataViewModel(
                        state.Name,
                        patientAverage,
                        state.CountyInfo,
                        state.PersonHealthStatuses.Select(
                            x =>
                                new PatientDataViewModel(
                                    x.Key,
                                    x.Value.Name,
                                    this.indexCalculator.ComputeIndex(x.Value.HealthIndex),
                                    this.indexCalculator.ComputeIndex(x.Value.HeartRateIndex))));
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Exception inside doctor actor {0}|{1}|{2}", this.Id, this.Id.Kind, e));
            }

            throw new ArgumentException(string.Format("No actor state in actor {0}", this.Id));
        }

        public async Task<Tuple<CountyRecord, string>> GetInfoAndNameAsync()
        {
            ConditionalValue<DoctorActorState> doctorActorStateResult = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");

            if (doctorActorStateResult.HasValue)
            {
                return new Tuple<CountyRecord, string>(doctorActorStateResult.Value.CountyInfo, doctorActorStateResult.Value.Name);
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
                ConditionalValue<DoctorActorState> doctorStateResult = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");
                if (doctorStateResult.HasValue)
                {
                    DoctorActorState state = doctorStateResult.Value;

                    if (state.PersonHealthStatuses.Count > 0)
                    {
                        DoctorStatsViewModel payload = new DoctorStatsViewModel(
                            state.PersonHealthStatuses.Count,
                            state.HealthReportCount,
                            await this.GetAveragePatientHealthInfoAsync(),
                            state.Name);

                        ServicePartitionKey partitionKey = new ServicePartitionKey(state.CountyInfo.CountyId);
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


        private async Task<int> GetAveragePatientHealthInfoAsync()
        {
            int avgHealth = 0;

            ConditionalValue<DoctorActorState> doctorActorStateResult = await this.StateManager.TryGetStateAsync<DoctorActorState>("DoctorActorState");

            if (doctorActorStateResult.HasValue)
            {
                if (doctorActorStateResult.Value.HealthReportCount > 0)
                {
                    avgHealth = this.indexCalculator.ComputeAverageIndex(doctorActorStateResult.Value.PersonHealthStatuses.Select(x => x.Value.HealthIndex));
                    ActorEventSource.Current.ActorMessage(this, "Average patient Health Calculated: {0}", avgHealth);
                }
            }

            return avgHealth;
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