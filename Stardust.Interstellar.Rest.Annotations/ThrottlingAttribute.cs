using System;

namespace Stardust.Interstellar.Rest.Annotations
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Assembly)]
	public class ThrottlingAttribute : Attribute
	{
		private readonly Type _throttlingManager;
		private readonly long? _maxRequestsPerSecound;

		public ThrottlingAttribute(long maxRequestsPerSecound)
		{
			_maxRequestsPerSecound = maxRequestsPerSecound;
		}

		public ThrottlingAttribute(Type throttlingManager)
		{
			if (!typeof(IThrottlingManager).IsAssignableFrom(throttlingManager)) throw new InvalidCastException($"Unable to cast {throttlingManager.FullName} to {nameof(IThrottlingManager)}");
			_throttlingManager = throttlingManager;
		}

		public IThrottlingManager GetManager(AppliesToTypes appliesTo)
		{
			if (_maxRequestsPerSecound != null)
				return new RequestsPerSecoundManager(_maxRequestsPerSecound, appliesTo);
			return (IThrottlingManager)Activator.CreateInstance(_throttlingManager);
		}
	}
}