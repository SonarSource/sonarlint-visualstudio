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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Rules
{
    [TestClass]
    public class RulesProviderTests
    {
        private static readonly ActiveRulesCalculator ValidActiveRulesCalculator = new ActiveRulesCalculator(null, null, null);

        [TestMethod]
        public void Ctor_InvalidArg_Throws()
        {
            Action act = () => new RulesProvider(null, Mock.Of<IActiveRulesCalculator>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("ruleDefinitions");

            act = () => new RulesProvider(Array.Empty<RuleDefinition>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("activeRulesCalculator");
        }

        [TestMethod]
        public void GetDefinitions_ReturnsExpected()
        {
            var defns = new RuleDefinition[]
            {
                new RuleDefinition { RuleKey = "key1" },
                new RuleDefinition { RuleKey = "key2" }
            };

            var testSubject = new RulesProvider(defns, ValidActiveRulesCalculator);

            testSubject.GetDefinitions().Should().BeEquivalentTo(defns);
        }


        [TestMethod]
        public void GetActiveRulesConfig_ReturnsExpected()
        {
            var defns = Array.Empty<RuleDefinition>();
            var activeRulesConfig = new[]
            {
                new Rule{ Key = "key1" }, new Rule{ Key = "key2" }
            };

            var calculator = new Mock<IActiveRulesCalculator>();
            calculator.Setup(x => x.Calculate()).Returns(activeRulesConfig);

            var testSubject = new RulesProvider(defns, calculator.Object);

            testSubject.GetActiveRulesConfiguration().Should().BeEquivalentTo(activeRulesConfig);
            calculator.VerifyAll();
            calculator.VerifyNoOtherCalls();
        }
    }
}
