using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http.Dispatcher;

namespace Stardust.Interstellar.Rest.Service
{
    public class CustomAssebliesResolver : IHttpControllerTypeResolver
    {

        internal static void SetParent(IHttpControllerTypeResolver parent)
        {
            ParentResolver = parent;
        }
        private static IHttpControllerTypeResolver ParentResolver;

        /// <summary> Returns a list of assemblies available for the application. </summary>
        /// <returns>A &lt;see cref="T:System.Collections.ObjectModel.Collection`1" /&gt; of assemblies.</returns>
        public ICollection<Assembly> GetAssemblies()
        {
            var baseAssemblies = new List<Assembly>(); //AppDomain.CurrentDomain.GetAssemblies().ToList();
            baseAssemblies.Add(ServiceFactory.GetAssembly());
            return baseAssemblies;

        }

        /// <summary> Returns a list of controllers available for the application. </summary>
        /// <returns>An &lt;see cref="T:System.Collections.Generic.ICollection`1" /&gt; of controllers.</returns>
        /// <param name="assembliesResolver">The resolver for failed assemblies.</param>
        public ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            var controllers = ParentResolver.GetControllerTypes(assembliesResolver).ToList();
            controllers.AddRange(ServiceFactory.GetTypes());

            return controllers;
        }

    }
}