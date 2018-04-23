using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Annotations.Cache
{
    public interface ICacheHelper
    {
        string GetEtag(string itemId);

        void SetImplementation(ICacheHelper instance);
    }
}
