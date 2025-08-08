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
public class LanguageStandardConverterTests
{

    [TestMethod]
    [DataRow("", "")]
    [DataRow("Default", "")]
    [DataRow(null, "")]
    [DataRow("stdcpplatest", "/std:c++latest")]
    [DataRow("stdcpp23", "/std:c++23")]
    [DataRow("stdcpp23preview", "/std:c++23preview")]
    [DataRow("stdcpp20", "/std:c++20")]
    [DataRow("stdcpp17", "/std:c++17")]
    [DataRow("stdcpp14", "/std:c++14")]
    public void GetCppStandardFlagValue(string input, string output) =>
        LanguageStandardConverter.GetCppStandardFlagValue(input).Should().Be(output);

    [TestMethod]
    public void GetCppStandardFlagValue_UnsupportedValue_Throws()
    {
        var act = () => LanguageStandardConverter.GetCppStandardFlagValue("INVALID");

        act.Should().Throw<ArgumentException>().WithMessage("Unsupported LanguageStandard: INVALID");
    }

    [TestMethod]
    [DataRow("", "")]
    [DataRow("Default", "")]
    [DataRow(null, "")]
    [DataRow("stdclatest", "/std:clatest")]
    [DataRow("stdc23", "/std:c23")]
    [DataRow("stdc17", "/std:c17")]
    [DataRow("stdc11", "/std:c11")]
    public void GetCStandardFlagValue(string input, string output) =>
        LanguageStandardConverter.GetCStandardFlagValue(input).Should().Be(output);

    [TestMethod]
    public void GetCStandardFlagValue_UnsupportedValue_Throws()
    {
        var act = () => LanguageStandardConverter.GetCStandardFlagValue("INVALID");

        act.Should().Throw<ArgumentException>().WithMessage("Unsupported LanguageStandard_C: INVALID");
    }
}
