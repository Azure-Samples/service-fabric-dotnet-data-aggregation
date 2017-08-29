// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Collections.Generic;
    using Microsoft.ServiceFabric.Data.Collections;
    using System.Fabric;
    using Web.Service;
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceFabric.Data.Notifications;
    using HealthMetrics.NationalService.Models;

    public class Service : StatefulService
    {
        public const string ServiceTypeName = "HealthMetrics.NationalServiceType";
        private const string HealthStatusDictionary = "healthStatusDictionary";
        private const string TimeStatsDictionary = "TimeTracker";
        private readonly ConcurrentDictionary<string, long> statsDictionary = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<int, DataSet> historyDictionary = new ConcurrentDictionary<int, DataSet>();
        private ConcurrentBag<int> updatedCounties = new ConcurrentBag<int>();

        public Service(StatefulServiceContext serviceContext) : base(serviceContext)
        {
            this.StateManager.StateManagerChanged += StateManager_StateManagerChanged;
        }

        public Service(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica)
            : base(serviceContext, reliableStateManagerReplica)
        {
            this.StateManager.StateManagerChanged += StateManager_StateManagerChanged;
        }

        private void StateManager_StateManagerChanged(object sender, NotifyStateManagerChangedEventArgs e)
        {
            if (e.Action == NotifyStateManagerChangedAction.Add)
            {
                var args = e as NotifyStateManagerSingleEntityChangedEventArgs;
                if (args.ReliableState.Name.ToString() == "urn:" + HealthStatusDictionary)
                {
                    var dictionary = (IReliableDictionary<int, NationalCountyStats>)args.ReliableState;
                    dictionary.DictionaryChanged += Dictionary_DictionaryChanged;
                }
            }
        }

        private void Dictionary_DictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<int, NationalCountyStats> e)
        {
            switch (e.Action)
            {
                case NotifyDictionaryChangedAction.Clear:
                    return;

                case NotifyDictionaryChangedAction.Add:
                    var addEvent = e as NotifyDictionaryItemAddedEventArgs<int, NationalCountyStats>;

                    long tmp = -1;

                    if (statsDictionary.TryGetValue("totalDoctors", out tmp))
                    {
                        statsDictionary["totalDoctors"] += addEvent.Value.DoctorCount;
                        statsDictionary["totalPatientCount"] += addEvent.Value.PatientCount;
                        statsDictionary["totalHealthReportCount"] += addEvent.Value.HealthReportCount;
                    }
                    else
                    {
                        statsDictionary["totalDoctors"] = addEvent.Value.DoctorCount;
                        statsDictionary["totalPatientCount"] = addEvent.Value.PatientCount;
                        statsDictionary["totalHealthReportCount"] = addEvent.Value.HealthReportCount;
                    }

                    historyDictionary[addEvent.Key] = new DataSet(addEvent.Value.DoctorCount, addEvent.Value.PatientCount, addEvent.Value.HealthReportCount);
                    return;

                case NotifyDictionaryChangedAction.Update:
                    var updateEvent = e as NotifyDictionaryItemUpdatedEventArgs<int, NationalCountyStats>;
                    statsDictionary["totalDoctors"] += (updateEvent.Value.DoctorCount - historyDictionary[updateEvent.Key].totalDoctors);
                    statsDictionary["totalPatientCount"] += (updateEvent.Value.PatientCount - historyDictionary[updateEvent.Key].totalPatientCount);
                    statsDictionary["totalHealthReportCount"] += (updateEvent.Value.HealthReportCount - historyDictionary[updateEvent.Key].totalHealthReportCount);
                    historyDictionary[updateEvent.Key] = new DataSet(updateEvent.Value.DoctorCount, updateEvent.Value.PatientCount, updateEvent.Value.HealthReportCount);
                    return;

                case NotifyDictionaryChangedAction.Remove:
                    return;

                default:
                    break;
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>()
            {
                new ServiceReplicaListener(
                    (initParams) =>
                        new HttpCommunicationListener("healthnational", new Startup(this.StateManager, this.updatedCounties, this.statsDictionary), this.Context))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (!cancellationToken.IsCancellationRequested && retryCount < 5)
            {
                try
                {
                    var timeDictionary = await StateManager.GetOrAddAsync<IReliableDictionary<string, DateTimeOffset>>("TimeTracker");

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        var result = await timeDictionary.TryGetValueAsync(tx, "StartTime");
                        if (!result.HasValue)
                        {
                            await timeDictionary.SetAsync(tx, "StartTime", DateTimeOffset.UtcNow);
                        }

                        await tx.CommitAsync();

                    }

                    return;
                }
                catch (TimeoutException te)
                {
                    // transient error. Retry.
                    retryCount++;
                    ServiceEventSource.Current.ServiceMessage(this, "NationalService encountered an exception trying to record start time: TimeoutException in RunAsync: {0}", te.ToString());
                    continue;
                }
                catch (FabricNotReadableException)
                {
                    // transient error. Retry.
                    retryCount++;
                    continue;
                }
                catch (FabricTransientException fte)
                {
                    // transient error. Retry.
                    retryCount++;
                    ServiceEventSource.Current.ServiceMessage(this, "NationalService encountered an exception trying to record start time: FabricTransientException in RunAsync: {0}", fte.ToString());
                    continue;
                }
                catch (FabricNotPrimaryException)
                {
                    // not primary any more, time to quit.
                    return;
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(this, ex.ToString());
                    throw;
                }
            }
        }
    }

    struct DataSet
    {
        public long totalDoctors;
        public long totalPatientCount;
        public long totalHealthReportCount;

        public DataSet(long doctors, long patients, long reports)
        {
            this.totalDoctors = doctors;
            this.totalPatientCount = patients;
            this.totalHealthReportCount = reports;
        }
    }
}