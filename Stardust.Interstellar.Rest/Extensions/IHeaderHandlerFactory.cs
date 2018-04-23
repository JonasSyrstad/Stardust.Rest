using System.Collections.Generic;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IHeaderHandlerFactory
    {
        IEnumerable<IHeaderHandler> GetHandlers(IServiceLocator serviceLocator);
    }
}