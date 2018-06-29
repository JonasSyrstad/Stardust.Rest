using System;
using System.Configuration;
using Stardust.Interstellar.Rest.Extensions;
using Stardust.Particles;

namespace Stardust.Interstellar.Rest.Annotations.UserAgent
{
    public class ConfiguredClienUserAgentName : Attribute, IHeaderInspector
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ConfiguredClienUserAgentName(string preFix) : this()
        {
            this.PreFix = preFix;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ConfiguredClienUserAgentName(string preFix, string postFix) : this()
        {
            this.PreFix = preFix;
            this.PostFix = postFix;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Attribute" /> class.</summary>
        public ConfiguredClienUserAgentName()
        {
            if (string.IsNullOrWhiteSpace(configuredValue)) configuredValue = ConfigurationManagerHelper.GetValueOnKey("stardust.clientUserAgentName");
        }

        private string preFix;

        internal static string configuredValue;

        private string postFix;

        public string PreFix
        {
            get
            {
                return preFix;
            }
            set
            {
                preFix = value;
            }
        }

        public string PostFix
        {
            get
            {
                return postFix;
            }
            set
            {
                postFix = value;
            }
        }

        IHeaderHandler[] IHeaderInspector.GetHandlers(IServiceProvider serviceLocator)
        {
            return new IHeaderHandler[] { new FixedClientUserAgentAttribute(string.Format("{0}{1}{2}", PreFix, configuredValue, PostFix)) };
        }


    }
}