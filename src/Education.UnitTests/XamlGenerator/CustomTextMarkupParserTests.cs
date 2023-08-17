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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Education.XamlGenerator;

namespace SonarLint.VisualStudio.Education.UnitTests.XamlGenerator
{
    [TestClass]
    public class CustomTextMarkupParserTests
    {
        [TestMethod]
        public void ProcessRawText_Multiline_OneCrossReference_WithSurroundingText_ReturnsResultWithCorrectItems()
        {
            string text = " {rule:javascript:S1656} -" +
                          "\n Implements a check " +
                          "\non";

            var result = CustomTextMarkupParser.Parse(text).ToArray();

            result.Should().HaveCount(3);

            var firstElement = result[0];
            firstElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)firstElement).Text.Should().Be(" ");

            var secondElement = result[1];
            secondElement.Should().BeOfType<RuleCrossRef>();

            var ruleCrossRef = (IRuleCrossRef)secondElement;
            ruleCrossRef.CompositeRuleId.RepoKey.Should().Be("javascript");
            ruleCrossRef.CompositeRuleId.RuleKey.Should().Be("S1656");

            var lastElement = result[2];
            lastElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)lastElement).Text.Should().Be(" -\n Implements a check \non");
        }

        [TestMethod]
        public void ProcessRawText_OneCrossReference_WithSurroundingText_ReturnsResultWithCorrectItems()
        {
            string text = " {rule:javascript:S1656} - Implements a check on";

            var result = CustomTextMarkupParser.Parse(text).ToArray();

            result.Should().HaveCount(3);

            var firstElement = result[0];
            firstElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)firstElement).Text.Should().Be(" ");

            var secondElement = result[1];
            secondElement.Should().BeOfType<RuleCrossRef>();

            var ruleCrossRef = (IRuleCrossRef)secondElement;
            ruleCrossRef.CompositeRuleId.RepoKey.Should().Be("javascript");
            ruleCrossRef.CompositeRuleId.RuleKey.Should().Be("S1656");

            var lastElement = result[2];
            lastElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)lastElement).Text.Should().Be(" - Implements a check on");
        }

        [TestMethod]
        public void ProcessRawText_OneCrossReference_WithoutSurroundingText_ReturnsResultWithCorrectItems()
        {
            string text = "{rule:cpp:S165}";

            var result = CustomTextMarkupParser.Parse(text).ToArray();

            result.Should().HaveCount(1);

            var firstElement = result[0];
            firstElement.Should().BeOfType<RuleCrossRef>();

            var ruleCrossRef = (IRuleCrossRef)firstElement;
            ruleCrossRef.CompositeRuleId.RepoKey.Should().Be("cpp");
            ruleCrossRef.CompositeRuleId.RuleKey.Should().Be("S165");
        }

        [TestMethod]
        public void ProcessRawText_TwoCrossReferences_WithSurroundingText_ReturnsResultWithCorrectItems()
        {
            string text = "also found {rule:cpp:S165} hello this is a test {rule:c:S1111}";

            var result = CustomTextMarkupParser.Parse(text).ToArray();

            result.Should().HaveCount(4);

            var firstElement = result[0];
            firstElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)firstElement).Text.Should().Be("also found ");

            var secondElement = result[1];
            secondElement.Should().BeOfType<RuleCrossRef>();

            var ruleCrossRef = (IRuleCrossRef)secondElement;
            ruleCrossRef.CompositeRuleId.RepoKey.Should().Be("cpp");
            ruleCrossRef.CompositeRuleId.RuleKey.Should().Be("S165");

            var thirdElement = result[2];
            thirdElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)thirdElement).Text.Should().Be(" hello this is a test ");

            var lastElement = result[3];
            lastElement.Should().BeOfType<RuleCrossRef>();

            var ruleCrossRefTwo = (IRuleCrossRef)lastElement;
            ruleCrossRefTwo.CompositeRuleId.RepoKey.Should().Be("c");
            ruleCrossRefTwo.CompositeRuleId.RuleKey.Should().Be("S1111");

        }

        [TestMethod]
        public void ProcessRawText_NoCrossReferences_ReturnsResultWithOneItem()
        {
            string text = "this is a text without any cross references";

            var result = CustomTextMarkupParser.Parse(text).ToArray();

            result.Should().HaveCount(1);

            var firstElement = result[0];
            firstElement.Should().BeOfType<SimpleText>();

            ((ISimpleText)firstElement).Text.Should().Be("this is a text without any cross references");
        }

        [TestMethod]
        public void ProcessRawText_EmptyString_ReturnsEmptyresult()
        {
            string text = "";

            var result = CustomTextMarkupParser.Parse(text);

            result.Should().HaveCount(0);
        }
    }
}
