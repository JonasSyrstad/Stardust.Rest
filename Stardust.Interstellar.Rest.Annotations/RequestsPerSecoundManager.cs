using System;
using System.Collections.Concurrent;

namespace Stardust.Interstellar.Rest.Annotations
{
	public class RequestsPerSecoundManager : IThrottlingManager
	{
		private readonly long? _maxRequestsPerSecound;
		private readonly AppliesToTypes _appliesTo;

		public RequestsPerSecoundManager(long? maxRequestsPerSecound, AppliesToTypes appliesTo, long waitTime = 1000)
		{
			_maxRequestsPerSecound = maxRequestsPerSecound;
			_appliesTo = appliesTo;
			_waitTime = waitTime;
		}

		private static ConcurrentDictionary<string, CounterItem> reqPerSecCounter = new ConcurrentDictionary<string, CounterItem>();
		private readonly long _waitTime;

		public long? IsThrottled(string method, string service, string host)
		{
			CounterItem validator;
			switch (_appliesTo)
			{
				case AppliesToTypes.Method:

					if (!reqPerSecCounter.TryGetValue($"method:{method}", out validator))
					{
						validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
						reqPerSecCounter.TryAdd($"method:{method}", validator);
					}
					break;
				case AppliesToTypes.Service:
					if (!reqPerSecCounter.TryGetValue($"service:{service}", out validator))
					{
						validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
						reqPerSecCounter.TryAdd($"service:{service}", validator);
					}
					break;
				case AppliesToTypes.Host:
					if (!reqPerSecCounter.TryGetValue($"host:{host}", out validator))
					{
						validator = new CounterItem(_maxRequestsPerSecound.Value, _waitTime);
						reqPerSecCounter.TryAdd($"host:{host}", validator);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return validator.CountAndValidate();
		}

	}
}