// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor.Interfaces
{
    using System;
    using System.Threading.Tasks;
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Actors;

    public interface IDoctorActor : IActor
    {
        Task ReportHealthAsync(Guid personId, string personName, HealthIndex healthIndex);

        Task NewAsync(string name, CountyRecord countyRecord);

        //Task<DoctorDataViewModel> GetPatientsAsync();

        Task<Tuple<CountyRecord, string>> GetInfoAndNameAsync();
    }
}