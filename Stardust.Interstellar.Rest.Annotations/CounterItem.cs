using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	internal class CounterItem
	{
		private DateTime CounterSecound = NowTruncated;
		private long _counter = 0;
		private readonly long _maxLimit;
		private readonly long _waitTime = 1000;

		public CounterItem(long maxLimit)
		{
			_maxLimit = maxLimit;
		}

		public CounterItem(long maxLimit, long waitTime)
		{
			_maxLimit = maxLimit;
			_waitTime = waitTime;
		}

		private static DateTime Truncate(DateTime dateTime, TimeSpan timeSpan)
		{
			if (timeSpan == TimeSpan.Zero) return dateTime;
			return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
		}

		public long? CountAndValidate()
		{
			if (CounterSecound == NowTruncated)
			{
				_counter++;
				return _counter < _maxLimit ? (long?)null : _waitTime;
			}
			else
			{
				_counter = 0;
				CounterSecound = NowTruncated;
				_counter++;
				return _counter < _maxLimit ? (long?)null : _waitTime;
			}
		}

		private static DateTime NowTruncated => Truncate(DateTime.UtcNow, TimeSpan.FromMilliseconds(1));
	}
}