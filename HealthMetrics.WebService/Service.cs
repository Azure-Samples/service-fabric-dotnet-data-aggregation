// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService
{
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Collections.Generic;
    using System.Fabric;
    using Web.Service;
    using System.Threading;
    using System.Threading.Tasks;

    public class Service : StatelessService
    {
        public const string ServiceTypeName = "HealthMetrics.WebServiceType";

        public Service(StatelessServiceContext serviceContext) : base(serviceContext)
        {
            
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(
                    (parameters) =>
                        new HttpCommunicationListener("", new Startup(this.Context), parameters))
            };
        }
    }
}