using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations.Cache
{
    public class UseEtagAttribute : Attribute, ICahceInspector
    {
        public ICacheHelper GetHandler()
        {
            return new ETagHandler();
        }
    }
}
