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
        private readonly Version compatibleNodeVersion = new(12, 0);

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
        [DataRow(1)]
        [DataRow(2)]
        public void Locate_NoFoundPath_Null(int numberOfLocators)
        {
            var locators = new List<Mock<INodeLocator>>();
            
            for (var i = 0; i < numberOfLocators; i++)
            {
                locators.Add(SetupNodeLocator(path: null));
            }

            var testSubject = CreateTestSubject(getNodeExeVersion: null, locators: locators.Select(x=> x.Object).ToArray());
            var result = testSubject.Locate();

            result.Should().BeNull();

            foreach (var locator in locators)
            {
                VerifyLocatorCalled(locator);
            }
        }

        [TestMethod]
        [DataRow(9)]
        [DataRow(11)]
        public void Locate_OneLocatorHasIncompatibleVersion_LocatorSkipped(int incompatibleVersion)
        {
            var compatible = SetupNodeLocator("compatible");
            var incompatible = SetupNodeLocator("incompatible");

            var versions = new Dictionary<string, Version>
            {
                {"incompatible", new Version(incompatibleVersion, 0)},
                {"compatible", compatibleNodeVersion}
            };

            Version GetNodeExeVersion(string path) => versions[path];
            
            var testSubject = CreateTestSubject(GetNodeExeVersion, incompatible.Object, compatible.Object);
            var result = testSubject.Locate();

            result.Should().Be("compatible");

            VerifyLocatorCalled(compatible);
            VerifyLocatorCalled(incompatible);
        }

        [TestMethod]
        public void Locate_OneLocatorHasCompatibleVersion_OtherLocatorsNotCalled()
        {
            var compatible1 = SetupNodeLocator("compatible1");
            var compatible2 = SetupNodeLocator("compatible2");

            Version GetNodeExeVersion(string path) => compatibleNodeVersion;

            var testSubject = CreateTestSubject(GetNodeExeVersion, compatible1.Object, compatible2.Object);
            var result = testSubject.Locate();

            result.Should().Be("compatible1");

            VerifyLocatorCalled(compatible1);
            VerifyLocatorNotCalled(compatible2);
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

        private NodeLocatorAggregator CreateTestSubject(Func<string, Version> getNodeExeVersion = null, params INodeLocator[] locators)
        {
            var locatorsProvider = new Mock<INodeLocatorsProvider>();
            locatorsProvider.Setup(x => x.Get()).Returns(locators);

            var logger = Mock.Of<ILogger>();

            return new NodeLocatorAggregator(locatorsProvider.Object, logger, getNodeExeVersion);
        }
    }
}
