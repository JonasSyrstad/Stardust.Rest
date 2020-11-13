using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using Stardust.Interstellar.Rest.Annotations.Service;

namespace Stardust.Interstellar.Rest.Annotations
{
    public class SuccessStatusCodeAttribute : Attribute
    {
        private ConstructorInfo _ctor;
        private ConstructorInfo _ctor2;
        public HttpStatusCode StatusCode { get; }

        public SuccessStatusCodeAttribute(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            var t = Type.GetType("Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute, Microsoft.AspNetCore.Mvc.Core") ?? typeof(ResponseTypeAttribute);
            _ctor = t.GetConstructor(new Type[]
                {typeof(Type), typeof(int)});
            _ctor2 = t.GetConstructor(new Type[]
                { typeof(int)});
        }

        public CustomAttributeBuilder CreateAttribute(Type responseType)
        {
            if (responseType == null) return new CustomAttributeBuilder(_ctor2, new object[] { (int)StatusCode });
            return new CustomAttributeBuilder(_ctor, new object[] { responseType, (int)StatusCode });
        }

        public CustomAttributeBuilder CreateAttribute()
        {
            return CreateAttribute(null);
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public class ResponseTypeAttribute : Attribute, ICreateImplementationAttribute
    {
        private readonly int _statusCode;
        private readonly Type _type;
        private static ConstructorInfo _ctor;
        private static ConstructorInfo _ctor2;

        static ResponseTypeAttribute()
        {
            var t = Type.GetType("Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute, Microsoft.AspNetCore.Mvc.Core") ?? typeof(ResponseTypeAttribute);
            _ctor = t.GetConstructor(new Type[]
                {typeof(Type), typeof(int)});
            _ctor2 = t.GetConstructor(new Type[]
                { typeof(int)});
        }
        public ResponseTypeAttribute(HttpStatusCode statusCode, Type type)
        {
            _statusCode = (int)statusCode;
            _type = type;
        }

        public ResponseTypeAttribute(HttpStatusCode statusCode)
        {
            _statusCode = (int)statusCode;
        }

        public ResponseTypeAttribute(int statusCode, Type type)
        {
            _statusCode = statusCode;
            _type = type;
        }

        public ResponseTypeAttribute(int statusCode)
        {
            _statusCode = statusCode;
        }
        public CustomAttributeBuilder CreateAttribute()
        {

            if (_type == null) return new CustomAttributeBuilder(_ctor2, new object[] { (int)_statusCode });
            return new CustomAttributeBuilder(_ctor, new object[] { _type, (int)_statusCode });
        }
    }
}