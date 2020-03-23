using Google.Solutions.IapDesktop.Application.ObjectModel;
using NUnit.Framework;
using System;

namespace Google.Solutions.IapDesktop.Application.Test
{
    [TestFixture]
    public class TestServiceRegistry
    {
        public class ServiceWithDefaultConstructor
        {
        }

        public class ServiceWithServiceProviderConstructor
        {
            public IServiceProvider provider;

            public ServiceWithServiceProviderConstructor()
            {
                this.provider = null;
            }

            public ServiceWithServiceProviderConstructor(IServiceProvider provider)
            {
                this.provider = provider;
            }
        }
        public class ServiceWithIncompatibleConstructor
        {
            public ServiceWithIncompatibleConstructor(string s)
            {
            }
        }

        [Test]
        public void WhenRequestingUnknownService_UnknownServiceExceptionIsThrown()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<string>();
            });
        }

        [Test]
        public void WhenRequestingSingleton_ExistingInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            var singleton = new ServiceWithDefaultConstructor();
            registry.AddSingleton<ServiceWithDefaultConstructor>(singleton);

            Assert.AreSame(singleton, registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenRequestingTransientWithDefaultConstructor_NewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithDefaultConstructor>();

            Assert.IsNotNull(registry.GetService<ServiceWithDefaultConstructor>());
        }

        [Test]
        public void WhenRequestingTransientWithSericeProviderConstructor_NewInstanceIsReturned()
        {
            var registry = new ServiceRegistry();
            registry.AddTransient<ServiceWithServiceProviderConstructor>();

            var service = registry.GetService<ServiceWithServiceProviderConstructor>();
            Assert.IsNotNull(service);
            Assert.AreSame(registry, service.provider);

        }

        [Test]
        public void WhenRequestingTransientWithIncompatibleConstructor_UnknownServiceExceptionIsThrown()
        {
            var registry = new ServiceRegistry();

            Assert.Throws<UnknownServiceException>(() =>
            {
                registry.GetService<ServiceWithIncompatibleConstructor>();
            });
        }
    }
}
