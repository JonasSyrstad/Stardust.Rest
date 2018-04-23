using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

namespace Stardust.Interstellar.Rest.Common
{
    public interface IWebMethodConverter
    {
        List<HttpMethod> GetHttpMethods(MethodInfo method);
    }
}