// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService
{
    using HealthMetrics.WebService.Controllers;
    using Microsoft.Practices.Unity;
    using System.Fabric;
    using System.Web.Http;
    using Unity.WebApi;

    public static class UnityConfig
    {
        public static void RegisterComponents(HttpConfiguration config, ServiceContext serviceContext)
        {
            UnityContainer container = new UnityContainer();

            container.RegisterType<DefaultApiController>(
                new TransientLifetimeManager(),
                new InjectionConstructor(serviceContext.CodePackageActivationContext.GetConfigurationPackageObject("Config").Settings));


            config.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}