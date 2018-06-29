using System;
using System.Text;
using System.Threading.Tasks;
using Swashbuckle.Application;

namespace Swashbuckle.Stardust.Interstellar
{
    public static class SwaggerExtensions
    {
        public static SwaggerDocsConfig ConfigureStardust(this SwaggerDocsConfig swaggerConfig, Action<SwaggerDocsConfig> additionalConfigurations, IServiceProvider serviceProvider)
        {
            swaggerConfig.OperationFilter(() => new StardustOperationDescriptor(serviceProvider));
            return swaggerConfig;
        }

        public static SwaggerDocsConfig ConfigureStardust(this SwaggerDocsConfig swaggerConfig, IServiceProvider serviceProvider)
        {
            swaggerConfig.OperationFilter(() => new StardustOperationDescriptor(serviceProvider));
            return swaggerConfig;
        }
    }
}
