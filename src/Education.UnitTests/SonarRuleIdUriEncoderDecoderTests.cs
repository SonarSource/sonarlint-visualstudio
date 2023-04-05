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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class SonarRuleIdUriEncoderDecoderTests
    {
        [TestMethod]
        public void EncodeToUri_EncodesCompositeKeyCorrectly()
        {
            var compositeKey = new SonarCompositeRuleId("cpp", "S1234");

            var uri = SonarRuleIdUriEncoderDecoder.EncodeToUri(compositeKey);

            uri.AbsoluteUri.Should().Be("sonarlintrulecrossref://cpp/S1234");
        }

        [TestMethod]
        public void TryDecodeToCompositeRuleId_IncomingUriCanBeDecoded_ReturnsTrue()
        {
            var decodableUri = new Uri($"sonarlintrulecrossref://cpp/S1234");

            var result = SonarRuleIdUriEncoderDecoder.TryDecodeToCompositeRuleId(decodableUri, out SonarCompositeRuleId compositeRuleId);

            result.Should().BeTrue();
            compositeRuleId.RepoKey.Should().Be("cpp");
            compositeRuleId.RuleKey.Should().Be("S1234");
        }

        [TestMethod]
        [DataRow("https://www.IamAUri.com")]
        [DataRow("wrong://format")]
        public void TryDecodeToCompositeRuleId_IncomingUriCannotBeDecoded_ReturnsFalse(string uriText)
        {
            var regularUri = new Uri(uriText);

            var result = SonarRuleIdUriEncoderDecoder.TryDecodeToCompositeRuleId(regularUri, out SonarCompositeRuleId compositeRuleId);

            result.Should().BeFalse();
            compositeRuleId.Should().BeNull();
        }
    }
}