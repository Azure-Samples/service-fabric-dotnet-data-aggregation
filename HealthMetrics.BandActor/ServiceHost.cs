// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using Microsoft.ServiceFabric.Actors.Runtime;
    using System;
    using System.Threading;

    public class ServiceHost
    {
        public static void Main(string[] args)
        {
            try
            {
                ActorRuntime.RegisterActorAsync<BandActor>();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e);
                throw;
            }
        }
    }
}