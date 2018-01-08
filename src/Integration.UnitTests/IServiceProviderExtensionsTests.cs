/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            this.serviceInstance = new TestService();

            // Create a MEF service
            this.mefServiceInstance = new TestMefService();
            var mefExports = MefTestHelpers.CreateExport<IMefService>(this.mefServiceInstance);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);

            // Register services
            var sp = new ConfigurableServiceProvider(false);
            sp.RegisterService(typeof(IService), this.serviceInstance);
            sp.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            this.serviceProvider = sp;
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
        public void IServiceProviderExtensions_GetServiceOfT_ReturnsServiceT()
        {
            // Act
            IService actual = this.serviceProvider.GetService<IService>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(this.serviceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingService service = this.serviceProvider.GetService<IMissingService>();

            // Assert
            service.Should().BeNull();
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfTU_ReturnsServiceU()
        {
            // Act
            IOther actual = this.serviceProvider.GetService<IService, IOther>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(this.serviceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_ReturnsServiceT()
        {
            // Act
            IMefService actual = this.serviceProvider.GetMefService<IMefService>();

            // Assert
            actual.Should().NotBeNull();
            actual.Should().Be(this.mefServiceInstance);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingMefService mefService = this.serviceProvider.GetMefService<IMissingMefService>();

            // Assert
            mefService.Should().BeNull();
        }

        #endregion Tests

        #region Test Services and Interfaces

        private interface IMissingService { }

        private interface IService { }

        private interface IOther { }

        private class TestService : IService, IOther { }

        private interface IMissingMefService { }

        private interface IMefService { }

        private class TestMefService : IMefService { }

        #endregion Test Services and Interfaces
    }
}