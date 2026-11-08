using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;
using Microsoft.Extensions.DependencyInjection;

namespace NII.Infrastructure
{
    public class MicrosoftDependencyResolver : IDependencyResolver
    {
        protected IServiceProvider ServiceProvider;
        public MicrosoftDependencyResolver(IServiceProvider serviceProvider) { ServiceProvider = serviceProvider; }
        public object GetService(Type serviceType) => ServiceProvider.GetService(serviceType);
        public IEnumerable<object> GetServices(Type serviceType) => ServiceProvider.GetServices(serviceType);
        public IDependencyScope BeginScope() => new MicrosoftDependencyResolver(ServiceProvider.CreateScope().ServiceProvider);
        public void Dispose() { }
    }
}
