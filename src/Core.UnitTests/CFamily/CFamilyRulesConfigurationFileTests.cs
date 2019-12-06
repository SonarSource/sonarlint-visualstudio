/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class CFamilyRulesConfigurationFileTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            Action act = () => new CFamilyRulesConfigurationFile(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userSettings");
        }

        [TestMethod]
        public void Ctor_ValidArgs()
        {
            var userSettings = new UserSettings();
            var testSubject = new CFamilyRulesConfigurationFile(userSettings);
            testSubject.UserSettings.Equals(userSettings);
        }
    }
}
