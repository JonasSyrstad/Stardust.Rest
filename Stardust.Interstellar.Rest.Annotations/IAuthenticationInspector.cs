using System;

namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IAuthenticationInspector
    {
        IAuthenticationHandler GetHandler(IServiceProvider provider);
    }
}