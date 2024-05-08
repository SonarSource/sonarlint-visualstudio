/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator
{
    [TestClass]
    public class NodeLocationsProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<NodeLocationsProvider, INodeLocationsProvider>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
            => MefTestHelpers.CheckIsSingletonMefComponent<NodeLocationsProvider>();

        [TestMethod]
        public void Ctor_DoesNotCallAnyServices()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger>();

            _ = new NodeLocationsProvider(serviceProvider.Object, logger.Object);

            serviceProvider.Invocations.Should().BeEmpty();
            logger.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_ProvidersListIsLazy()
        {
            var testSubject = new NodeLocationsProvider(Mock.Of<IServiceProvider>(), Mock.Of<ILogger>());
            testSubject.LocationProviders.IsValueCreated.Should().BeFalse();
        }

        [TestMethod]
        public void Ctor_InitializesCorrectProviders_InPriorityOrder()
        {
            var testSubject = new NodeLocationsProvider(Mock.Of<IServiceProvider>(), Mock.Of<ILogger>());

            testSubject.LocationProviders.Value.Count.Should().Be(3);
            testSubject.LocationProviders.Value[0].Should().BeOfType<EnvironmentVariableNodeLocationsProvider>();
            testSubject.LocationProviders.Value[1].Should().BeOfType<GlobalPathNodeLocationsProvider>();
            testSubject.LocationProviders.Value[2].Should().BeOfType<BundledNodeLocationsProvider>();
        }

        [TestMethod]
        public void Get_NoLocationProviders_EmptyList()
        {
            var testSubject = CreateTestSubject();
            var result = testSubject.Get();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Get_HasLocationProviders_AggregatedNotDistinctList()
        {
            var provider1 = SetupLocationsProvider(new[] { "path1" });
            var provider2 = SetupLocationsProvider(null);
            var provider3 = SetupLocationsProvider(new[] { "path2", null, "" });
            var provider4 = SetupLocationsProvider(new[] { "path3", "path4", "path1" });

            var testSubject = CreateTestSubject(provider1, provider2, provider3, provider4);
            var result = testSubject.Get();

            result.Should().BeEquivalentTo("path1", "path2", "path3", "path4", "path1");
        }

        private INodeLocationsProvider SetupLocationsProvider(string[] paths)
        {
            var provider = new Mock<INodeLocationsProvider>();
            provider.Setup(x => x.Get()).Returns(paths);

            return provider.Object;
        }

        private NodeLocationsProvider CreateTestSubject(params INodeLocationsProvider[] locationsProviders)
        {
            var locationProvidersLazy = new Lazy<IList<INodeLocationsProvider>>(() => locationsProviders);
            return new NodeLocationsProvider(locationProvidersLazy);
        }
    }
}
