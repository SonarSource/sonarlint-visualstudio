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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator
{
    [TestClass]
    public class NodeLocatorAggregatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<NodeLocatorAggregator, INodeLocator>(null, new[]
            {
                MefTestHelpers.CreateExport<INodeLocatorsProvider>(Mock.Of<INodeLocatorsProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public void Locate_NoLocators_Null()
        {
            var testSubject = CreateTestSubject();
            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        [TestMethod]
        public void Locate_ReturnsFirstCompatiblePath()
        {
            var nonExistingFile = SetupNodeLocator("does not exist");
            var badVersion = SetupNodeLocator("bad version");
            var nullPath = SetupNodeLocator(null);
            var compatible1 = SetupNodeLocator("compatible1");
            var compatible2 = SetupNodeLocator("compatible2");

            var fileSystem = new Mock<IFileSystem>();
            SetupFileExists(fileSystem, "does not exist", false);
            SetupFileExists(fileSystem, "bad version", true);
            SetupFileExists(fileSystem, "compatible1", true);
            SetupFileExists(fileSystem, "compatible2", true);

            var versions = new Dictionary<string, Version>
            {
                {"bad version", new Version(11, 0)},
                {"compatible1", new Version(12, 0)},
                {"compatible2", new Version(12, 0)}
            };

            Version GetNodeExeVersion(string path) => versions[path];

            var testSubject = CreateTestSubject(
                fileSystem.Object,
                GetNodeExeVersion,
                nonExistingFile.Object,
                badVersion.Object,
                nullPath.Object,
                compatible1.Object,
                compatible2.Object);

            var result = testSubject.Locate();
            result.Should().Be("compatible1");

            VerifyLocatorCalled(nonExistingFile);
            VerifyLocatorCalled(badVersion);
            VerifyLocatorCalled(nullPath);
            VerifyLocatorCalled(compatible1);
            VerifyLocatorNotCalled(compatible2);
        }

        [TestMethod]
        [DataRow(9, false)]
        [DataRow(10, true)]
        [DataRow(11, false)]
        [DataRow(12, true)]
        [DataRow(13, true)]
        public void IsCompatibleVersion_ReturnsTrueFalse(int majorVersion, bool expectedResult)
        {
            var version = new Version(majorVersion, 0);
            var result = NodeLocatorAggregator.IsCompatibleVersion(version);

            result.Should().Be(expectedResult);
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
            var result = NodeLocatorAggregator.GetNodeVersion(assemblyPath);

            result.Should().BeEquivalentTo(expectedVersion);
        }

        private Mock<INodeLocator> SetupNodeLocator(string path)
        {
            var nodeLocator = new Mock<INodeLocator>();
            nodeLocator.Setup(x => x.Locate()).Returns(path);

            return nodeLocator;
        }

        private void VerifyLocatorCalled(Mock<INodeLocator> locator)
        {
            locator.Verify(x=> x.Locate(), Times.Once);
            locator.VerifyNoOtherCalls();
        }

        private void VerifyLocatorNotCalled(Mock<INodeLocator> locator)
        {
            locator.Verify(x => x.Locate(), Times.Never);
            locator.VerifyNoOtherCalls();
        }

        private void SetupFileExists(Mock<IFileSystem> fileSystem, string path, bool exists)
        {
            fileSystem.Setup(x => x.File.Exists(path)).Returns(exists);
        }

        private NodeLocatorAggregator CreateTestSubject(IFileSystem fileSystem = null, Func<string, Version> getNodeExeVersion = null, params INodeLocator[] locators)
        {
            var locatorsProvider = new Mock<INodeLocatorsProvider>();
            locatorsProvider.Setup(x => x.Get()).Returns(locators);

            var logger = Mock.Of<ILogger>();

            return new NodeLocatorAggregator(locatorsProvider.Object, logger, fileSystem, getNodeExeVersion);
        }
    }
}
