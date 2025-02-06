/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using static SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class LanguageExtensionsTests
{
    [TestMethod]
    [DynamicData(nameof(CoreToSlCore))]
    public void ConvertToCoreLanguage_KnownLanguage_ConvertsAsExpected(VisualStudio.Core.Language coreLanguage, Language slCoreLanguage) =>
        slCoreLanguage.ConvertToCoreLanguage().Should().BeSameAs(coreLanguage);

    [TestMethod]
    public void ConvertToCoreLanguage_UnknownLanguage_ReturnsUnknown()
    {
        Language.ABAP.ConvertToCoreLanguage().Should().BeSameAs(Unknown);
        Language.JAVA.ConvertToCoreLanguage().Should().BeSameAs(Unknown);
    }

    [TestMethod]
    [DynamicData(nameof(CoreToSlCore))]
    public void ConvertToSlCoreLanguage_KnownLanguage_ConvertsAsExpected(VisualStudio.Core.Language coreLanguage, Language slCoreLanguage) =>
        coreLanguage.ConvertToSlCoreLanguage().Should().Be(slCoreLanguage);

    [TestMethod]
    public void ConvertToSlCoreLanguage_UnknownLanguage_Throws()
    {
        var act = () => Unknown.ConvertToSlCoreLanguage();

        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
    }

    public static IEnumerable<object[]> CoreToSlCore =>
    [
        [CSharp, Language.CS], [VBNET, Language.VBNET], [C, Language.C], [Cpp, Language.CPP],
        [Css, Language.CSS], [Html, Language.HTML], [Js, Language.JS], [Secrets, Language.SECRETS],
        [Ts, Language.TS], [TSql, Language.TSQL]
    ];
}
