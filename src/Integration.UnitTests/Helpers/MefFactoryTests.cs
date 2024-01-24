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

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class MefFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MefFactory, IMefFactory>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public void CheckIsSingletonMefComponent()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<MefFactory>();
        }

        [TestMethod]
        public async Task CreateAsync_IsCalledOnMainThread()
        {
            var serviceProvider = SetUpServiceProviderWithComponentModel();
            var threadHandling = new Mock<IThreadHandling>();
            Action runOnUiAction = null;
            threadHandling
                .Setup(x => x.RunOnUIThreadAsync(It.IsAny<Action>()))
                .Callback((Action callbackAction) => runOnUiAction = callbackAction);

            var testSubject = new MefFactory(serviceProvider.Object, threadHandling.Object);
            await testSubject.CreateAsync<NonSharedEmptyTestMefImplementation>();

            threadHandling.Verify(x => x.RunOnUIThreadAsync(It.IsAny<Action>()), Times.Once);

            serviceProvider.Invocations.Should().HaveCount(0);

            runOnUiAction.Should().NotBeNull();
            runOnUiAction();

            serviceProvider.Verify(x => x.GetService(typeof(SComponentModel)), Times.Once);
        }

        [TestMethod]
        public async Task CreateAsync_ReturnCorrectObject()
        {
            var testObject = new NonSharedEmptyTestMefImplementation();

            var serviceProvider = SetUpServiceProviderWithComponentModel(testObject);

            var testSubject = new MefFactory(serviceProvider.Object, new NoOpThreadHandler());
            var result = await testSubject.CreateAsync<NonSharedEmptyTestMefImplementation>();

            result.Should().Be(testObject);
        }

        private Mock<IServiceProvider> SetUpServiceProviderWithComponentModel(NonSharedEmptyTestMefImplementation testObject = null)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var componentModel = new Mock<IComponentModel>();

            componentModel.Setup(x => x.GetService<NonSharedEmptyTestMefImplementation>()).Returns(testObject);
            serviceProvider.Setup(x => x.GetService(typeof(SComponentModel))).Returns(componentModel.Object);

            return serviceProvider;
        }

        [Export(typeof(NonSharedEmptyTestMefImplementation))]
        [PartCreationPolicy(CreationPolicy.NonShared)]
        internal class NonSharedEmptyTestMefImplementation { }
    }
}
