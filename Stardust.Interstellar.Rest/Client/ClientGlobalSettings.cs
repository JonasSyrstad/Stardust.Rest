using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Stardust.Interstellar.Rest.Annotations.Service;
using Stardust.Interstellar.Rest.Common;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Interstellar.Rest.Service;

namespace Stardust.Interstellar.Rest.Client
{
    public static class ClientGlobalSettings
    {
        /// <summary>
        /// Gets or sets the time-out value in milliseconds for the System.Net.HttpWebRequest.GetResponse
        /// and System.Net.HttpWebRequest.GetRequestStream methods.
        /// </summary>
        public static int? Timeout { get; set; }

        /// <summary>
        /// Gets or sets a time-out in milliseconds when writing to or reading from a stream.
        /// </summary>
        public static int? ReadWriteTimeout { get; set; }

        /// <summary>
        /// Gets or sets a timeout, in milliseconds, to wait until the 100-Continue is received
        /// from the server.
        /// </summary>
        public static int? ContinueTimeout { get; set; }

        public static bool? KeepAlive { get; set; }

        public static void SetExtendedTimeouts()
        {
            Timeout = 300000;
            ReadWriteTimeout = 3000000;
            ContinueTimeout = 300000;
        }

        public static ServiceConfigurationContext<T> ServiceSettings<T>(IServiceLocator locator)
        {
            ProxyFactory.CreateInstance<T>("", locator);
            return new ServiceConfigurationContext<T>(RestWrapper.Cache()[typeof(T)]);
        }

        //public static ServiceConfigurationContext<T> ServiceSettings<T>(IServiceLocator locator)
        //{
        //    return ServiceSettings<T>(locator);
        //}
    }

    public class ServiceConfigurationContext<T>
    {
        private readonly ConcurrentDictionary<string, ActionWrapper> _concurrentDictionary;

        internal ServiceConfigurationContext(ConcurrentDictionary<string, ActionWrapper> concurrentDictionary)
        {
            _concurrentDictionary = concurrentDictionary;
        }



        public ActionConfigurationContext<T> ConfigureAction(Expression<Action<T>> expression)
        {
            var name = StaticReflection.GetMemberName(expression);
            return new ActionConfigurationContext<T>(_concurrentDictionary, _concurrentDictionary[name]);
        }


        public ActionConfigurationContext<T> ConfigureAction(Expression<Func<T, object>> expression)
        {
            var name = StaticReflection.GetMemberName(expression);
            return new ActionConfigurationContext<T>(_concurrentDictionary, _concurrentDictionary[name]);
        }

        public ServiceConfigurationContext<T> SetDefaultErrorHandler(IErrorHandler errorHandler)
        {
            foreach (var actionWrapper in _concurrentDictionary)
            {
                actionWrapper.Value.DefaultErrorHandler = errorHandler;
            }
            return this;
        }

    }

    public class ActionConfigurationContext<T> : ServiceConfigurationContext<T>
    {
        private readonly ActionWrapper _action;

        internal ActionConfigurationContext(ConcurrentDictionary<string, ActionWrapper> concurrentDictionary, ActionWrapper action) : base(concurrentDictionary)
        {
            _action = action;
        }



        public ActionConfigurationContext<T> AddHeaderHandler(IHeaderInspector handler)
        {
            _action.CustomHandlers.Add(handler);
            return this;
        }

        public ActionConfigurationContext<T> RemoveHandlers(Func<IHeaderInspector, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate", "Predicate cannot be NULL");
            var itemsToRemove = _action.CustomHandlers.Where(predicate).ToArray();
            foreach (var headerHandler in itemsToRemove)
            {
                _action.CustomHandlers.Remove(headerHandler);
            }
            return this;
        }

        public int NumberOfRetries { get { return _action.NumberOfRetries; } set { _action.NumberOfRetries = value; } }

        public bool IncrementalRetry { get { return _action.IncrementalRetry; } set { _action.IncrementalRetry = value; } }

        public int Interval
        {
            get { return _action.Interval; }
            set { _action.Interval = value; }
        }

        public ActionConfigurationContext<T> AddServiceInitializer(ServiceInitializerAttribute handler)
        {
            _action.Initializers.Add(handler);
            return this;
        }

        public ActionConfigurationContext<T> RemoveServiceInitializers(Func<ServiceInitializerAttribute, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate", "Predicate cannot be NULL");
            var itemsToRemove = _action.Initializers.Where(predicate).ToArray();
            foreach (var headerHandler in itemsToRemove)
            {
                _action.Initializers.Remove(headerHandler);
            }
            return this;
        }
    }
}