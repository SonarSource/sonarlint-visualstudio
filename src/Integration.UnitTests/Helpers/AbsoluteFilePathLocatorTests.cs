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
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    [TestClass]
    public class AbsoluteFilePathLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsUIShellOpenDocument))).Returns(Mock.Of<IVsUIShellOpenDocument>());

            MefTestHelpers.CheckTypeCanBeImported<AbsoluteFilePathLocator, IAbsoluteFilePathLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(serviceProvider.Object)
            });
        }

        [TestMethod]
        public void Locate_RelativePathIsNull_ArgumentNullException()
        {
            var vsUiShell = new Mock<IVsUIShellOpenDocument>();
            
            var testSubject = new AbsoluteFilePathLocator(CreateServiceProvider(vsUiShell.Object));

            Action act = () => testSubject.Locate(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("relativeFilePath");
            vsUiShell.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void Locate_BadHrResult_Null()
        {
            const string relativePath = "some relative path";
            const string absolutePath = "some absolute path";
            var vsUiShell = SetupVsUiShellOpenDocument(relativePath, VSConstants.E_FAIL, absolutePath);

            var testSubject = new AbsoluteFilePathLocator(CreateServiceProvider(vsUiShell.Object));

            var result = testSubject.Locate(relativePath);
            result.Should().BeNull();

            vsUiShell.VerifyAll();
            vsUiShell.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_NoMatches_Null()
        {
            const string relativePath = "some relative path";

            var vsUiShell = SetupVsUiShellOpenDocument(relativePath, VSConstants.S_OK, null);

            var testSubject = new AbsoluteFilePathLocator(CreateServiceProvider(vsUiShell.Object));

            var result = testSubject.Locate(relativePath);
            result.Should().BeNull();

            vsUiShell.VerifyAll();
            vsUiShell.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_HasMatch_MatchReturned()
        {
            const string path = "some relative path";
            const string absolutePath = "some absolute path";

            var vsUiShell = SetupVsUiShellOpenDocument(path, VSConstants.S_OK, absolutePath);

            var testSubject = new AbsoluteFilePathLocator(CreateServiceProvider(vsUiShell.Object));

            var result = testSubject.Locate(path);
            result.Should().Be(absolutePath);

            vsUiShell.VerifyAll();
            vsUiShell.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_RelativePathStartsWithSlashes_PathTrimmed()
        {
            const string path = "some relative path";
            const string absolutePath = "some absolute path";

            var vsUiShell = SetupVsUiShellOpenDocument(path, VSConstants.S_OK, absolutePath);

            var testSubject = new AbsoluteFilePathLocator(CreateServiceProvider(vsUiShell.Object));

            var result = testSubject.Locate("\\some relative path");
            result.Should().Be(absolutePath);

            vsUiShell.VerifyAll();
            vsUiShell.VerifyNoOtherCalls();
        }

        private IServiceProvider CreateServiceProvider(IVsUIShellOpenDocument vsUiShellOpenDocument)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsUIShellOpenDocument))).Returns(vsUiShellOpenDocument);

            return serviceProvider.Object;
        }

        private Mock<IVsUIShellOpenDocument> SetupVsUiShellOpenDocument(string relativePath, int hrResult, string absolutePath)
        {
            var vsUiShell = new Mock<IVsUIShellOpenDocument>();
            vsUiShell
                .Setup(x => x.SearchProjectsForRelativePath(
                    (uint)__VSRELPATHSEARCHFLAGS.RPS_UseAllSearchStrategies,
                    relativePath,
                    It.IsAny<string[]>()))
                .Callback((uint searchFlag, string path, string[] res) =>
                {
                    res[0] = absolutePath;
                })
                .Returns(hrResult);

            return vsUiShell;
        }
    }
}
