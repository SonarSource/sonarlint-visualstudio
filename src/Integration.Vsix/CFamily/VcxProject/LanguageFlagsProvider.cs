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

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject;

internal interface ILanguageFlagsProvider
{
    (string compileAsFlag, string languageStandardFlag) GetLanguageConfiguration(string compileAs, string cStandard, string cppStandard);
}

internal class LanguageFlagsProvider : ILanguageFlagsProvider
{
    private readonly string vcFileContentType;
    private const string CompileAsCFlag = "/TC";
    private const string CompileAsCppFlag = "/TP";
    private const string ContentTypeCCode = "CCode";

    public LanguageFlagsProvider(string vcFileContentType)
    {
        this.vcFileContentType = vcFileContentType;
    }

    public (string compileAsFlag, string languageStandardFlag) GetLanguageConfiguration(string compileAs, string cStandard, string cppStandard)
    {
        var compileAsFlag = CompileAsValueConverter.GetFlagValue(compileAs);

        // if a C/CPP language file has no file-specific override for the CompileAs option in vsxproj, the property storage returns a non-lanugage-specific "Default" value.
        return compileAsFlag == CompileAsCFlag || (compileAsFlag != CompileAsCppFlag && vcFileContentType == ContentTypeCCode)
            ? (CompileAsCFlag, LanguageStandardConverter.GetCStandardFlagValue(cStandard))
            : (CompileAsCppFlag, LanguageStandardConverter.GetCppStandardFlagValue(cppStandard));
    }
}
