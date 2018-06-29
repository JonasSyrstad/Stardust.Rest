using System;
using System.Collections.Generic;
using System.Text;

namespace Stardust.Interstellar.Rest.Annotations
{
    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
        {
            var instance = provider.GetService(typeof(T));
            if (instance == null) return default(T);
            return (T)instance;
        }
    }
}
