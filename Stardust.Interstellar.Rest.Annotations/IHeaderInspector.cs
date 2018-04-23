namespace Stardust.Interstellar.Rest.Extensions
{
    public interface IHeaderInspector
    {

        IHeaderHandler[] GetHandlers(IServiceLocator serviceLocator);
    }
}
