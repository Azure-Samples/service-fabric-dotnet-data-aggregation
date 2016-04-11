// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor.Interfaces
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public struct HeartRateRecord
    {
        [DataMember]
        public float HeartRate { get; private set; }

        [DataMember]
        public DateTimeOffset Timestamp { get; private set; }

        public HeartRateRecord(float heartRate)
        {
            this.HeartRate = heartRate;
            this.Timestamp = DateTimeOffset.UtcNow;
        }
    }
}