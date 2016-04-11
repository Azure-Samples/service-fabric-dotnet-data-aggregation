// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.DoctorActor
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using HealthMetrics.Common;

    [DataContract]
    internal class DoctorActorState
    {
        public DoctorActorState()
        {
            this.PersonHealthStatuses = new Dictionary<Guid, DoctorPatientState>();
        }

        [DataMember]
        public IDictionary<Guid, DoctorPatientState> PersonHealthStatuses { get; set; }

        [DataMember]
        public CountyRecord CountyInfo { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int HealthReportCount { get; set; }
    }
}