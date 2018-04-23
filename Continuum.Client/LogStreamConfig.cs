using System.Configuration;
using Stardust.Interstellar.Rest.Client;

namespace Stardust.Continuum.Client
{
    public static class LogStreamConfig
    {
        static LogStreamConfig()
        {
            try
            {
                ProxyFactory.CreateProxy<ILogStream>();
            }
            catch (System.Exception)
            {
                // ignored
            }
        }
        private static string _apiKey;

        public static string ApiKey
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(_apiKey)) _apiKey = ConfigurationManager.AppSettings["continuum:apiKey"];
                }
                catch (System.Exception)
                {
                    // ignored
                }
                return _apiKey;
            }
            set { _apiKey = value; }
        }
    }
}