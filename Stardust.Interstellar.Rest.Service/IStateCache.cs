using Newtonsoft.Json.Linq;

namespace Stardust.Interstellar.Rest.Service
{
    public interface IStateCache
    {
        JObject GetState();

        void SetState(JObject extendedMessage);
    }
}