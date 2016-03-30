// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor.Interfaces
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public struct PatientDataViewModel
    {
        public PatientDataViewModel(
            Guid patientId,
            string name,
            int healthIndex,
            int heartRateIndex)
        {
            this.PatientId = patientId;
            this.PatientName = name;
            this.HealthIndex = healthIndex;
            this.HeartRateIndex = heartRateIndex;
        }

        [DataMember]
        public Guid PatientId { get; private set; }

        [DataMember]
        public string PatientName { get; private set; }

        [DataMember]
        public int HealthIndex { get; private set; }

        [DataMember]
        public int HeartRateIndex { get; private set; }
    }
}