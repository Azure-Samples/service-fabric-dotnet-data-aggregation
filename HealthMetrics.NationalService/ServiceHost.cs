// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Net;
    using System.Threading;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class ServiceHost
    {
        public static void Main(string[] args)
        {
            try
            {
                //while(!Debugger.IsAttached)
                //{
                //    Thread.Sleep(TimeSpan.FromSeconds(5));
                //}

                ServicePointManager.DefaultConnectionLimit = 1024;
                ServicePointManager.SetTcpKeepAlive(true, 2000, 1000);
                ServicePointManager.UseNagleAlgorithm = false;

                ServiceRuntime.RegisterServiceAsync(Service.ServiceTypeName, context => new Service(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, Service.ServiceTypeName);

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                var cx = FabricRuntime.GetActivationContext();
                HealthInformation info = new HealthInformation("ProcessHost", "HostCrashing", HealthState.Error);
                info.Description = e.ToString();
                info.TimeToLive = TimeSpan.FromMinutes(2);
                info.RemoveWhenExpired = true;
                cx.ReportDeployedServicePackageHealth(info);
                ServiceEventSource.Current.ServiceHostInitializationFailed(e);
                Thread.Sleep(TimeSpan.FromMinutes(1));
                throw;
            }
        }
    }
}