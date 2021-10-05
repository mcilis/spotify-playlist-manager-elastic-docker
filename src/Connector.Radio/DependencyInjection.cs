using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Connector.Radio
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRadioManagement(this IServiceCollection services, IConfiguration configuration)
        {
            var radioConfiguration = configuration.GetSection("Connector.Radio");

            services.AddHttpClient("Eksen", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["Eksen:BaseAddress"]);
            });

            services.AddHttpClient("JoyFm", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["JoyFm:BaseAddress"]);
            });

            services.AddHttpClient("JoyTurkRock", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["JoyTurkRock:BaseAddress"]);
            });

            services.AddHttpClient("Veronica", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["Veronica:BaseAddress"]);
            });

            services.AddHttpClient("VeronicaRock", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["VeronicaRock:BaseAddress"]);
            });

            services.AddHttpClient("SlowTime", x =>
            {
                x.BaseAddress = new Uri(radioConfiguration["SlowTime:BaseAddress"]);
            });

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

            var radioFactory = new RadioFactory();
            radioFactory.Register(new Eksen(httpClientFactory, configuration));
            radioFactory.Register(new JoyFm(httpClientFactory, configuration));
            radioFactory.Register(new JoyTurkRock(httpClientFactory, configuration));
            radioFactory.Register(new Veronica(httpClientFactory, configuration));
            radioFactory.Register(new VeronicaRock(httpClientFactory, configuration));
            radioFactory.Register(new SlowTime(httpClientFactory, configuration));

            services.AddSingleton<IRadioFactory>(radioFactory);

            return services;
        }
    }
}
