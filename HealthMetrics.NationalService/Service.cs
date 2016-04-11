// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System.Collections.Generic;
    using System.Fabric;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using Web.Service;

    public class Service : StatefulService
    {
        public const string ServiceTypeName = "HealthMetrics.NationalServiceType";

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
                        new HttpCommunicationListener("healthnational", new Startup(this.StateManager), this.Context))
            };
        }
    }
}