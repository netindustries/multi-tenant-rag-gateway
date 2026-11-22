using System.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using NII.Infrastructure;
using NII.Security.Configuration;
using NII.Security.Contracts;
using NII.Security.DataAccess;
using NII.Security.Services;

namespace NII.App_Start
{
    public static class DependencyInjectionConfig
    {
        public static void RegisterDependencies()
        {
            var services = new ServiceCollection();
            services.AddSingleton(AppOptionsFactory.CreateSecuritySettings());

            services.AddScoped<IInfrastructureSecretRepository, SqlInfrastructureSecretRepository>();
            services.AddScoped<IClientIdentityRepository, SqlClientIdentityRepository>();
            services.AddSingleton<ISecureHttpClientFactory, SecureHttpClientFactory>();

            services.AddTransient<Controllers.SecurityAdminController>();
            services.AddTransient<Controllers.ClientOutboundController>();

            var provider = services.BuildServiceProvider();
            GlobalConfiguration.Configuration.DependencyResolver = new MicrosoftDependencyResolver(provider);
        }
    }
}