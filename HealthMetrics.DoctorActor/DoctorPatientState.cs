// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor
{
    using System;
    using System.Runtime.Serialization;
    using HealthMetrics.Common;

    [DataContract]
    internal struct DoctorPatientState
    {
        public DoctorPatientState(Guid id, string name, HealthIndex healthIndex, HealthIndex heartRateIndex)
        {
            this.Id = id;
            this.Name = name;
            this.HealthIndex = healthIndex;
            this.HeartRateIndex = heartRateIndex;
        }

        [DataMember]
        public Guid Id { get; private set; }

        [DataMember]
        public string Name { get; private set; }

        [DataMember]
        public HealthIndex HealthIndex { get; private set; }

        [DataMember]
        public HealthIndex HeartRateIndex { get; private set; }
    }
}