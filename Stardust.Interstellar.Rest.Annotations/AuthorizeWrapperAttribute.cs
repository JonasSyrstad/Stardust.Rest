using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Annotations
{
    /// <summary>
    /// Authorize attribute wrapper. Allows the framework to inject the correct authorice attribute on the service implementation.
    /// Override this and return the webapi authorize attribute (or your custom auth code)
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeWrapperAttribute : Attribute
    {
        public static void SetAttributeCreator(Func<AuthorizeWrapperAttribute, Attribute> creator)
        {
            _creator = creator;
        }
        private object[] ctorParams;
        private static Func<AuthorizeWrapperAttribute, Attribute> _creator;

        public virtual Attribute GetAutorizationAttribute()
        {
            return _creator?.Invoke(this);
        }


        public object[] GetConstructorParameters()
        {
            return ctorParams;
        }

        public AuthorizeWrapperAttribute(params object[] constructorParams)
        {
            ctorParams = constructorParams ?? new object[0];
        }

        /// <summary>
        /// For use with .net framework webapi
        /// </summary>
        public string Users { get; set; }

        public string Roles { get; set; }

        /// <summary>
        /// For use in asp.net core
        /// </summary>
        public string Policy { get; set; }
    }
}
