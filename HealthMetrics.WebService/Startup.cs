// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.WebService
{
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Owin;
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Web.Http;
    using Web.Service;

    public class Startup : IOwinAppBuilder
    {
        private readonly ServiceContext serviceContext;

        public Startup(ServiceContext serviceContext)
        {
            this.serviceContext = serviceContext;
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            config.MapHttpAttributeRoutes();
            FormatterConfig.ConfigureFormatters(config.Formatters);
            UnityConfig.RegisterComponents(config, this.serviceContext);

            try
            {
                appBuilder.UseWebApi(config);
                appBuilder.UseFileServer(
                    new FileServerOptions()
                    {
                        EnableDefaultFiles = true,
                        RequestPath = PathString.Empty,
                        FileSystem = new PhysicalFileSystem(@".\wwwroot"),
                    });

                appBuilder.UseDefaultFiles(
                    new DefaultFilesOptions()
                    {
                        DefaultFileNames = new[] {"healthmetrics/index.html"}
                    });

                appBuilder.UseStaticFiles(
                    new StaticFileOptions()
                    {
                        FileSystem = new PhysicalFileSystem(@".\wwwroot\Content"),
                        RequestPath = PathString.FromUriComponent(@"/Content"),
                        ServeUnknownFileTypes = true
                    });

                config.EnsureInitialized();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
            }
        }
    }
}