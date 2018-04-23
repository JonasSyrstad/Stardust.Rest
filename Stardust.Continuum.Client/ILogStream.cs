using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.UserAgent;

namespace Stardust.Continuum.Client
{
    [IRoutePrefix("api/v1")]
    [ApiKey]
    [CircuitBreaker(10, 3, 10)]
    [PerformanceHeaders]
    [CallingMachineName]
    [FixedClientUserAgent("continuum (v2.0.beta;.netstandard2.0++)")]
   public interface ILogStream
    {
        [Put]
        [IRoute("single/{project}/{environment}")]
        Task AddStream([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem item);

        [IRoute("batch/{project}/{environment}")]
        [Put]
        Task AddStreamBatch([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem[] items);

        [Options]
        [IRoute("single/{project}/{environment}")]
        Task Options([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment);
    }
}