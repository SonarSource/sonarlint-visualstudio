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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Education.UnitTests
{
    [TestClass]
    public class CrossReferenceRuleHelperTests
    {
        [TestMethod]
        public void EncodeToUri_EncodesCompositeKeyCorrectly()
        {
            var compositeKey = new SonarCompositeRuleId("cpp", "S1234");

            var uri = CrossReferenceRuleHelper.EncodeToUri(compositeKey);

            uri.AbsoluteUri.Should().Be("sonarlintrulecrossref://cpp/S1234");
        }
    }
}
