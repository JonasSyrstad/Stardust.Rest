using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Stardust.Interstellar.Rest.Annotations.Service
{
    public interface ICreateImplementationAttribute
    {
        CustomAttributeBuilder CreateAttribute();
    }
}
