using System.Net;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IInputInterceptor
    {

        object Intercept(object result, StateDictionary getState);
        bool Intercept(object[] values, StateDictionary stateDictionary, out string cancellationMessage, out HttpStatusCode statusCode);
        Task<object> InterceptAsync(object result, StateDictionary getState);
        Task<InterseptorStatus> InterceptAsync(object[] values, StateDictionary stateDictionary);
    }
}