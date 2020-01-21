using System;
using System.IO;
using System.Net;

namespace Stardust.Interstellar.Rest.Client
{
    /// <summary>
    /// Provide custom serializer
    /// </summary>
    public interface ISerializer
    {
        string SerializationType { get; }

        void Serialize(WebRequest req, object val);

        object Deserialize(Stream responseStream, Type type);
        byte[] Serialize(object value);
    }
}