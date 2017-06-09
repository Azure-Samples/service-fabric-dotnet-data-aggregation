// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor.Interfaces
{
    using Common;
    using System.Runtime.Serialization;

    [DataContract]
    public struct DoctorStatsViewModel
    {
        public DoctorStatsViewModel(int patientCount, long healthReportCount, HealthIndex averageHealthIndex, string doctorName)
        {
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorName = doctorName;
        }

        [DataMember]
        public int PatientCount { get; private set; }

        [DataMember]
        public long HealthReportCount { get; private set; }

        [DataMember]
        public HealthIndex AverageHealthIndex { get; private set; }

        [DataMember]
        public string DoctorName { get; private set; }
    }
}