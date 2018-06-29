using System;
using System.Collections.Generic;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IServiceLocator : IServiceProvider
    {
        T GetService<T>();

        IEnumerable<T> GetServices<T>();

        object CreateInstanceOf(Type type);

        T CreateInstance<T>() where T : class;
    }
}