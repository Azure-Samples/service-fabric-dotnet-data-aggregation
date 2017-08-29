// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;
    using System.Threading.Tasks;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    //[StatePersistence(StatePersistence.Volatile)]
    internal class BandActor : Actor, IBandActor, IRemindable
    {
        private const string GenerateHealthDataAsyncReminder = "GenerateHealthDataAsync";
        private const string GenerateAndSendHealthReportReminder = "SendHealthReportAsync";
        private readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(2);

        private Uri doctorActorServiceUri;
        private CryptoRandom random = new CryptoRandom();
        private HealthIndexCalculator indexCalculator;

        public BandActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task<BandDataViewModel> GetBandDataAsync()
        {
            try
            {
                //check to see if the patient name is set
                //if not this actor object hasn't been initialized
                //and we can skip the rest of the checks
                ConditionalValue<string> PatientInfoResult = await this.StateManager.TryGetStateAsync<string>("PatientName");

                if (PatientInfoResult.HasValue)
                {
                    ConditionalValue<CountyRecord> CountyInfoResult = await this.StateManager.TryGetStateAsync<CountyRecord>("CountyInfo");
                    ConditionalValue<Guid> DoctorInfoResult = await this.StateManager.TryGetStateAsync<Guid>("DoctorId");
                    ConditionalValue<HealthIndex> HeatlthInfoResult = await this.StateManager.TryGetStateAsync<HealthIndex>("HealthIndex");
                    ConditionalValue<List<HeartRateRecord>> HeartRateRecords =
                        await this.StateManager.TryGetStateAsync<List<HeartRateRecord>>("HeartRateRecords");

                    HealthIndexCalculator ic = this.indexCalculator;

                    HealthIndex healthIndex = ic.ComputeIndex(HeatlthInfoResult.Value);

                    return new BandDataViewModel(
                        DoctorInfoResult.Value,
                        this.Id.GetGuidId(),
                        PatientInfoResult.Value,
                        CountyInfoResult.Value,
                        healthIndex,
                        HeartRateRecords.Value);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Exception inside band actor {0}|{1}|{2}", this.Id, this.Id.Kind, e));
            }

            throw new ArgumentException(string.Format("No band actor state {0}|{1}", this.Id, this.Id.Kind));
        }

        public async Task NewAsync(BandInfo info)
        {
            await this.StateManager.SetStateAsync<CountyRecord>("CountyInfo", info.CountyInfo);
            await this.StateManager.SetStateAsync<Guid>("DoctorId", info.DoctorId);
            await this.StateManager.SetStateAsync<HealthIndex>("HealthIndex", info.HealthIndex);
            await this.StateManager.SetStateAsync<string>("PatientName", info.PersonName);
            await this.StateManager.SetStateAsync<List<HeartRateRecord>>("HeartRateRecords", new List<HeartRateRecord>());
            await this.RegisterReminders();

            ActorEventSource.Current.ActorMessage(this, "Band created. ID: {0}, Name: {1}, Doctor ID: {2}", this.Id, info.PersonName, info.DoctorId);
        }

        async Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case GenerateAndSendHealthReportReminder:
                    await this.GenerateAndSendHealthReportAsync();
                    break;

                default:
                    ActorEventSource.Current.Message("Reminder {0} is not implemented on BandActor.", reminderName);
                    break;
            }

            return;
        }

        protected override Task OnActivateAsync()
        {
            ConfigurationPackage configPackage = this.ActorService.Context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            this.indexCalculator = new HealthIndexCalculator(this.ActorService.Context);
            this.UpdateConfigSettings(configPackage.Settings);
            this.ActorService.Context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                this.CodePackageActivationContext_ConfigurationPackageModifiedEvent;
            ActorEventSource.Current.ActorMessage(this, "Band activated. ID: {0}", this.Id);
            return Task.FromResult(true);
        }

        private async Task GenerateAndSendHealthReportAsync()
        {
            try
            {
                ConditionalValue<HealthIndex> HeatlthInfoResult = await this.StateManager.TryGetStateAsync<HealthIndex>("HealthIndex");
                ConditionalValue<string> PatientInfoResult = await this.StateManager.TryGetStateAsync<string>("PatientName");
                ConditionalValue<Guid> DoctorInfoResult = await this.StateManager.TryGetStateAsync<Guid>("DoctorId");

                if (HeatlthInfoResult.HasValue && PatientInfoResult.HasValue && DoctorInfoResult.HasValue)
                {
                    ActorId doctorId = new ActorId(DoctorInfoResult.Value);
                    HeartRateRecord record = new HeartRateRecord((float) this.random.NextDouble());

                    await this.SaveHealthDataAsync(record);

                    IDoctorActor doctor = ActorProxy.Create<IDoctorActor>(doctorId, this.doctorActorServiceUri);

                    await
                        doctor.ReportHealthAsync(
                            this.Id.GetGuidId(),
                            PatientInfoResult.Value,
                            HeatlthInfoResult.Value);

                    ActorEventSource.Current.Message("Health info sent from band {0} to doctor {1}", this.Id, DoctorInfoResult.Value);
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.Message(
                    "Band Actor failed to send health data to doctor. Exception: {0}",
                    (e is AggregateException) ? e.InnerException.ToString() : e.ToString());
            }

            return;
        }

        private void UpdateConfigSettings(ConfigurationSettings configSettings)
        {
            KeyedCollection<string, ConfigurationProperty> parameters = configSettings.Sections["HealthMetrics.BandActor.Settings"].Parameters;

            this.doctorActorServiceUri = new ServiceUriBuilder(parameters["DoctorActorServiceInstanceName"].Value).ToUri();
        }

        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            this.UpdateConfigSettings(e.NewPackage.Settings);
        }

        private async Task SaveHealthDataAsync(HeartRateRecord newRecord)
        {
            ConditionalValue<List<HeartRateRecord>> HeartRateRecords = await this.StateManager.TryGetStateAsync<List<HeartRateRecord>>("HeartRateRecords");

            if (HeartRateRecords.HasValue)
            {
                List<HeartRateRecord> records = HeartRateRecords.Value;
                records = records.Where(x => DateTimeOffset.UtcNow - x.Timestamp.ToUniversalTime() <= this.TimeWindow).ToList();
                records.Add(newRecord);
                await this.StateManager.SetStateAsync<List<HeartRateRecord>>("HeartRateRecords", records);
            }

            return;
        }

        private async Task RegisterReminders()
        {
            await this.RegisterReminderAsync(GenerateAndSendHealthReportReminder, null, TimeSpan.FromSeconds(this.random.Next(5, 30)), TimeSpan.FromSeconds(1));
        }
    }
}