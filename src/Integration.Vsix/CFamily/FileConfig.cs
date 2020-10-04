/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.VCProjectEngine;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    partial class CFamilyHelper
    {
        internal class FileConfig
        {
            public static FileConfig TryGet(ILogger logger, ProjectItem dteProjectItem, string absoluteFilePath)
            {
                var vcProject = dteProjectItem.ContainingProject.Object as VCProject;
                var file = dteProjectItem.Object as VCFile;
                if (vcProject == null || file == null)
                {
                    return null;
                }
                var vcConfig = vcProject.ActiveConfiguration;
                var projectKind = vcConfig.ConfigurationType;
                // Unknown projects represent all the unspported project type like makefile projects
                if (projectKind == ConfigurationTypes.typeUnknown)
                {
                    logger.WriteLine(@"Project's ""Configuration type"" is not supported.");
                    return null;
                }
                // "ClCompile" for source files and "ClCompile" for header files
                // Don't rely on file extension to indentify header files since they can have any extension
                var itemType = file.ItemType;
                if (itemType != "ClCompile" && itemType != "ClInclude")
                {
                    logger.WriteLine($"File's \"Item type\" is not supported. File: '{absoluteFilePath}'");
                    return null;
                }
                var platformName = ((VCPlatform)vcConfig.Platform).Name; // "Win32" or "x64"
                var vcFileConfig = file.GetFileConfigurationForProjectConfiguration(vcConfig);
                IVCRulePropertyStorage vcFileSettings;
                if (itemType == "ClCompile")
                {
                    // We don't support custom build tools. VCCLCompilerTool is needed for all the necessary compilation options to be present
                    if (!(vcFileConfig.Tool is VCCLCompilerTool))
                    {
                        logger.WriteLine($"Custom built files are not supported. File: '{absoluteFilePath}'");
                        return null;
                    }
                    // The Tool property is typed as Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool, and
                    // we could use it to fetch most of the properties we need, rather than fetching them via the
                    // IVCRulePropertyStorage interface. However, not all of the properties are exposed directly by
                    // VCCLCompilerTool (e.g. LanguageStandard). Also, quite a few of the Tool properties are exposed
                    // as enums, so we'd need to change our code to handle them.
                    vcFileSettings = vcFileConfig.Tool as IVCRulePropertyStorage;
                }
                else
                {
                    // header file load file setting from the project
                    var tools = vcConfig.Tools as IVCCollection;
                    var toolItem = tools.Item("VCCLCompilerTool");
                    if (toolItem == null)
                    {
                        logger.WriteLine($"Custom built files are not supported. File: '{absoluteFilePath}'");
                        return null;
                    }
                    vcFileSettings = toolItem as IVCRulePropertyStorage;
                }

                // Fetch properties that can't be set at file level from the configuration object
                var includeDirs = vcConfig.GetEvaluatedPropertyValue("IncludePath");
                var platformToolset = vcConfig.GetEvaluatedPropertyValue("PlatformToolset");
                var vcToolsVersion = vcConfig.GetEvaluatedPropertyValue("VCToolsVersion");
                var compilerVersion = GetCompilerVersion(platformToolset, vcToolsVersion);

                // Fetch properties that can be set differently for header files
                string compileAs = vcFileSettings.GetEvaluatedPropertyValue("CompileAs");
                string forcedIncludeFiles = vcFileSettings.GetEvaluatedPropertyValue("ForcedIncludeFiles");
                string precompiledHeader = vcFileSettings.GetEvaluatedPropertyValue("PrecompiledHeader");
                string precompiledHeaderFile = vcFileSettings.GetEvaluatedPropertyValue("PrecompiledHeaderFile");
                if (itemType == "ClInclude")
                {
                    string compiledAs = vcFileSettings.GetEvaluatedPropertyValue("CompileAs");
                    // If the project language is not specified. Headers are compiled as CPP
                    compileAs = (compiledAs == "Default") ? "CompileAsCpp" : compiledAs;
                    // Use PCH in header files if project PCH is configured and not enforced through force include
                    // In normal code, it should be self to assume this since PCH should be used on every CPP file
                    if (string.IsNullOrEmpty(forcedIncludeFiles) && precompiledHeader == "Use")
                    {
                        forcedIncludeFiles = precompiledHeaderFile;
                    }
                }
                return new FileConfig
                {
                    AbsoluteFilePath = absoluteFilePath,
                    AbsoluteProjectPath = vcProject.ProjectFile,

                    PlatformName = platformName,
                    PlatformToolset = platformToolset,

                    IncludeDirectories = includeDirs,
                    AdditionalIncludeDirectories = vcFileSettings.GetEvaluatedPropertyValue("AdditionalIncludeDirectories"),
                    IgnoreStandardIncludePath = vcFileSettings.GetEvaluatedPropertyValue("IgnoreStandardIncludePath"),

                    UndefineAllPreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("UndefineAllPreprocessorDefinitions"),
                    PreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("PreprocessorDefinitions"),
                    UndefinePreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("UndefinePreprocessorDefinitions"),

                    ForcedIncludeFiles = forcedIncludeFiles,
                    PrecompiledHeader = precompiledHeader,
                    PrecompiledHeaderFile = precompiledHeaderFile,

                    CompileAs = compileAs,
                    CompileAsManaged = vcFileSettings.GetEvaluatedPropertyValue("CompileAsManaged"),
                    CompileAsWinRT = vcFileSettings.GetEvaluatedPropertyValue("CompileAsWinRT"),
                    DisableLanguageExtensions = vcFileSettings.GetEvaluatedPropertyValue("DisableLanguageExtensions"),
                    TreatWChar_tAsBuiltInType = vcFileSettings.GetEvaluatedPropertyValue("TreatWChar_tAsBuiltInType"),
                    ForceConformanceInForLoopScope = vcFileSettings.GetEvaluatedPropertyValue("ForceConformanceInForLoopScope"),
                    OpenMPSupport = vcFileSettings.GetEvaluatedPropertyValue("OpenMPSupport"),

                    RuntimeLibrary = vcFileSettings.GetEvaluatedPropertyValue("RuntimeLibrary"),
                    ExceptionHandling = vcFileSettings.GetEvaluatedPropertyValue("ExceptionHandling"),
                    EnableEnhancedInstructionSet = vcFileSettings.GetEvaluatedPropertyValue("EnableEnhancedInstructionSet"),
                    OmitDefaultLibName = vcFileSettings.GetEvaluatedPropertyValue("OmitDefaultLibName"),
                    RuntimeTypeInfo = vcFileSettings.GetEvaluatedPropertyValue("RuntimeTypeInfo"),
                    BasicRuntimeChecks = vcFileSettings.GetEvaluatedPropertyValue("BasicRuntimeChecks"),
                    LanguageStandard = GetPotentiallyUnsupportedPropertyValue(vcFileSettings, "LanguageStandard", null),
                    AdditionalOptions = vcFileSettings.GetEvaluatedPropertyValue("AdditionalOptions"),
                    CompilerVersion = compilerVersion,
                };
            }

            /// <summary>
            /// Returns the value of a property that might not be supported by the current version of the compiler.
            /// If the property is not supported the default value is returned.
            /// </summary>
            /// <remarks>e.g. LanguageStandard is not supported by VS2015 so attempting to fetch the property will throw.
            /// We don't want the analysis to fail in that case so we'll catch the exception and return the default.</remarks>
            internal /* for testing */ static string GetPotentiallyUnsupportedPropertyValue(IVCRulePropertyStorage settings, string propertyName, string defaultValue)
            {
                string result = null;
                try
                {
                    result = settings.GetEvaluatedPropertyValue(propertyName);
                }
                catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
                {
                    // Property was not found
                    result = defaultValue;
                }
                return result;
            }

            #region Properties

            public string AbsoluteFilePath { get; set; }

            public string AbsoluteProjectPath { get; set; }

            public string PlatformName { get; set; }

            public string PlatformToolset { get; set; }

            public string IncludeDirectories { get; set; }

            public string AdditionalIncludeDirectories { get; set; }

            public string IgnoreStandardIncludePath { get; set; }

            public string UndefineAllPreprocessorDefinitions { get; set; }

            public string PreprocessorDefinitions { get; set; }

            public string UndefinePreprocessorDefinitions { get; set; }

            public string ForcedIncludeFiles { get; set; }

            public string PrecompiledHeader { get; set; }

            public string PrecompiledHeaderFile { get; set; }

            public string CompileAs { get; set; }

            public string CompileAsManaged { get; set; }

            public string CompileAsWinRT { get; set; }

            public string DisableLanguageExtensions { get; set; }

            public string TreatWChar_tAsBuiltInType { get; set; }

            public string ForceConformanceInForLoopScope { get; set; }

            public string OpenMPSupport { get; set; }

            public string RuntimeLibrary { get; set; }

            public string ExceptionHandling { get; set; }

            public string EnableEnhancedInstructionSet { get; set; }

            public string RuntimeTypeInfo { get; set; }

            public string BasicRuntimeChecks { get; set; }

            public string OmitDefaultLibName { get; set; }

            public string AdditionalOptions { get; set; }

            public string LanguageStandard { get; set; }

            public string CompilerVersion { get; set; }

            #endregion

            internal /* for testing */ static string GetCompilerVersion(string platformToolset, string vcToolsVersion)
            {
                switch (platformToolset)
                {
                    default:
                        throw new ArgumentException($"Unsupported PlatformToolset: {platformToolset}", nameof(platformToolset));
                    case "": // Bug https://github.com/SonarSource/sonarlint-visualstudio/issues/804
                        throw new ArgumentException(Strings.Daemon_PlatformToolsetNotSpecified);
                    // VCToolsVersion is only avaialble from VS2017 onward
                    // Before VS2017 the platform was enough to deduce the compiler version
                    case "v142":
                    case "v141":
                    case "v141_xp":
                        int index = vcToolsVersion.IndexOf('.');
                        if (index != -1)
                        {
                            return "19" + vcToolsVersion.Substring(index);
                        }
                        else
                        {
                            throw new ArgumentException($"Unsupported VCToolsVersion: {vcToolsVersion}", nameof(vcToolsVersion));
                        }
                    case "v140":
                    case "v140_xp":
                        return "19.00.00";
                    case "v120":
                    case "v120_xp":
                        return "18.00.00";
                    case "v110":
                    case "v110_xp":
                        return "17.00.00";
                    case "v100":
                        return "16.00.00";
                    case "v90":
                        return "15.00.00";
                }
            }

        }
    }
}
