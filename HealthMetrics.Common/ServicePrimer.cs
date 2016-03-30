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

        public async Task WaitForStatefulService(Uri serviceInstanceUri)
        {
            StatefulServiceDescription description =
                await this.Client.ServiceManager.GetServiceDescriptionAsync(serviceInstanceUri) as StatefulServiceDescription;

            int targetTotalReplicas = description.TargetReplicaSetSize;
            if (description.PartitionSchemeDescription is UniformInt64RangePartitionSchemeDescription)
            {
                targetTotalReplicas *= ((UniformInt64RangePartitionSchemeDescription) description.PartitionSchemeDescription).PartitionCount;
            }

            ServicePartitionList partitions = await this.Client.QueryManager.GetPartitionListAsync(serviceInstanceUri);
            int replicaTotal = 0;

            while (replicaTotal < targetTotalReplicas)
            {
                await Task.Delay(this.interval);
                //ServiceEventSource.Current.ServiceMessage(this, "CountyService waiting for National Service to come up.");

                replicaTotal = 0;
                foreach (Partition partition in partitions)
                {
                    ServiceReplicaList replicaList = await this.Client.QueryManager.GetReplicaListAsync(partition.PartitionInformation.Id);

                    replicaTotal += replicaList.Count(x => x.ReplicaStatus == System.Fabric.Query.ServiceReplicaStatus.Ready);
                }
            }
        }
    }
}