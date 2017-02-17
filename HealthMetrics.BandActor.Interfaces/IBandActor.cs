// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor.Interfaces
{
    using Microsoft.ServiceFabric.Actors;
    using System.Threading.Tasks;

    public interface IBandActor : IActor
    {
        Task NewAsync(BandInfo info);

        Task<BandDataViewModel> GetBandDataAsync();
    }
}