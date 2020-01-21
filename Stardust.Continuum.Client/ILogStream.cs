using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Annotations.UserAgent;

namespace Stardust.Continuum.Client
{
    [Api("api/v1")]
    [ApiKey]
    [CircuitBreaker(10, 3, 10)]
    [PerformanceHeaders]
    [CallingMachineName]
    [FixedClientUserAgent("continuum (v2.0.beta;.netstandard2.0++)")]
    public interface ILogStream
    {
        [Put("single/{project}/{environment}")]
        Task AddStream([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem item);

        [Put("batch/{project}/{environment}")]
        Task AddStreamBatch([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment, [In(InclutionTypes.Body)]StreamItem[] items);


        [Options("single/{project}/{environment}")]
        Task Options([In(InclutionTypes.Path)]string project, [In(InclutionTypes.Path)]string environment);
    }
}