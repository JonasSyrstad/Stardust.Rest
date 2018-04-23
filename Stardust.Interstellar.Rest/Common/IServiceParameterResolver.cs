using System.Collections.Generic;
using System.Reflection;

namespace Stardust.Interstellar.Rest.Common
{
    public interface IServiceParameterResolver
    {
        IEnumerable<ParameterWrapper> ResolveParameters(MethodInfo methodInfo);
    }
}