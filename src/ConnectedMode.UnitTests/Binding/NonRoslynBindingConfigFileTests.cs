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

using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using SonarLint.VisualStudio.ConnectedMode.Binding;

namespace SSonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class NonRoslynBindingConfigFileTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            Action act = () => new NonRoslynBindingConfigFile(null, "c:\\test");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rulesSettings");

            act = () => new NonRoslynBindingConfigFile(new RulesSettings(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            act = () => new NonRoslynBindingConfigFile(new RulesSettings(), "");
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("filePath");

            act = () => new NonRoslynBindingConfigFile(new RulesSettings(), "c:\\test", null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Ctor_ValidArgs()
        {
            var settings = new RulesSettings();
            var testSubject = new NonRoslynBindingConfigFile(settings, "c:\\test");
            testSubject.RuleSettings.Should().BeEquivalentTo(settings);
            testSubject.FilePath.Should().BeEquivalentTo("c:\\test");
        }

        [TestMethod]
        public void Save_DirectoryCreatedAndFileSaved()
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

            string filePath = "c:\\full\\path\\file.txt";

            var fileSystem = new MockFileSystem();

            var testSubject = new NonRoslynBindingConfigFile(settings, filePath, fileSystem);

            // Act
            testSubject.Save();

            // Assert
            // Assert
            fileSystem.AllDirectories.Should().BeEquivalentTo(new[]
            {
                "C:\\",             // note: the MockFileSystem capitalises the drive
                "c:\\full",
                "c:\\full\\path",
            });

            fileSystem.AllFiles.Should().BeEquivalentTo(filePath);

            var savedText = fileSystem.File.ReadAllText(filePath);
            savedText.Should().Be(@"{
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
