using Stardust.Interstellar.Rest.Extensions;

namespace Stardust.Interstellar.Rest.Annotations.Messaging
{
    /// <summary>
    /// Marker interface for Rest services that support global properties in outgoing messages.
    /// </summary>
    public interface IServiceWithGlobalParameters
    {
        IServiceLocator ServiceLocator { get; }
    }
}