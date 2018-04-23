namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IAuthenticationInspector
    {
        IAuthenticationHandler GetHandler();
    }
}