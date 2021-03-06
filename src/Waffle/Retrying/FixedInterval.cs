﻿namespace Waffle.Retrying
{
    using System;
    using Waffle.Internal;

    /// <summary>
    /// Represents a retry strategy with a specified number of retry attempts and a default, fixed time interval between retries.
    /// </summary>
    public class FixedInterval : RetryStrategy
    {
        private readonly int retryCount;
        private readonly TimeSpan retryInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInterval" /> class. 
        /// </summary>
        public FixedInterval()
            : this(RetryStrategy.DefaultClientRetryCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInterval" /> class with the specified number of retry attempts. 
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        public FixedInterval(int retryCount)
            : this(retryCount, RetryStrategy.DefaultRetryInterval)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInterval" /> class with the specified number of retry attempts and time interval. 
        /// </summary>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="retryInterval">The time interval between retries.</param>
        public FixedInterval(int retryCount, TimeSpan retryInterval)
            : this(null, retryCount, retryInterval, RetryStrategy.DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInterval" /> class with the specified number of retry attempts, time interval, and retry strategy. 
        /// </summary>
        /// <param name="name">The retry strategy name.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="retryInterval">The time interval between retries.</param>
        public FixedInterval(string name, int retryCount, TimeSpan retryInterval)
            : this(name, retryCount, retryInterval, RetryStrategy.DefaultFirstFastRetry)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedInterval" /> class with the specified number of retry attempts, time interval, retry strategy, and fast start option. 
        /// </summary>
        /// <param name="name">The retry strategy name.</param>
        /// <param name="retryCount">The number of retry attempts.</param>
        /// <param name="retryInterval">The time interval between retries.</param>
        /// <param name="firstFastRetry"><see langword="true"/> to immediately retry in the first attempt; otherwise, <see langword="false"/>. The subsequent retries will remain subject to the configured retry interval.</param>
        public FixedInterval(string name, int retryCount, TimeSpan retryInterval, bool firstFastRetry)
            : base(name, firstFastRetry)
        {
            if (retryCount < 0)
            {
                throw Error.ArgumentMustBeGreaterThanOrEqualTo("retryCount", retryCount, 0);
            }

            if (retryInterval.Ticks < 0)
            {
                throw Error.ArgumentMustBeGreaterThanOrEqualTo("retryInterval", retryInterval.Ticks, 0);
            }

            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
        }

        /// <summary>
        /// Returns the corresponding ShouldRetry delegate.
        /// </summary>
        /// <returns>The ShouldRetry delegate.</returns>
        public override ShouldRetry GetShouldRetry()
        {
            if (this.retryCount == 0)
            {
                return (int currentRetryCount, Exception lastException, out TimeSpan interval) =>
                {
                    interval = TimeSpan.Zero;
                    return false;
                };
            }

            return (int currentRetryCount, Exception lastException, out TimeSpan interval) =>
            {
                if (currentRetryCount < this.retryCount)
                {
                    interval = this.retryInterval;
                    return true;
                }

                interval = TimeSpan.Zero;
                return false;
            };
        }
    }
}