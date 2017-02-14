// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.BandActor.Interfaces
{
    using HealthMetrics.Common;
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public struct BandInfo
    {
        [DataMember]
        public Guid DoctorId { get; set; }

        [DataMember]
        public CountyRecord CountyInfo { get; set; }

        [DataMember]
        public HealthIndex HealthIndex { get; set; }

        [DataMember]
        public string PersonName { get; set; }

        public static bool operator ==(BandInfo a, BandInfo b)
        {
            if (a.DoctorId == b.DoctorId
                && a.CountyInfo == b.CountyInfo
                && a.HealthIndex == b.HealthIndex
                && a.PersonName == b.PersonName)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(BandInfo a, BandInfo b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return (this == (BandInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashUtil.getIntHashCode(this.ToString());
        }
    }
}