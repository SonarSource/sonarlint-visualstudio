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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class RuleHelpLinkProviderTests
    {
        [TestMethod]
        [DataRow("javascript:123", "https://rules.sonarsource.com/javascript/RSPEC-123")]
        [DataRow("javascript:SOMETHING", "https://rules.sonarsource.com/javascript/RSPEC-SOMETHING")]
        [DataRow("c:456", "https://rules.sonarsource.com/c/RSPEC-456")]
        [DataRow("cpp:789", "https://rules.sonarsource.com/cpp/RSPEC-789")]
        [DataRow("php:101112", "https://rules.sonarsource.com/php/RSPEC-101112")]
        public void GetHelpLink(string ruleKey, string expectedLink)
        {
            var helpLink = new RuleHelpLinkProvider().GetHelpLink(ruleKey);

            helpLink.Should().Be(expectedLink);
        }
    }
}
