// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Data;
    using Owin;
    using System.Web.Http;
    using Web.Service;

    /// <summary>
    /// OWIN configuration
    /// </summary>
    public class Startup : IOwinAppBuilder
    {
        private readonly IReliableStateManager objectManager;
        private readonly HealthIndexCalculator indexCalculator;

        public Startup(IReliableStateManager objectManager, HealthIndexCalculator indexCalculator)
        {
            this.objectManager = objectManager;
            this.indexCalculator = indexCalculator;
        }

        /// <summary>
        /// Configures the app builder using Web API.
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.MapHttpAttributeRoutes();

            FormatterConfig.ConfigureFormatters(config.Formatters);
            UnityConfig.RegisterComponents(config, this.objectManager, this.indexCalculator);

            appBuilder.UseWebApi(config);
            config.EnsureInitialized();
        }
    }
}