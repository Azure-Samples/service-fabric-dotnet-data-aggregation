// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using System;
    using System.Security.Cryptography;

    ///<summary>
    /// Represents a pseudo-random number generator, a device that produces random data.
    ///</summary>
    public class CryptoRandom : RandomNumberGenerator
    {
        private static RandomNumberGenerator r;

        ///<summary>
        /// Creates an instance of the default implementation of a cryptographic random number generator that can be used to generate random data.
        ///</summary>
        public CryptoRandom()
        {
            r = Create();
        }

        ///<summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        ///</summary>
        ///<param name=”buffer”>An array of bytes to contain random numbers.</param>
        public override void GetBytes(byte[] buffer)
        {
            r.GetBytes(buffer);
        }

        ///<summary>
        /// Returns a random number between 0.0 and 1.0.
        ///</summary>
        public double NextDouble()
        {
            byte[] b = new byte[4];
            r.GetBytes(b);
            return (double) BitConverter.ToUInt32(b, 0) / UInt32.MaxValue;
        }


        ///<summary>
        /// Returns a random number within the specified range.
        ///</summary>
        ///<param name=”minValue”>The inclusive lower bound of the random number returned.</param>
        ///<param name=”maxValue”>The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        public int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                throw new ArgumentOutOfRangeException(String.Format("max:{0} must be > min:{1}!", maxValue, minValue));
            }

            return (int) Math.Round(this.NextDouble() * (maxValue - minValue - 1)) + minValue;
        }

        ///<summary>
        /// Returns a nonnegative random number.
        ///</summary>
        public int Next()
        {
            return this.Next(0, Int32.MaxValue);
        }

        ///<summary>
        /// Returns a nonnegative random number less than the specified maximum
        ///</summary>
        ///<param name=”maxValue”>The inclusive upper bound of the random number returned. maxValue must be greater than or equal 0</param>
        public int Next(int maxValue)
        {
            return this.Next(0, maxValue);
        }

        public long NextLong(long minValue, long maxValue)
        {
            //from http://stackoverflow.com/a/13095144 modified for this class
            if (maxValue <= minValue)
            {
                throw new ArgumentOutOfRangeException(String.Format("max:{0} must be > min:{1}!", maxValue, minValue));
            }

            //Working with ulong so that modulo works correctly with values > long.MaxValue
            ulong uRange = (ulong) (maxValue - minValue);

            //Prevent a modolo bias; see http://stackoverflow.com/a/10984975/238419
            //for more information.
            //In the worst case, the expected number of calls is 2 (though usually it's
            //much closer to 1) so this loop doesn't really hurt performance at all.
            ulong ulongRand;
            do
            {
                byte[] buf = new byte[8];
                r.GetBytes(buf);
                ulongRand = (ulong) BitConverter.ToInt64(buf, 0);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long) (ulongRand % uRange) + minValue;
        }

        /// <summary>
        /// Returns a random long from 0 (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater than 0</param>
        public long NextLong(long max)
        {
            return this.NextLong(0, max);
        }

        /// <summary>
        /// Returns a random long over all possible values of long (except long.MaxValue, similar to
        /// random.Next())
        /// </summary>
        /// <param name="random">The given random instance</param>
        public long NextLong()
        {
            return this.NextLong(long.MinValue, long.MaxValue);
        }
    }
}