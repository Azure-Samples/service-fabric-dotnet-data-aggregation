// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ServicePrimer
    {
        private static FabricClient fabricClient;
        private readonly TimeSpan interval = TimeSpan.FromSeconds(5);

        public ServicePrimer()
        {
        }

        protected FabricClient Client
        {
            get
            {
                if (fabricClient == null)
                {
                    fabricClient = new FabricClient();
                }

                return fabricClient;
            }
        }

        public async Task WaitForStatefulService(Uri serviceInstanceUri, CancellationToken token)
        {
            int maxRetryCount = 100;
            int currentAttempt = 0;
            bool complete = false;

            while (currentAttempt < maxRetryCount && !complete && !token.IsCancellationRequested)
            {
                try
                {

                    StatefulServiceDescription description =
                        await this.Client.ServiceManager.GetServiceDescriptionAsync(serviceInstanceUri) as StatefulServiceDescription;

                    int targetTotalReplicas = description.TargetReplicaSetSize;
                    if (description.PartitionSchemeDescription is UniformInt64RangePartitionSchemeDescription)
                    {
                        targetTotalReplicas *= ((UniformInt64RangePartitionSchemeDescription)description.PartitionSchemeDescription).PartitionCount;
                    }

                    ServicePartitionList partitions = await this.Client.QueryManager.GetPartitionListAsync(serviceInstanceUri);
                    int replicaTotal = 0;

                    while (replicaTotal < targetTotalReplicas && !token.IsCancellationRequested)
                    {
                        await Task.Delay(this.interval, token);

                        replicaTotal = 0;
                        foreach (Partition partition in partitions)
                        {
                            ServiceReplicaList replicaList = await this.Client.QueryManager.GetReplicaListAsync(partition.PartitionInformation.Id);

                            replicaTotal += replicaList.Count(x => x.ReplicaStatus == System.Fabric.Query.ServiceReplicaStatus.Ready);
                        }
                    }

                    complete = true;
                }
                catch (Exception e)
                {

                }
                finally
                {
                    await Task.Delay(this.interval, token);
                    currentAttempt++;
                }
            }
        }
    }
}