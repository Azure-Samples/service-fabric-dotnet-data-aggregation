// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Threading.Tasks;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;
    using HealthMetrics.DoctorActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using Microsoft.ServiceFabric.Data;

    internal class BandActor : Actor, IBandActor, IRemindable
    {
        private const string GenerateHealthDataAsyncReminder = "GenerateHealthDataAsync";
        private const string SendHealthReportAsyncReminder = "SendHealthReportAsync";

        private Uri doctorActorServiceUri;
        private CryptoRandom random = new CryptoRandom();
        private HealthIndexCalculator indexCalculator;

        public BandActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        { }

        public async Task<BandDataViewModel> GetBandDataAsync()
        {
            try
            {
                ConditionalValue<BandActorState> BandActorStateResult = await this.StateManager.TryGetStateAsync<BandActorState>("BandActorState");

                if (BandActorStateResult.HasValue)
                {
                    BandActorState state = BandActorStateResult.Value;

                    HealthIndexCalculator ic = this.indexCalculator;
                    HealthIndex hi = state.HealthIndex;

                    int healthIndex = ic.ComputeIndex(hi);

                    return new BandDataViewModel(
                        state.DoctorId,
                        this.Id.GetGuidId(),
                        state.PatientName,
                        state.CountyInfo,
                        healthIndex,
                        state.HeartRateHistory);
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format("Exception inside band actor {0}|{1}|{2}", this.Id, this.Id.Kind, e));
            }

            throw new ArgumentException(string.Format("No band actor state {0}|{1}|{2}", this.Id, this.Id.Kind));
        }

        public async Task NewAsync(BandInfo info)
        {
            ConditionalValue<BandActorState> BandActorStateResult = await this.StateManager.TryGetStateAsync<BandActorState>("BandActorState");

            if (!BandActorStateResult.HasValue)
            {
                BandActorState state = new BandActorState();
                state.CountyInfo = info.CountyInfo;
                state.DoctorId = info.DoctorId;
                state.HealthIndex = info.HealthIndex;
                state.PatientName = info.PersonName;

                await this.StateManager.SetStateAsync<BandActorState>("BandActorState", state);
                await this.RegisterReminders();

                ActorEventSource.Current.ActorMessage(this, "Band created. ID: {0}, Name: {1}, Doctor ID: {2}", this.Id, state.PatientName, state.DoctorId);
            }
        }

        async Task IRemindable.ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            switch (reminderName)
            {
                case SendHealthReportAsyncReminder:
                    await this.SendHealthReportAsync();
                    break;

                case GenerateHealthDataAsyncReminder:
                    await this.GenerateHealthDataAsync();
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

        private async Task SendHealthReportAsync()
        {
            try
            {
                ConditionalValue<BandActorState> BandActorStateResult = await this.StateManager.TryGetStateAsync<BandActorState>("BandActorState");

                if (BandActorStateResult.HasValue)
                {
                    ActorId doctorId = new ActorId(BandActorStateResult.Value.DoctorId);

                    IDoctorActor doctor = ActorProxy.Create<IDoctorActor>(doctorId, this.doctorActorServiceUri);

                    await
                        doctor.ReportHealthAsync(
                            this.Id.GetGuidId(),
                            BandActorStateResult.Value.PatientName,
                            BandActorStateResult.Value.HealthIndex,
                            new HealthIndex(this.random.Next(0, 101)));

                    ActorEventSource.Current.Message("Health info sent from band {0} to doctor {1}", this.Id, BandActorStateResult.Value.DoctorId);
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

        private async Task GenerateHealthDataAsync()
        {
            ConditionalValue<BandActorState> BandActorStateResult = await this.StateManager.TryGetStateAsync<BandActorState>("BandActorState");

            if (BandActorStateResult.HasValue)
            {
                BandActorStateResult.Value.AddHeartRateRecord(new HeartRateRecord((float) this.random.NextDouble()));
                await this.StateManager.SetStateAsync<BandActorState>("BandActorState", BandActorStateResult.Value);
            }
            return;
        }

        private async Task RegisterReminders()
        {
            await this.RegisterReminderAsync(GenerateHealthDataAsyncReminder, null, TimeSpan.FromSeconds(this.random.Next(5, 15)), TimeSpan.FromSeconds(5));
            await this.RegisterReminderAsync(SendHealthReportAsyncReminder, null, TimeSpan.FromSeconds(this.random.Next(5, 15)), TimeSpan.FromSeconds(10));
        }
    }
}