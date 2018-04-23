using System;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Enable automatic retries for an action or service
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class RetryAttribute : Attribute
    {
        private Type _errorCategorizer;
        public bool IncremetalWait { get; }
        public int NumberOfRetries { get; }
        /// <summary>
        /// the wait time in ms
        /// </summary>
        public int RetryInterval { get; }

        public RetryAttribute() : this(3, 10, true)
        { }

        public RetryAttribute(int numberOfRetries) : this(numberOfRetries, 1000, true)
        { }

        public RetryAttribute(int numberOfRetries, bool incremetalWait) : this(numberOfRetries, 10, incremetalWait)
        { }

        public RetryAttribute(int numberOfRetries, int retryInterval, bool incremetalWait)
        {
            if (numberOfRetries < 1) numberOfRetries = 1;
            if (retryInterval < 1) retryInterval = 10;
            IncremetalWait = incremetalWait;
            NumberOfRetries = numberOfRetries;
            RetryInterval = retryInterval;
        }

        public Type ErrorCategorizer
        {
            get => _errorCategorizer;
            set
            {
                if (!typeof(IErrorCategorizer).IsAssignableFrom(value))
                    throw new InvalidCastException($"Unable to convert {value.FullName} to {nameof(IErrorCategorizer)}");
                _errorCategorizer = value;
            }
        }
    }
}
