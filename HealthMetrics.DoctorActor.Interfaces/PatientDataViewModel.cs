// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor.Interfaces
{
    using System;
    using System.Runtime.Serialization;
    using HealthMetrics.Common;

    [DataContract]
    public struct PatientDataViewModel
    {
        public PatientDataViewModel(
            Guid patientId,
            string name,
            HealthIndex healthIndex)
        {
            this.PatientId = patientId;
            this.PatientName = name;
            this.HealthIndex = healthIndex;
        }

        [DataMember]
        public Guid PatientId { get; private set; }

        [DataMember]
        public string PatientName { get; private set; }

        [DataMember]
        public HealthIndex HealthIndex { get; private set; }
    }
}