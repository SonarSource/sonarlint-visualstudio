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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyBindingConfigFileTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            Action act = () => new CFamilyBindingConfigFile(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettings");
        }

        [TestMethod]
        public void Ctor_ValidArgs()
        {
            var userSettings = new UserSettings();
            var testSubject = new CFamilyBindingConfigFile(userSettings);
            testSubject.UserSettings.Equals(userSettings);
        }

        [TestMethod]
        public void Save_SettingsAreSerializedAndSaved()
        {
            // Arrange
            var settings = new UserSettings
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

            var fileSystemMock = new Mock<IFile>();
            fileSystemMock.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((p, t) => { actualPath = p; actualText = t; });

            var testSubject = new CFamilyBindingConfigFile(settings, fileSystemMock.Object);

            // Act
            testSubject.Save("c:\\full\\path\\file.txt");

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
