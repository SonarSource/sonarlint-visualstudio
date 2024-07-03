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

using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Common.Helpers;

[TestClass]
public class LanguageExtensionsTests
{
    [TestMethod]
    public void VerifyConversionToCoreLanguage()
    {
        VerifyConversionToCoreLanguage(Language.C, VisualStudio.Core.Language.C);
        VerifyConversionToCoreLanguage(Language.CPP, VisualStudio.Core.Language.Cpp);
        VerifyConversionToCoreLanguage(Language.CS, VisualStudio.Core.Language.CSharp);
        VerifyConversionToCoreLanguage(Language.CSS, VisualStudio.Core.Language.Css);
        VerifyConversionToCoreLanguage(Language.JS, VisualStudio.Core.Language.Js);
        VerifyConversionToCoreLanguage(Language.SECRETS, VisualStudio.Core.Language.Secrets);
        VerifyConversionToCoreLanguage(Language.TS, VisualStudio.Core.Language.Ts);
        VerifyConversionToCoreLanguage(Language.VBNET, VisualStudio.Core.Language.VBNET);
        VerifyConversionToCoreLanguage(Language.ABAP, VisualStudio.Core.Language.Unknown);
        VerifyConversionToCoreLanguage(Language.JAVA, VisualStudio.Core.Language.Unknown);
    }

    [DataTestMethod]
    [DataRow(Language.C, "cpp")]
    [DataRow(Language.CPP, "cpp")]
    [DataRow(Language.JS, "javascript")]
    [DataRow(Language.TS, "javascript")]
    [DataRow(Language.CSS, "javascript")]
    [DataRow(Language.CS, "csharp")]
    [DataRow(Language.VBNET, "vbnet")]
    [DataRow(Language.SECRETS, "text")]
    [DataRow(Language.ABAP, null)]
    [DataRow(Language.JAVA, null)]
    public void VerifyPluginKeys(Language language, string expectedPluginKey)
    {
        language.GetPluginKey().Should().BeEquivalentTo(expectedPluginKey);
    }

    private static void VerifyConversionToCoreLanguage(Language language, VisualStudio.Core.Language coreLanguage)
    {
        language.ConvertToCoreLanguage().Should().BeSameAs(coreLanguage);
    }
}
