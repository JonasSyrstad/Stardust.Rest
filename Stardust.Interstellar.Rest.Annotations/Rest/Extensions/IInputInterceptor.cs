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

    public class InterseptorStatus
    {
        public bool Cancel { get; set; }

        public string CancellationMessage { get; set; }

        public HttpStatusCode StatusCode { get; set; }
    }

    public abstract class InputInterceptorBase : IInputInterceptor
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        protected InputInterceptorBase()
        {
            StatusCode = HttpStatusCode.OK;
            CancelMessage = "";
        }

        public abstract object Intercept(object result, StateDictionary getState);

        public virtual bool Intercept(object[] values, StateDictionary stateDictionary, out string cancellationMessage, out HttpStatusCode statusCode)
        {
            var result = Intercept(values);
            cancellationMessage = CancelMessage;
            statusCode = StatusCode;
            return result;
        }

        public abstract Task<object> InterceptAsync(object result, StateDictionary getState);

        public abstract Task<InterseptorStatus> InterceptAsync(object[] values, StateDictionary stateDictionary);

        protected HttpStatusCode StatusCode { get; set; }

        protected string CancelMessage { get; set; }

        protected abstract bool Intercept(object[] values);
    }
}