/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IServiceProviderExtensionsTests
    {
        private TestService serviceInstance;
        private TestMefService mefServiceInstance;
        private IServiceProvider serviceProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a non-MEF service
            serviceInstance = new TestService();

            // Create a MEF service
            mefServiceInstance = new TestMefService();
            var mefExports = MefTestHelpers.CreateExport<IMefService>(mefServiceInstance);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            // Register services
            var sp = new ConfigurableServiceProvider(false);
            sp.RegisterService(typeof(IService), serviceInstance);
            sp.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            serviceProvider = sp;
        }

        #region Tests

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfT_NullArgChecks()
        {
            IServiceProvider nullServiceProvider = null;
            Exceptions.Expect<ArgumentNullException>(() => nullServiceProvider.GetService<IService>());
            Exceptions.Expect<ArgumentNullException>(() => IServiceProviderExtensions.GetService<IService>(nullServiceProvider));
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfTU_NullArgChecks()
        {
            IServiceProvider nullServiceProvider = null;
            Exceptions.Expect<ArgumentNullException>(() => nullServiceProvider.GetService<IService, IOther>());
            Exceptions.Expect<ArgumentNullException>(() => IServiceProviderExtensions.GetService<IService, IOther>(nullServiceProvider));
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefService_NullArgChecks()
        {
            IServiceProvider nullServiceProvider = null;
            Exceptions.Expect<ArgumentNullException>(() => nullServiceProvider.GetMefService<IService>());
            Exceptions.Expect<ArgumentNullException>(() => IServiceProviderExtensions.GetMefService<IService>(nullServiceProvider));
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceAsync_NullArgChecks()
        {
            IAsyncServiceProvider nullServiceProvider = null;
            Exceptions.Expect<ArgumentNullException>(() => nullServiceProvider.GetMefServiceAsync<IService>());
            Exceptions.Expect<ArgumentNullException>(() => IServiceProviderExtensions.GetMefServiceAsync<IService>(nullServiceProvider));
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfT_ReturnsServiceT()
        {
            // Act
            IService actual = serviceProvider.GetService<IService>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(serviceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingService service = serviceProvider.GetService<IMissingService>();

            // Assert
            service.Should().BeNull();
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfTU_ReturnsServiceU()
        {
            // Act
            IOther actual = serviceProvider.GetService<IService, IOther>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(serviceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_ReturnsServiceT()
        {
            // Act
            IMefService actual = serviceProvider.GetMefService<IMefService>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(mefServiceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingMefService mefService = serviceProvider.GetMefService<IMissingMefService>();

            // Assert
            mefService.Should().BeNull();
        }

        #endregion Tests

        #region Test Services and Interfaces

        private interface IMissingService
        {
        }

        private interface IService
        {
        }

        private interface IOther
        {
        }

        private class TestService : IService, IOther
        {
        }

        private interface IMissingMefService
        {
        }

        private interface IMefService
        {
        }

        private class TestMefService : IMefService
        {
        }

        #endregion Test Services and Interfaces
    }
}
