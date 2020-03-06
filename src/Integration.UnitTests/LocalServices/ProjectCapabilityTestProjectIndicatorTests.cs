/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
            var serviceProvider = VsServiceProviderHelper.GlobalServiceProvider;
            var configurableVsProjectSystemHelper = new ConfigurableVsProjectSystemHelper(serviceProvider);
            serviceProvider.RegisterService(typeof(IProjectSystemHelper), configurableVsProjectSystemHelper, true);

            SetupCapabilityEvaluator(serviceProvider);

            testSubject = new ProjectCapabilityTestProjectIndicator(serviceProvider);
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
            var vsHierarchy = projectMock as IVsHierarchy;
            vsHierarchy.SetProperty(VSConstants.VSITEMID_ROOT, -2124, capability);
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
