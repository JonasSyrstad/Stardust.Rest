using System.Threading.Tasks;
using System.Web.Http;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.UserAgent;

namespace Stardust.Continuum.Client
{
    [IRoutePrefix("api/v1")]
    [ApiKey]
    [CircuitBreaker(10, 3, 10)]
    [PerformanceHeaders]
    [CallingMachineName]
    [FixedClientUserAgent("continuum (v1.1.beta;.net4.5++)")]
    [IAuthorize]
    public interface ILogStream
    {
        [HttpPut]
        [Route("single/{project}/{environment}")]
        Task AddStream([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem item);

        [Route("batch/{project}/{environment}")]
        [HttpPut]
        Task AddStreamBatch([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem[] items);

        [HttpOptions]
        [Route("single/{project}/{environment}")]
        Task Options([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment);
    }
}