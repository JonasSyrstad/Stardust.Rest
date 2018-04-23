using System.Reflection;

namespace Stardust.Interstellar.Rest.Common
{
    public interface IRouteTemplateResolver
    {
        string GetTemplate(MethodInfo methodInfo);
    }
}