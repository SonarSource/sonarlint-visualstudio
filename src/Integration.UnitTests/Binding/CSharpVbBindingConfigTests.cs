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
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class CSharpVbBindingConfigTests
    {
        private static readonly RuleSet ValidRuleSet = new RuleSet { Name = "any" };

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            Action act = () => new CSharpVBBindingConfig(null, "dummy");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleSet");

            act = () => new CSharpVBBindingConfig(ValidRuleSet, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            act = () => new CSharpVBBindingConfig(ValidRuleSet, "");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        }

        [TestMethod]
        public void GetSolutionLevelFilePaths_ReturnPathToRulesetFile()
        {
            var testSubject = new CSharpVBBindingConfig(ValidRuleSet, "c:\\test.txt");
            testSubject.SolutionLevelFilePaths.Count().Should().Be(1);
            testSubject.SolutionLevelFilePaths.First().Should().Be(testSubject.FilePath);
        }

        [TestMethod]
        public void Save_ValidFilePath_SaveCalled()
        {
            // Arrange
            const string expectedPath = "c:\\aaa\file.xxx";
            string actualPath = null;
            string actualText = null;

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((p, t) => { actualPath = p; actualText = t; });

            var testSubject = new CSharpVBBindingConfig(ValidRuleSet, expectedPath, fileSystemMock.Object);

            // Act
            testSubject.Save();

            // Assert
            actualPath.Should().Be(expectedPath);
            actualText.Should().Be(ValidRuleSet.ToXml());
        }
    }
}
