// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor.Interfaces
{
    using HealthMetrics.Common;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public struct DoctorDataViewModel
    {
        public DoctorDataViewModel(string doctorName, int averagePatientHealthIndex, CountyRecord county, IEnumerable<PatientDataViewModel> patients)
        {
            this.DoctorName = doctorName;
            this.AveragePatientHealthIndex = averagePatientHealthIndex;
            this.Patients = patients;
            this.CountyInfo = county;
        }

        [DataMember]
        public string DoctorName { get; private set; }

        [DataMember]
        public CountyRecord CountyInfo { get; private set; }

        [DataMember]
        public int AveragePatientHealthIndex { get; private set; }

        [DataMember]
        public IEnumerable<PatientDataViewModel> Patients { get; private set; }
    }
}