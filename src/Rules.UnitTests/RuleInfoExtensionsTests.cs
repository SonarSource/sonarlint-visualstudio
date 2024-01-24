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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Rules.UnitTests;

[TestClass]
public class RuleInfoExtensionsTests
{
    [TestMethod]
    public void IsRich_DescriptionSectionsNull_False()
    {
        var testSubject = new Mock<IRuleInfo>();
        testSubject.SetupGet(x => x.DescriptionSections).Returns((IReadOnlyList<IDescriptionSection>)null);

        testSubject.Object.IsRichRuleDescription().Should().BeFalse();
    }

    [TestMethod]
    public void IsRich_DescriptionSectionsEmpty_False()
    {
        var testSubject = new Mock<IRuleInfo>();
        testSubject.SetupGet(x => x.DescriptionSections).Returns(new List<IDescriptionSection>());

        testSubject.Object.IsRichRuleDescription().Should().BeFalse();
    }

    [TestMethod]
    public void IsRich_DescriptionSectionsHasOneMember_False()
    {
        var testSubject = new Mock<IRuleInfo>();
        testSubject.SetupGet(x => x.DescriptionSections).Returns(new List<IDescriptionSection> { new DescriptionSection(null, null) });

        testSubject.Object.IsRichRuleDescription().Should().BeFalse();
    }

    [TestMethod]
    public void IsRich_DescriptionSectionsHasMoreThanOneMember_True()
    {
        var testSubject = new Mock<IRuleInfo>();
        testSubject.SetupGet(x => x.DescriptionSections).Returns(new List<IDescriptionSection> { new DescriptionSection(null, null), new DescriptionSection(null, null) });

        testSubject.Object.IsRichRuleDescription().Should().BeTrue();
    }
}
