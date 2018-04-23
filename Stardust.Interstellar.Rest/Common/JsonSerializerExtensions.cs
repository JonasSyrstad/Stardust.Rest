using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Stardust.Interstellar.Rest.Common
{
    public static class JsonSerializerExtensions
    {

        private static Dictionary<Type, JsonSerializerSettings> clientSerializerSettings = new Dictionary<Type, JsonSerializerSettings>();
        public static JsonSerializerSettings AddClientSerializer<T>(this JsonSerializerSettings settings) where T : class
        {
            if (clientSerializerSettings.ContainsKey(typeof(T))) throw new AmbiguousMatchException($"Serailization settings already configured for {nameof(T)}");
            clientSerializerSettings.Add(typeof(T), settings);
            return settings;
        }

        public static JsonSerializerSettings GetClientSerializationSettings<T>(this T service) where T : class
        {
            var serviceType = typeof(T);    
            return serviceType.GetClientSerializationSettings();
        }

        public static JsonSerializerSettings GetClientSerializationSettings(this Type serviceType)
        {
            JsonSerializerSettings settings;
            return clientSerializerSettings.TryGetValue(serviceType, out settings) ? settings : JsonConvert.DefaultSettings!=null? JsonConvert.DefaultSettings():null;
        }

        public static void UseSerializerSettingsFor(Type messageOrServiceType, JsonSerializerSettings settings)
        {
            if (clientSerializerSettings.ContainsKey(messageOrServiceType)) throw new AmbiguousMatchException($"Serailization settings already configured for {messageOrServiceType.Name}");
            clientSerializerSettings.Add(messageOrServiceType, settings);
        }
    }
}
