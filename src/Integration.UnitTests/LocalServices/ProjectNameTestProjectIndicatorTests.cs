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

using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class ProjectNameTestProjectIndicatorTests
    {
        private Mock<ILogger> logger;
        private Project project;
        private ProjectNameTestProjectIndicator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            project = Mock.Of<Project>();
            project.Name = "a.test.b";

            logger = new Mock<ILogger>();
            testSubject = new ProjectNameTestProjectIndicator(logger.Object);
        }

        [TestMethod]
        public void SetTestRegex_Null_DefaultRegexUsed()
        {
            testSubject.SetTestRegex(null);

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void SetTestRegex_InvalidRegex_DefaultRegexUsed()
        {
            testSubject.SetTestRegex("[0-9]++");

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void SetTestRegex_ValidRegex_NewRegexUsed()
        {
            testSubject.SetTestRegex("^b");

            var actual = testSubject.IsTestProject(project);
            actual.Should().BeNull();
        }

        [DataTestMethod]
        [DataRow("", null)]
        [DataRow("test\\", null)]
        [DataRow("\\asd\\.!@#$%sdftest234234@~!sdasd\\", null)]
        [DataRow(".!@#$%sdftest", true)]
        [DataRow(".!@#$%sdftest234234@~!sdasd", true)]
        [DataRow("\\asd\\.!@#$%sdftest234234@~!sdasd", true)]
        [DataRow("test", true)]
        [DataRow("thetestisalie", true)]
        public void IsTestProject_RegexTests(string projectName, bool? expectedResult)
        {
            project.Name = projectName;

            var actual = testSubject.IsTestProject(project);
            actual.Should().Be(expectedResult);
        }
    }
}
