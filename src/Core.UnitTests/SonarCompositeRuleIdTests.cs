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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class SonarCompositeRuleIdTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("S1234")]
        [DataRow("a:b:c")]
        [DataRow(":leadingcolon")]
        [DataRow("trailingcolon:")]
        public void TryParse_Invalid_ReturnsNull(string errorCode)
        {
            SonarCompositeRuleId.TryParse(errorCode, out var result).Should().BeFalse();
            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow("X:x", "X", "x")]
        [DataRow("typescript:S1234", "typescript", "S1234")]
        public void TryParse_Valid_ReturnExpecteed(string errorCode, string expectedRepo, string expectedRule)
        {
            SonarCompositeRuleId.TryParse(errorCode, out var result).Should().BeTrue();
            result.ErrorListErrorCode.Should().Be(errorCode);
            result.RepoKey.Should().Be(expectedRepo);
            result.RuleKey.Should().Be(expectedRule);
        }

        [TestMethod]
        public void ToString_ReturnsExpected()
        {
            const string input = "xxx:YYY";

            SonarCompositeRuleId.TryParse(input, out var output);
            output.ToString().Should().Be(input);
        }
    }
}
