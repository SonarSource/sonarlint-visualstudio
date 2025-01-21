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

using SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.VcxProject;

[TestClass]
public class LanguageFlagsProviderTests
{
    [TestMethod]
    public void GetLanguageConfiguration_DefaultCompileAs_NoContentType_ReturnsCpp() =>
        new LanguageFlagsProvider("unknown")
            .GetLanguageConfiguration("Default", "stdc23", "stdcpp20")
            .Should().Be(("/TP", "/std:c++20"));

    [TestMethod]
    public void GetLanguageConfiguration_DefaultCompileAs_ContentTypeCppCode_ReturnsCpp() =>
        new LanguageFlagsProvider("CppCode")
            .GetLanguageConfiguration("Default", "stdc23", "stdcpp20")
            .Should().Be(("/TP", "/std:c++20"));

    [TestMethod]
    public void GetLanguageConfiguration_DefaultCompileAs_ContentTypeCppHeader_ReturnsCpp() =>
        new LanguageFlagsProvider("CppHeader")
            .GetLanguageConfiguration("Default", "stdc23", "stdcpp20")
            .Should().Be(("/TP", "/std:c++20"));

    [TestMethod]
    public void GetLanguageConfiguration_DefaultCompileAs_ContentTypeCCode_ReturnsC() =>
        new LanguageFlagsProvider("CCode")
            .GetLanguageConfiguration("Default", "stdc23", "stdcpp20")
            .Should().Be(("/TC", "/std:c23"));

    [DataTestMethod]
    [DataRow("CppCode")]
    [DataRow("CCode")]
    [DataRow("Default")]
    public void GetLanguageConfiguration_CompileAsC_AnyContentType_ReturnsC(string contentType) =>
        new LanguageFlagsProvider(contentType)
            .GetLanguageConfiguration("CompileAsC", "stdc23", "stdcpp20")
            .Should().Be(("/TC", "/std:c23"));

    [DataTestMethod]
    [DataRow("CppCode")]
    [DataRow("CCode")]
    [DataRow("Default")]
    public void GetLanguageConfiguration_CompileAsCpp_AnyContentType_ReturnsCpp(string contentType) =>
        new LanguageFlagsProvider(contentType)
            .GetLanguageConfiguration("CompileAsCpp", "stdc23", "stdcpp20")
            .Should().Be(("/TP", "/std:c++20"));
}
