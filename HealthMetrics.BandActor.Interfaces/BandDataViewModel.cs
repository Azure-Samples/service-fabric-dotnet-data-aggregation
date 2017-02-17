// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor.Interfaces
{
    using HealthMetrics.Common;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public struct BandDataViewModel
    {
        public BandDataViewModel(
            Guid doctorId,
            Guid bandId,
            string patientName,
            CountyRecord countyInfo,
            int healthIndex,
            IEnumerable<HeartRateRecord> heartRateHistory)
        {
            this.DoctorId = doctorId;
            this.PersonId = bandId;
            this.PersonName = patientName;
            this.CountyInfo = countyInfo;
            this.HealthIndex = healthIndex;
            this.HeartRateHistory = heartRateHistory;
        }

        [DataMember]
        public Guid DoctorId { get; private set; }

        [DataMember]
        public Guid PersonId { get; private set; }

        [DataMember]
        public string PersonName { get; private set; }

        [DataMember]
        public CountyRecord CountyInfo { get; private set; }

        [DataMember]
        public int HealthIndex { get; private set; }

        [DataMember]
        public IEnumerable<HeartRateRecord> HeartRateHistory { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}|{4}", this.DoctorId, this.PersonId, this.PersonName, this.CountyInfo, this.HealthIndex);
        }
    }
}