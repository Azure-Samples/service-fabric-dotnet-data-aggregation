// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using HealthMetrics.BandActor.Interfaces;
    using HealthMetrics.Common;

    [DataContract]
    internal class BandActorState
    {
        private static TimeSpan TimeWindow = TimeSpan.FromMinutes(2);

        [DataMember] private List<HeartRateRecord> heartRateHistory;

        public BandActorState()
        {
            this.heartRateHistory = new List<HeartRateRecord>();
        }

        [DataMember]
        public Guid DoctorId { get; set; }

        [DataMember]
        public CountyRecord CountyInfo { get; set; }

        [DataMember]
        public HealthIndex HealthIndex { get; set; }

        [DataMember]
        public string PatientName { get; set; }

        public IEnumerable<HeartRateRecord> HeartRateHistory
        {
            get { return this.heartRateHistory; }
        }

        public void AddHeartRateRecord(HeartRateRecord record)
        {
            this.heartRateHistory = this.heartRateHistory.Where(x => DateTimeOffset.UtcNow - x.Timestamp.ToUniversalTime() <= TimeWindow).ToList();
            this.heartRateHistory.Add(record);
        }
    }
}