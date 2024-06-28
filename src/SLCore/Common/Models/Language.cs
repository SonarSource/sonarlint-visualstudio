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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using CoreLanguage = SonarLint.VisualStudio.Core.Language;


namespace SonarLint.VisualStudio.SLCore.Common.Models;

/// <summary>
/// SLCore Language. Taken from org.sonarsource.sonarlint.core.rpc.protocol.common
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum Language 
{
    ABAP,
    APEX,
    AZURERESOURCEMANAGER,
    C,
    CLOUDFORMATION,
    COBOL,
    CPP,
    CS,
    CSS,
    DOCKER,
    GO,
    HTML,
    IPYTHON,
    JAVA,
    JS,
    JSON,
    JSP,
    KOTLIN,
    KUBERNETES,
    OBJC,
    PHP,
    PLI,
    PLSQL,
    PYTHON,
    RPG,
    RUBY,
    SCALA,
    SECRETS,
    SWIFT,
    TERRAFORM,
    TS,
    TSQL,
    VBNET,
    XML,
    YAML
}

public static class LanguageExtensions
{
    public static CoreLanguage ConvertToCoreLanguage(this Language language)
    {
        return language switch
        {

            Language.C => CoreLanguage.C,
            Language.CPP => CoreLanguage.Cpp,
            Language.CS => CoreLanguage.CSharp,
            Language.CSS => CoreLanguage.Css,
            Language.JS => CoreLanguage.Js,
            Language.SECRETS => CoreLanguage.Secrets,
            Language.TS => CoreLanguage.Ts,
            Language.VBNET => CoreLanguage.VBNET,
            _ => CoreLanguage.Unknown
        };
    }
}
