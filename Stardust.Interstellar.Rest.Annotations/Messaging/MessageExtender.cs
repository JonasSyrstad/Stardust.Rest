using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Stardust.Interstellar.Rest.Annotations.Messaging
{
    public static class GlobalParameterExtensions
    {
        /// <summary>
        /// Adds a global property for the service. this applies to the whole service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T SetGlobalProperty<T>(this T service, string propertyName, object value) where T : IServiceWithGlobalParameters
        {
            SetGlobalProperty<T>(propertyName,value);
            return service;
        }

        public static void SetGlobalProperty<T>(string propertyName, object value) where T : IServiceWithGlobalParameters
        {
            ConcurrentDictionary<string, object> parameterCache;
            if (!globalParameterCache.TryGetValue(typeof(T).FullName, out parameterCache))
            {
                parameterCache = new ConcurrentDictionary<string, object>();
                globalParameterCache.TryAdd(typeof(T).FullName, parameterCache);
            }
            parameterCache.TryAdd(propertyName, JToken.FromObject(value));
        }

        public static object AppendGlobalParameters(string serviceName, object message, int level)
        {
            ConcurrentDictionary<string, object> globalVals;
            if (!globalParameterCache.TryGetValue(serviceName, out globalVals)) return message;
            var jobject = JObject.FromObject(message);
            foreach (var globalVal in globalVals)
            {
                if (level == 0)
                    jobject.Add(globalVal.Key, JToken.FromObject(globalVal.Value));
                else
                {
                    AddInLevel(globalVal, jobject, 0, level);
                }
            }
            return jobject;
        }

        private static void AddInLevel(KeyValuePair<string, object> globalVal, IJEnumerable<JToken> jobject, int i, int level)
        {
            if (i == level)
            {
                var jo = jobject as JObject;
                jo?.Add(globalVal.Key, JToken.FromObject(globalVal.Value));
                //var jt = jobject as JToken;
                //var conteent = new JProperty(globalVal.Key,globalVal.Value);
                //jt.Last.AddAfterSelf(conteent);
                return;
            }
            foreach (var child in jobject)
            {
                AddInLevel(globalVal, child, i + 1, level);
            }
        }

        private static ConcurrentDictionary<string, ConcurrentDictionary<string, object>> globalParameterCache = new ConcurrentDictionary<string, ConcurrentDictionary<string, object>>();
    }
}
