using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stardust.Interstellar.Rest.Common;

namespace Stardust.Interstellar.Rest.Extensions
{
    public class HeaderHandlerFactory : IHeaderHandlerFactory
    {
        private readonly Type type;

        public HeaderHandlerFactory(Type type)
        {
            this.type = type;
        }

        public IEnumerable<IHeaderHandler> GetHandlers(IServiceProvider serviceLocator)
        {
            var inspectors = type.GetCustomAttributes().OfType<IHeaderInspector>();
            var handlers = new List<IHeaderHandler>();
            foreach (var inspector in inspectors)
            {
                handlers.AddRange(inspector.GetHandlers(serviceLocator));
            }
            return handlers;
        }
    }
}