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

using SonarLint.VisualStudio.ConnectedMode.Migration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class LegacySettingsTests
    {
        [TestMethod]
        public void Ctor_AllArgumentsAreRequired()
        {
            Action action = () => new LegacySettings(null, "cs ruleset", "cs sonarlint", "vb ruleset", "vb sonarlint");    
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("sonarLintFolderPath");

            action = () => new LegacySettings("folder", null, "cs sonarlint", "vb ruleset", "vb sonarlint");
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialCSharpRuleSetPath");

            action = () => new LegacySettings("folder", "cs ruleset", null, "vb ruleset", "vb sonarlint");
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialCSharpSonarLintXmlPath");

            action = () => new LegacySettings("folder", "cs ruleset", "cs sonarlint", null, "vb sonarlint");
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialVBRuleSetPath");

            action = () => new LegacySettings("folder", "cs ruleset", "cs sonarlint", "vb ruleset", null);
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialVBSonarLintXmlPath");
        }

        [TestMethod]
        public void Ctor_ValidArgs_PropertiesSetCorrectly()
        {
            var testSubject = new LegacySettings(
                "c:\\foo\\.sonarlint",
                "c:\\csharp\\x.ruleset",
                "c:\\csharp\\SonarLint.xml",
                "c:\\vb\\x.ruleset",
                "c:\\vb\\SonarLint.xml");

            testSubject.LegacySonarLintFolderPath.Should().Be("c:\\foo\\.sonarlint");

            testSubject.PartialCSharpRuleSetPath.Should().Be("c:\\csharp\\x.ruleset");
            testSubject.PartialCSharpSonarLintXmlPath.Should().Be("c:\\csharp\\SonarLint.xml");

            testSubject.PartialVBRuleSetPath.Should().Be("c:\\vb\\x.ruleset");
            testSubject.PartialVBSonarLintXmlPath.Should().Be("c:\\vb\\SonarLint.xml");
        }
    }
}
