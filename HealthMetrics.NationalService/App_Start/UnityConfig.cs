// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using Microsoft.Practices.Unity;
    using Microsoft.ServiceFabric.Data;
    using System.Collections.Concurrent;
    using System.Web.Http;
    using Unity.WebApi;

    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config, IReliableStateManager objectManager, ConcurrentBag<int> updatedCounties)
        {
            UnityContainer container = new UnityContainer();

            container.RegisterType<NationalHealthController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(objectManager, updatedCounties));

            container.RegisterType<NationalStatsController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(objectManager));

            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}