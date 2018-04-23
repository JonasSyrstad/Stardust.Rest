using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Service
{
    internal static class StateHelperEx
    {
        private const string ActionIdName = "sd-ActionId";
        public static string ActionId(this HttpRequest req)
        {
            var headers = req.Headers;
            return ActionId(headers);
        }
        
        public static StateDictionary GetState(this HttpRequest req)
        {
            
            var actionId = req.ActionId();
            return StateHelper.InitializeState(actionId);
        }

        public static void InitializeState(this HttpRequest req)
        {

            var actionId = req.ActionId();
            if (string.IsNullOrWhiteSpace(actionId))
            {

                actionId = Guid.NewGuid().ToString();
                req.Headers.Add(ActionIdName, actionId);

            }
            StateHelper.InitializeState(actionId);
        }

        public static void EndState(this HttpRequest req)
        {
            var actionId = req.ActionId();
            StateHelper.EndState(actionId);
        }

        public static string ActionId(this IHeaderDictionary headers)
        {
            return headers.Where(h => h.Key == ActionIdName).Select(h => h.Value).FirstOrDefault().FirstOrDefault();
        }

        public static string ActionId(this HttpResponse resp)
        {
            var headers = resp.Headers;
            return ActionId(headers);
        }

        public static StateDictionary GetState(this HttpResponse req)
        {
            var actionId = req.ActionId();
            return StateHelper.InitializeState(actionId);
        }
    }
}