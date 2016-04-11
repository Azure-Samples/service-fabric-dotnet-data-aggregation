// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System.Web.Http;
    using Microsoft.ServiceFabric.Data;
    using Owin;
    using Web.Service;

    /// <summary>
    /// OWIN configuration
    /// </summary>
    public class Startup : IOwinAppBuilder
    {
        private readonly IReliableStateManager objectManager;

        public Startup(IReliableStateManager objectManager)
        {
            this.objectManager = objectManager;
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
            UnityConfig.RegisterComponents(config, this.objectManager);

            appBuilder.UseWebApi(config);

            config.EnsureInitialized();
        }
    }
}