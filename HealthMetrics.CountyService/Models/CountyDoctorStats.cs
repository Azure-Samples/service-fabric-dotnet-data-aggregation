// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using HealthMetrics.Common;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct CountyDoctorStats
    {
        public CountyDoctorStats(int patientCount, int healthReportCount, string doctorName, HealthIndex averageHealthIndex)
        {
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorName = doctorName;
        }

        [DataMember]
        public string DoctorName { get; private set; }

        [DataMember]
        public int PatientCount { get; private set; }

        [DataMember]
        public int HealthReportCount { get; private set; }

        [DataMember]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}