// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public struct NationalStatsViewModel
    {
        public NationalStatsViewModel(long doctorCount, long patientCount, long healthReportCount, long averageHealthIndex, DateTimeOffset creationDateTime)
        {
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorCount = doctorCount;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.StartTimeOffset = creationDateTime;
        }

        [DataMember]
        public long DoctorCount { get; private set; }

        [DataMember]
        public long PatientCount { get; private set; }

        [DataMember]
        public long HealthReportCount { get; private set; }

        [DataMember]
        public long AverageHealthIndex { get; private set; }

        [DataMember]
        public DateTimeOffset StartTimeOffset { get; private set; }
    }
}