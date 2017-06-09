// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Web.Service;

    public class Service : StatefulService
    {
        public const string ServiceTypeName = "HealthMetrics.NationalServiceType";
        private ConcurrentBag<int> updatedCounties = new ConcurrentBag<int>();

        public Service(StatefulServiceContext serviceContext) : base(serviceContext)
        {
        }

        public Service(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica)
            : base(serviceContext, reliableStateManagerReplica)
        {
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>()
            {
                new ServiceReplicaListener(
                    (initParams) =>
                        new HttpCommunicationListener("healthnational", new Startup(this.StateManager, this.updatedCounties), this.Context))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (!cancellationToken.IsCancellationRequested && retryCount < 5)
            {
                try
                {
                    IReliableDictionary<string, DateTimeOffset> timeDictionary =
                        await this.StateManager.GetOrAddAsync<IReliableDictionary<string, DateTimeOffset>>("TimeTracker");

                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        ConditionalValue<DateTimeOffset> result = await timeDictionary.TryGetValueAsync(tx, "StartTime");
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
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "NationalService encountered an exception trying to record start time: TimeoutException in RunAsync: {0}",
                        te.ToString());
                    continue;
                }
                catch (FabricTransientException fte)
                {
                    // transient error. Retry.
                    retryCount++;
                    ServiceEventSource.Current.ServiceMessage(
                        this,
                        "NationalService encountered an exception trying to record start time: FabricTransientException in RunAsync: {0}",
                        fte.ToString());
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
}