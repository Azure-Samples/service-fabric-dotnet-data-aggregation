// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using System.Web.Http;
    using HealthMetrics.Common;
    using Microsoft.Practices.Unity;
    using Microsoft.ServiceFabric.Data;
    using Unity.WebApi;

    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config, IReliableStateManager objectManager, HealthIndexCalculator indexCalculator)
        {
            UnityContainer container = new UnityContainer();

            container.RegisterType<CountyHealthController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(objectManager, indexCalculator));

            container.RegisterType<CountyDoctorsController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(objectManager, indexCalculator));

            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}