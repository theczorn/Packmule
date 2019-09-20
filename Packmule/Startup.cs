using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Packmule.Configuration;
using Packmule.Repositories.MuleRepository;
using System;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(Packmule.Startup))]

namespace Packmule
{
    class Startup : FunctionsStartup
    {
        private PackmuleConfiguration _packmuleConfiguration;
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<PackmuleConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.Bind(settings);
                });

            AddMuleRepository(builder);
            AddVisionClient(builder);
        }

        private void AddMuleRepository(IFunctionsHostBuilder builder)
        {
            _packmuleConfiguration = builder.Services.BuildServiceProvider().GetService<IOptions<PackmuleConfiguration>>().Value;
            builder.Services.AddDbContext<MuleContext>(
                options => options.UseSqlServer(_packmuleConfiguration.MuleRepositoryConnectionString));

            builder.Services.TryAddScoped<IMuleRepository, MuleRepository>();
        }

        private void AddVisionClient(IFunctionsHostBuilder builder)
        {
            builder.Services.TryAddTransient<IComputerVisionClient>(_ => new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(_packmuleConfiguration.CognitiveServiceKey),
                new DelegatingHandler[] { })
            { Endpoint = _packmuleConfiguration.CognitiveServicesUri }
            );
        }
    }
}