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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.TsConfig;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.TsConfig
{
    [TestClass]
    public class TsConfigsLocatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TsConfigsLocator, ITsConfigsLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
            });
        }

        [TestMethod]
        public void Locate_NoMatchingFiles_ReturnsEmptyList()
        {
            var vsUiShellOpenDocument = SetupVsUiShellOpenDocument(VSConstants.S_OK);

            var testSubject = CreateTestSubject(vsUiShellOpenDocument.Object);
            var result = testSubject.Locate();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_HasMatchingFiles_HrResultIsFailed_ReturnsEmptyList()
        {
            var vsUiShellOpenDocument = SetupVsUiShellOpenDocument(VSConstants.E_FAIL, "some path");

            var testSubject = CreateTestSubject(vsUiShellOpenDocument.Object);
            var result = testSubject.Locate();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Locate_HasMatchingFiles_HrResultIsSuccess_ReturnsFoundFiles()
        {
            var expectedFiles = new[] {"path1", "path2"};
            var vsUiShellOpenDocument = SetupVsUiShellOpenDocument(VSConstants.S_OK, expectedFiles);

            var testSubject = CreateTestSubject(vsUiShellOpenDocument.Object);
            var result = testSubject.Locate();

            result.Should().BeEquivalentTo(expectedFiles);
        }

        [TestMethod]
        public void Locate_HasMoreMatchingFilesThanMaxLimit_ReturnsMaxNumberOfFoundFiles()
        {
            var foundFiles = new List<string>();

            for (var i = 0; i < TsConfigsLocator.MaxNumberOfFiles + 1; i++)
            {
                foundFiles.Add("path" + i);
            }

            var vsUiShellOpenDocument = SetupVsUiShellOpenDocument(VSConstants.S_OK, foundFiles.ToArray());

            var testSubject = CreateTestSubject(vsUiShellOpenDocument.Object);
            var result = testSubject.Locate();

            result.Count().Should().Be(TsConfigsLocator.MaxNumberOfFiles);
            result.Should().NotContain(foundFiles.Last());
        }

        private TsConfigsLocator CreateTestSubject(IVsUIShellOpenDocument vsUiShellOpenDocument)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsUIShellOpenDocument))).Returns(vsUiShellOpenDocument);

            return new TsConfigsLocator(serviceProvider.Object);
        }

        private Mock<IVsUIShellOpenDocument> SetupVsUiShellOpenDocument(int hrResult, params string[] foundFiles)
        {
            var vsUiShell = new Mock<IVsUIShellOpenDocument>();
            vsUiShell
                .Setup(x => x.SearchProjectsForRelativePath(
                    (uint) __VSRELPATHSEARCHFLAGS.RPS_UseAllSearchStrategies,
                    "tsconfig.json",
                    It.IsAny<string[]>()))
                .Callback((uint searchFlag, string path, string[] res) =>
                {
                    for (var i = 0; i < Math.Min(res.Length, foundFiles.Length); i++)
                    {
                        res[i] = foundFiles[i];
                    }
                })
                .Returns(hrResult);

            return vsUiShell;
        }
    }
}
