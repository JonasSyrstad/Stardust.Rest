using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Annotations.Resolver
{
    public interface IVerbResolver
    {
        void AppendCustomAttribute(MemberInfo member, MethodBuilder ilGenerator);
    }
}
