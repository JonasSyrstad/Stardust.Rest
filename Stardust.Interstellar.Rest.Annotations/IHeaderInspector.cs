using System;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IHeaderInspector
    {
        IHeaderHandler[] GetHandlers(IServiceProvider serviceLocator);
    }
}
