/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily
{
    [TestClass]
    public class VcxRequestFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VcxRequestFactory, IRequestFactory>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<ICFamilyRulesConfigProvider>(Mock.Of<ICFamilyRulesConfigProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void TryGet_NoProjectItem_Null()
        {
            var testSubject = CreateTestSubject(projectItem: null);

            var request = testSubject.TryGet("path", new CFamilyAnalyzerOptions());

            request.Should().BeNull();
        }

        private VcxRequestFactory CreateTestSubject(ProjectItem projectItem)
        {
            var serviceProvider = CreateServiceProviderReturningProjectItem(projectItem);

            var cFamilyRulesConfigProvider = Mock.Of<ICFamilyRulesConfigProvider>();
            var logger = Mock.Of<ILogger>();

            return new VcxRequestFactory(serviceProvider.Object, cFamilyRulesConfigProvider, logger);
        }

        private static Mock<IServiceProvider> CreateServiceProviderReturningProjectItem(ProjectItem projectItemToReturn)
        {
            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItemToReturn);

            var mockDTE = new Mock<DTE>();
            mockDTE.Setup(d => d.Solution).Returns(mockSolution.Object);

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(DTE))).Returns(mockDTE.Object);

            return mockServiceProvider;
        }
    }
}
