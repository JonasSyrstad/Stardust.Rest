using System;
using System.Collections.Generic;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IServiceLocator
    {
        T GetService<T>();

        object GetService(Type serviceType);

        IEnumerable<T> GetServices<T>();

        object CreateInstanceOf(Type type);

        T CreateInstance<T>() where T : class;
    }
}