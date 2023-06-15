/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Migration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration
{
    [TestClass]
    public class LegacySettingsTests
    {
        [TestMethod]
        public void Ctor_RulesetPathIsRequired()
        {
            Action action = () => new LegacySettings(null, "sonarlint.xml");    
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialRuleSetPath");
        }

        [TestMethod]
        public void Ctor_SonarLintPathsRequired()
        {
            Action action = () => new LegacySettings("x.ruleset", null);
            action.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("partialSonarLintXmlPath");
        }

        [TestMethod]
        public void Ctor_ValidArgs_PropertiesSetCorrectly()
        {
            var testSubject = new LegacySettings("c:\\bar\\x.ruleset", "c:\\foo\\SonarLint.xml");

            testSubject.PartialRuleSetPath.Should().Be("c:\\bar\\x.ruleset");
            testSubject.PartialSonarLintXmlPath.Should().Be("c:\\foo\\SonarLint.xml");
        }
    }
}
