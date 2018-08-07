using System;
using Newtonsoft.Json.Linq;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.Messaging;

namespace Stardust.Interstellar.Rest.Service
{
    public static class MessageExtensions
    {
        public static JObject GetExtendedMessage(this IServiceWithGlobalParameters service)
        {
            var state = GetCache(service.ServiceLocator).GetState();
            return state;
        }

        public static IStateCache GetCache(IServiceProvider serviceLocator)
        {
            var cache = serviceLocator.GetService<IStateCache>();
            if (cache == null) return new HttpContextMessageContainer();
            return cache;
        }
    }
}