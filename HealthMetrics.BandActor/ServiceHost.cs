// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using Microsoft.ServiceFabric.Actors.Runtime;
    using System;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Net;
    using System.Threading;

    public class ServiceHost
    {
        public static void Main(string[] args)
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = 1024;
                ServicePointManager.SetTcpKeepAlive(true, 2000, 1000);
                ServicePointManager.UseNagleAlgorithm = false;

                ActorRuntime.RegisterActorAsync<BandActor>();

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
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                Thread.Sleep(TimeSpan.FromMinutes(1));
                throw;
            }
        }
    }
}