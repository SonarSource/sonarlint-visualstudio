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
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyBindingConfigTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            Action act = () => new CFamilyBindingConfig(null, "c:\\test");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rulesSettings");

            act = () => new CFamilyBindingConfig(new RulesSettings(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            act = () => new CFamilyBindingConfig(new RulesSettings(), "");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            act = () => new CFamilyBindingConfig(new RulesSettings(), "c:\\test", null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Ctor_ValidArgs()
        {
            var settings = new RulesSettings();
            var testSubject = new CFamilyBindingConfig(settings, "c:\\test");
            testSubject.RuleSettings.Equals(settings);
            testSubject.FilePath.Equals("c:\\test");
        }

        [TestMethod]
        public void GetSolutionLevelFilePaths_ReturnPathToSettingsFile()
        {
            var settings = new RulesSettings();
            var testSubject = new CFamilyBindingConfig(settings, "c:\\test");
            testSubject.SolutionLevelFilePaths.Count().Should().Be(1);
            testSubject.SolutionLevelFilePaths.First().Should().Be(testSubject.FilePath);
        }

        [TestMethod]
        public void Save_SettingsAreSerializedAndSaved()
        {
            // Arrange
            var settings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    { "key", new RuleConfig
                        {
                            Level = RuleLevel.On,
                            Severity = IssueSeverity.Minor,
                            Parameters = new Dictionary<string, string>
                            {
                                { "p1", "p2" }
                            }
                        }
                    }
                }
            };

            string actualPath = null;
            string actualText = null;

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((p, t) => { actualPath = p; actualText = t; });

            var testSubject = new CFamilyBindingConfig(settings, "c:\\full\\path\\file.txt", fileSystemMock.Object);

            // Act
            testSubject.Save();

            // Assert
            actualPath.Should().Be("c:\\full\\path\\file.txt");
            actualText.Should().Be(@"{
  ""sonarlint.rules"": {
    ""key"": {
      ""level"": ""On"",
      ""parameters"": {
        ""p1"": ""p2""
      },
      ""severity"": ""Minor""
    }
  }
}");
        }
    }
}
