// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public struct HealthIndex : IComparable, IComparable<HealthIndex>, IEquatable<HealthIndex>
    {
        [DataMember] private int value;
        [DataMember] private bool mode;

        public HealthIndex(int value, bool mode)
        {
            this.value = value;
            this.mode = mode;
        }

        public int CompareTo(HealthIndex other)
        {
            return this.value.CompareTo(other.value);
        }

        //public static explicit operator HealthIndex(int value, bool mode)
        //{
        //    return new HealthIndex(value, mode);
        //}

        public static bool operator ==(HealthIndex item1, HealthIndex item2)
        {
            return item1.Equals(item2);
        }

        public static bool operator !=(HealthIndex item1, HealthIndex item2)
        {
            return !item1.Equals(item2);
        }

        public static bool operator >(HealthIndex item1, HealthIndex item2)
        {
            return item1.value > item2.value;
        }

        public static bool operator >=(HealthIndex item1, HealthIndex item2)
        {
            return item1.value >= item2.value;
        }

        public static bool operator <(HealthIndex item1, HealthIndex item2)
        {
            return item1.value < item2.value;
        }

        public static bool operator <=(HealthIndex item1, HealthIndex item2)
        {
            return item1.value <= item2.value;
        }

        public int CompareTo(object obj)
        {
            return this.CompareTo((HealthIndex)obj);
        }

        public bool Equals(HealthIndex other)
        {
            return (this.value.Equals(other.value) && this.mode.Equals(other.mode));
        }

        public override bool Equals(object obj)
        {
            if (obj is HealthIndex)
            {
                return this.Equals((HealthIndex) obj);
            }

            return false;
        }

        public override string ToString()
        {
            return this.value.ToString();
        }

        public override int GetHashCode()
        {
            return this.value.GetHashCode();
        }

        internal int GetValue()
        {
            return this.value;
        }
    }
}