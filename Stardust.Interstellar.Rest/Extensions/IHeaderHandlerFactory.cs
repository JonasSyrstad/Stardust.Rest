using System;
using System.Collections.Generic;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IHeaderHandlerFactory
    {
        IEnumerable<IHeaderHandler> GetHandlers(IServiceProvider serviceLocator);
    }
}