// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService
{
    using Microsoft.ServiceFabric.Services.Runtime;
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;

    public class ServiceHost
    {
        public static void Main(string[] args)
        {
            try
            {
                ServicePointManager.DefaultConnectionLimit = 10240;

                ServiceRuntime.RegisterServiceAsync(Service.ServiceTypeName, context => new Service(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, Service.ServiceTypeName);

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e);
            }
        }
    }
}