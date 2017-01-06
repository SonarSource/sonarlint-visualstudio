/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

            // Verify
            Assert.IsNotNull(actual);
            Assert.AreSame(this.serviceInstance, actual);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingService service = this.serviceProvider.GetService<IMissingService>();

            // Verify
            Assert.IsNull(service);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetServiceOfTU_ReturnsServiceU()
        {
            // Act
            IOther actual = this.serviceProvider.GetService<IService, IOther>();

            // Verify
            Assert.IsNotNull(actual);
            Assert.AreSame(this.serviceInstance, actual);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_ReturnsServiceT()
        {
            // Act
            IMefService actual = this.serviceProvider.GetMefService<IMefService>();

            // Verify
            Assert.IsNotNull(actual);
            Assert.AreSame(this.mefServiceInstance, actual);
        }

        [TestMethod]
        public void IServiceProviderExtensions_GetMefServiceOfT_NotFound_ReturnsNull()
        {
            // Act
            IMissingMefService mefService = this.serviceProvider.GetMefService<IMissingMefService>();

            // Verify
            Assert.IsNull(mefService);
        }

        #endregion

        #region Test Services and Interfaces

        private interface IMissingService { }
        private interface IService { }
        private interface IOther { }
        private class TestService : IService, IOther { }

        private interface IMissingMefService { }
        private interface IMefService { }
        private class TestMefService : IMefService { }

        #endregion
    }
}
