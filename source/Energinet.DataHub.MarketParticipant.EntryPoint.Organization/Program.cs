using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common;
using Energinet.DataHub.MarketParticipant.EntryPoint.Organization.Common.SimpleInjector;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization
{
    public static class Program
    {
        public static async Task Main()
        {
            var startup = new Startup();

            await using (startup.ConfigureAwait(false))
            {
                var host = new HostBuilder()
                    .ConfigureFunctionsWorkerDefaults(options => options.UseMiddleware<SimpleInjectorScopedRequest>())
                    .ConfigureServices(startup.ConfigureServices)
                    .Build()
                    .UseSimpleInjector(startup.Container);

                await host.RunAsync().ConfigureAwait(false);
            }
        }
    }
}
