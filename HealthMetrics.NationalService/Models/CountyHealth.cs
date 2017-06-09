// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService.Models
{
    using Common;
    using System.Runtime.Serialization;

    [DataContract]
    public struct CountyHealth
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public HealthIndex Health { get; set; }
    }
}