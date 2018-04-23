using System;

namespace Stardust.Interstellar.Rest.Client
{
    public class TypeWrapper
    {
        public Type Type { get; set; }

        public static TypeWrapper Create<T>()
        {
            return new TypeWrapper
                       {
                           Type = typeof(T)
                       };
        }

        public static TypeWrapper Create(Type interfaceType)
        {
            return new TypeWrapper
            {
                Type = interfaceType
            };
        }
    }
}