﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    internal interface IFileConfig
    {
        string AbsoluteFilePath { get; }
        string AbsoluteProjectPath { get; }
        string PlatformName { get; }
        string PlatformToolset { get; }
        string IncludeDirectories { get; }
        string AdditionalIncludeDirectories { get; }
        string IgnoreStandardIncludePath { get; }
        string UndefineAllPreprocessorDefinitions { get; }
        string PreprocessorDefinitions { get; }
        string UndefinePreprocessorDefinitions { get; }
        string ForcedIncludeFiles { get; }
        string PrecompiledHeader { get; }
        string PrecompiledHeaderFile { get; }
        string ItemType { get; }
        string CompileAs { get; }
        string CompileAsManaged { get; }
        string CompileAsWinRT { get; }
        string DisableLanguageExtensions { get; }
        string TreatWChar_tAsBuiltInType { get; }
        string ForceConformanceInForLoopScope { get; }
        string OpenMPSupport { get; }
        string RuntimeLibrary { get; }
        string ExceptionHandling { get; }
        string EnableEnhancedInstructionSet { get; }
        string RuntimeTypeInfo { get; }
        string BasicRuntimeChecks { get; }
        string OmitDefaultLibName { get; }
        string AdditionalOptions { get; }
        string LanguageStandard { get; }
        string CompilerVersion { get; }
    }
}
