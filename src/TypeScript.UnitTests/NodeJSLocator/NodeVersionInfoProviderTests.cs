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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.JsTs;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator
{
    [TestClass]
    public class NodeVersionInfoProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<NodeVersionInfoProvider, INodeVersionInfoProvider>(
                MefTestHelpers.CreateExport<INodeLocationsProvider>());
        }

        [TestMethod]
        public void GetAll_NoCandidateLocations_EmptyList()
        {
            var testSubject = CreateTestSubject();
            var result = testSubject.GetAllNodeVersions();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetAll_ReturnsVersionsOfFilesThatExistOnDisk()
        {
            var candidateLocations = new List<string>
            {
                "does not exist",
                "version1",
                null,
                "version2"
            };

            var fileSystem = new Mock<IFileSystem>();
            SetupFileExists(fileSystem, "does not exist", false);
            SetupFileExists(fileSystem, "version1", true);
            SetupFileExists(fileSystem, "version2", true);

            var versions = new Dictionary<string, Version>
            {
                {"version1", new Version(11, 0)},
                {"version2", new Version(12, 0)}
            };

            Version GetNodeExeVersion(string path) => versions[path];

            var testSubject = CreateTestSubject(
                fileSystem.Object,
                GetNodeExeVersion,
                candidateLocations);

            var result = testSubject.GetAllNodeVersions();

            result.Should().BeEquivalentTo(
                new NodeVersionInfo("version1", new Version(11, 0)),
                new NodeVersionInfo("version2", new Version(12, 0)));
        }

        [TestMethod]
        public void GetAll_UsesYieldReturn()
        {
            var fileSystem = new Mock<IFileSystem>();
            SetupFileExists(fileSystem, "version1", true);
            SetupFileExists(fileSystem, "version2", true);

            var versions = new Dictionary<string, Version>
            {
                {"version1", new Version(11, 0)},
                {"version2", new Version(12, 0)}
            };

            Version GetNodeExeVersion(string path) => versions[path];

            var testSubject = CreateTestSubject(
                fileSystem.Object,
                GetNodeExeVersion,
                versions.Keys);

            var result = testSubject.GetAllNodeVersions();

            using var enumerator = result.GetEnumerator();
            enumerator.MoveNext();

            enumerator.Current.Should().BeEquivalentTo(new NodeVersionInfo("version1", new Version(11, 0)));

            fileSystem.Verify(x=> x.File.Exists("version1"), Times.Once);
            fileSystem.Verify(x=> x.File.Exists("version2"), Times.Never);
            fileSystem.VerifyNoOtherCalls();

            enumerator.MoveNext();
            enumerator.Current.Should().BeEquivalentTo(new NodeVersionInfo("version2", new Version(12, 0)));
            
            fileSystem.Verify(x => x.File.Exists("version1"), Times.Once);
            fileSystem.Verify(x => x.File.Exists("version2"), Times.Once);
            fileSystem.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetNodeVersion_ReturnsFileProductVersion()
        {
            var assemblyPath = Assembly.GetAssembly(typeof(ExportAttribute)).Location;
            var assemblyVersion = FileVersionInfo.GetVersionInfo(assemblyPath);

            var expectedVersion = new Version(assemblyVersion.ProductMajorPart,
                assemblyVersion.ProductMinorPart,
                assemblyVersion.ProductBuildPart);

            // The implementation relies on checking a file in the file system, so we pass a file that we know already exists and has a product version
            var result = NodeVersionInfoProvider.GetNodeVersion(assemblyPath);

            result.Should().BeEquivalentTo(expectedVersion);
        }

        private void SetupFileExists(Mock<IFileSystem> fileSystem, string path, bool exists)
        {
            fileSystem.Setup(x => x.File.Exists(path)).Returns(exists);
        }

        private NodeVersionInfoProvider CreateTestSubject(IFileSystem fileSystem = null, Func<string, Version> getNodeExeVersion = null, IReadOnlyCollection<string> candidateLocations = null)
        {
            candidateLocations ??= Array.Empty<string>();
            var locationsProvider = new Mock<INodeLocationsProvider>();
            locationsProvider.Setup(x => x.Get()).Returns(candidateLocations);

            return new NodeVersionInfoProvider(locationsProvider.Object, fileSystem, getNodeExeVersion);
        }
    }
}
