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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Rules
{
    [TestClass]
    public class RulesProviderTests
    {
        [TestMethod]
        public void Ctor_InvalidArg_Throws()
        {
            Action act = () => new RulesProvider(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleDefinitions");
        }

        [TestMethod]
        public void GetDefinitions_ReturnsExpectedDefinitions()
        {
            var defns = new RuleDefinition[]
            {
                new RuleDefinition { RuleKey = "key1" },
                new RuleDefinition { RuleKey = "key2" }
            };

            var testSubject = new RulesProvider(defns);

            testSubject.GetDefinitions().Should().BeEquivalentTo(defns);
        }
    }
}
