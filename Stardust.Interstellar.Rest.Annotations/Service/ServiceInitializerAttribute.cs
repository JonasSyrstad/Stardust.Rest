using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations.Service
{
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Method, AllowMultiple = true)]
    public abstract class ServiceInitializerAttribute:Attribute
    {
        public abstract void Initialize(IInitializableService service, StateDictionary state, object[] parameters);
    }

    public interface IInitializableService
    {
        void Initialize(params object[] instances);
    }
}
