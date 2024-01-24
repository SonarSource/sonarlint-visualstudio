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
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using IOLEServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using VsServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectCapabilityTestProjectIndicatorTests
    {
        private ProjectCapabilityTestProjectIndicator testSubject;
        private const string TestCapability = "TestContainer";

        [TestInitialize]
        public void TestInitialize()
        {
            var serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: true);
            var configurableVsProjectSystemHelper = new ConfigurableVsProjectSystemHelper(serviceProvider);

            var mefHost = ConfigurableComponentModel.CreateWithExports(
                MefTestHelpers.CreateExport<IProjectSystemHelper>(configurableVsProjectSystemHelper));
            serviceProvider.RegisterService(typeof(SComponentModel), mefHost);

            SetupCapabilityEvaluator(serviceProvider);

            SetupVSGlobalServiceProvider(serviceProvider);
            
            testSubject = new ProjectCapabilityTestProjectIndicator(serviceProvider);
        }

        private void SetupVSGlobalServiceProvider(IOLEServiceProvider oleServiceProvider)
        {
            // The product code is calling a VS class that internally uses the VS global ServiceProvider instance.
            // There's no public way to mock this, but we can use reflection to inject our OLE service provider.

            // Note: VsServiceProvider.CreateFromSetSite(...) can also be used to set the global service provider. However,
            // it has side-effects (additional initialisation that needs other services to be mocked. See
            // https://www.nuget.org/packages/Microsoft.VisualStudio.Sdk.TestFramework/16.5.22-beta for more information.

            // 1. Create a new VS service provider that will delegate calls to our OLE service provider.
            var vsServiceProvider = new VsServiceProvider(oleServiceProvider);

            // 2. Update the static field in the VS ServiceProvider to make the new VSServiceProvider the global provider.
            var globalProviderField = typeof(VsServiceProvider).GetField("globalProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            globalProviderField.SetValue(null, vsServiceProvider);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new ProjectCapabilityTestProjectIndicator(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void IsTestProject_ProjectHasNoCapabilities_Null()
        {
            var projectMock = new ProjectMock("csproj.csproj");

            var actual = testSubject.IsTestProject(projectMock);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasNonTestCapability_Null()
        {
            var projectMock = new ProjectMock("csproj.csproj");
            SetCapability(projectMock, "some other capability");

            var actual = testSubject.IsTestProject(projectMock);
            actual.Should().BeNull();
        }

        [TestMethod]
        public void IsTestProject_ProjectHasTestCapability_True()
        {
            var projectMock = new ProjectMock("csproj.csproj");
            SetCapability(projectMock, TestCapability);

            var actual = testSubject.IsTestProject(projectMock);
            actual.Should().BeTrue();
        }

        private static void SetCapability(ProjectMock projectMock, string capability)
        {
            var vsHierarchy = (IVsHierarchy)projectMock;
            vsHierarchy.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID5.VSHPROPID_ProjectCapabilities, capability);
        }

        private static void SetupCapabilityEvaluator(ConfigurableServiceProvider serviceProvider)
        {
            var booleanEvaluator = new Mock<IVsBooleanSymbolExpressionEvaluator>();
            booleanEvaluator
                .Setup(x => x.EvaluateExpression(TestCapability, TestCapability))
                .Returns(true);

            var localRegister = new Mock<ILocalRegistry>();
            var iidIunknown = VSConstants.IID_IUnknown;
            var iUnknownForObject = Marshal.GetIUnknownForObject(booleanEvaluator.Object);

            localRegister
                .Setup(x => x.CreateInstance(typeof(BooleanSymbolExpressionEvaluator).GUID, (object)null,
                    ref iidIunknown, 1U, out iUnknownForObject));

            serviceProvider.RegisterService(typeof(SLocalRegistry), localRegister.Object, true);
        }
    }
}
