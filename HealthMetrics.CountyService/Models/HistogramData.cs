// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public struct HistogramData
    {
        [DataMember]
        public string Bin { get; set; }

        [DataMember]
        public int Value { get; set; }
    }
}