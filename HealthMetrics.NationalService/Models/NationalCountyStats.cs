// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService.Models
{
    using Common;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct NationalCountyStats
    {
        public NationalCountyStats(int doctorCount, int patientCount, long healthReportCount, HealthIndex averageHealthIndex)
        {
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorCount = doctorCount;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
        }

        [DataMember]
        public int DoctorCount { get; private set; }

        [DataMember]
        public int PatientCount { get; private set; }

        [DataMember]
        public long HealthReportCount { get; private set; }

        [DataMember]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}