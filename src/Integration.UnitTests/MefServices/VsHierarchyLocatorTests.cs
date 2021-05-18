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
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.MefServices;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices
{
    [TestClass]
    public class VsHierarchyLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<VsHierarchyLocator, IVsHierarchyLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>())
            });
        }

        [TestMethod]
        public void GetFileVsHierarchy_CallsProjectSystemHelper()
        {
            var hierarchy = Mock.Of<IVsHierarchy>();
            var projectSystemHelper = new Mock<IProjectSystemHelper>();
            projectSystemHelper.Setup(x => x.GetFileVsHierarchy("some file")).Returns(hierarchy);

            var testSubject = CreateTestSubject(projectSystemHelper.Object);
            var result = testSubject.GetFileVsHierarchy("some file");

            result.Should().Be(hierarchy);

            projectSystemHelper.Verify(x=> x.GetFileVsHierarchy("some file"), Times.Once);
            projectSystemHelper.VerifyNoOtherCalls();
        }

        private VsHierarchyLocator CreateTestSubject(IProjectSystemHelper projectSystemHelper) => 
            new VsHierarchyLocator(projectSystemHelper);
    }
}
