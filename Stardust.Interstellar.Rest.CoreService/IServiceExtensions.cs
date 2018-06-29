using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Annotations;

namespace Stardust.Interstellar.Rest.Service
{
    /// <summary>
    /// Append this interface to the service implementation, to gain access to the controller context.
    /// </summary>
    public interface IServiceExtensions
    {
        void SetControllerContext(ControllerContext currentContext);

        void SetResponseHeaderCollection(Dictionary<string, string> headers);

        Dictionary<string, string> GetHeaders();
    }

    public static class MessageExtensions
    {
        public static JObject GetExtendedMessage(this IServiceWithGlobalParameters service)
        {
            var state = GetCache(service.ServiceLocator)?.GetState();
            return state;
        }

        public static IStateCache GetCache(IServiceProvider serviceLocator)
        {
            var cache = serviceLocator?.GetService<IStateCache>();
            if (cache == null) return null;
            return cache;
        }
    }

    public interface IStateCache
    {
        JObject GetState();

        void SetState(JObject extendedMessage);
    }
}
