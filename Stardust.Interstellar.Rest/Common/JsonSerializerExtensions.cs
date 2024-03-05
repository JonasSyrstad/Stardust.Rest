using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Stardust.Particles;

namespace Stardust.Interstellar.Rest.Common
{
    public static class JsonSerializerExtensions
    {
        public static string SanitizeHttpHeaderValue(this string header)
        {
            if (header.IsNullOrWhiteSpace()) return header;
            return header.Replace("\r", string.Empty)
                .Replace("%0d", string.Empty)
                .Replace("%0D", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("%0a", string.Empty)
                .Replace("%0A", string.Empty);
        }
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
