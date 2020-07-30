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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
            private static readonly Regex AdditionalOptionsSplitPattern = new Regex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            public static FileConfig TryGet(ProjectItem dteProjectItem, string absoluteFilePath)
            {
                var vcProject = dteProjectItem.ContainingProject.Object as VCProject;
                var file = dteProjectItem.Object as VCFile;
                if (vcProject == null || file == null)
                {
                    return null;
                }

                var vcConfig = vcProject.ActiveConfiguration;
                var platformName = ((VCPlatform)vcConfig.Platform).Name; // "Win32" or "x64"
                var vcFileConfig = file.GetFileConfigurationForProjectConfiguration(vcConfig);

                // The Tool property is typed as Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool, and
                // we could use it to fetch most of the properties we need, rather than fetching them via the
                // IVCRulePropertyStorage interface. However, not all of the properties are exposed directly by
                // VCCLCompilerTool (e.g. LanguageStandard). Also, quite a few of the Tool properties are exposed
                // as enums, so we'd need to change our code to handle them.
                var vcFileSettings = vcFileConfig.Tool as IVCRulePropertyStorage;

                // Fetch properties that can't be set at file level from the configuration object
                var includeDirs = vcConfig.GetEvaluatedPropertyValue("IncludePath");
                var platformToolset = vcConfig.GetEvaluatedPropertyValue("PlatformToolset");
                var vcToolsVersion = vcConfig.GetEvaluatedPropertyValue("VCToolsVersion");
                var compilerVersion = GetCompilerVersion(platformToolset, vcToolsVersion);

                return new FileConfig
                {
                    AbsoluteFilePath = absoluteFilePath,

                    PlatformName = platformName,
                    PlatformToolset = platformToolset,

                    IncludeDirectories = includeDirs,
                    AdditionalIncludeDirectories = vcFileSettings.GetEvaluatedPropertyValue("AdditionalIncludeDirectories"),
                    IgnoreStandardIncludePath = vcFileSettings.GetEvaluatedPropertyValue("IgnoreStandardIncludePath"),

                    UndefineAllPreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("UndefineAllPreprocessorDefinitions"),
                    PreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("PreprocessorDefinitions"),
                    UndefinePreprocessorDefinitions = vcFileSettings.GetEvaluatedPropertyValue("UndefinePreprocessorDefinitions"),

                    ForcedIncludeFiles = vcFileSettings.GetEvaluatedPropertyValue("ForcedIncludeFiles"),
                    PrecompiledHeader = vcFileSettings.GetEvaluatedPropertyValue("PrecompiledHeader"),
                    PrecompiledHeaderFile = vcFileSettings.GetEvaluatedPropertyValue("PrecompiledHeaderFile"),

                    CompileAs = vcFileSettings.GetEvaluatedPropertyValue("CompileAs"),
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

            public string AbsoluteFilePath { get; set; }

            #region Properties

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

            public Capture[] ToCaptures(string path, out string cfamilyLanguage)
            {
                var p = new Capture()
                {
                    Executable = "cl.exe",
                    Cwd = Path.GetDirectoryName(AbsoluteFilePath),
                    CompilerVersion= CompilerVersion,
                    X64= IsPlatformX64(PlatformName),
                    StdOut = "",
                };

                var c = new Capture()
                {
                    Executable = p.Executable,
                    Cwd = p.Cwd,
                    Env = new List<string>(),
                    Cmd = new List<string>(),
                };
                c.Env.Add("INCLUDE=" + IncludeDirectories);
                c.Cmd.Add(c.Executable);
                Add(c.Cmd, "true".Equals(IgnoreStandardIncludePath) ? "/X" : "");
                AddRange(c.Cmd, "/I", AdditionalIncludeDirectories.Split(';'));
                AddRange(c.Cmd, "/FI", ForcedIncludeFiles.Split(';'));
                Add(c.Cmd, ConvertPrecompiledHeader(PrecompiledHeader, PrecompiledHeaderFile));

                Add(c.Cmd, "true".Equals(UndefineAllPreprocessorDefinitions) ? "/u" : "");
                AddRange(c.Cmd, "/D", PreprocessorDefinitions.Split(';'));
                AddRange(c.Cmd, "/U", UndefinePreprocessorDefinitions.Split(';'));

                Add(c.Cmd, ConvertCompileAsAndGetLanguage(CompileAs, path, out cfamilyLanguage));
                Add(c.Cmd, ConvertCompileAsManaged(CompileAsManaged));
                Add(c.Cmd, "true".Equals(CompileAsWinRT) ? "/ZW" : "");
                Add(c.Cmd, "true".Equals(DisableLanguageExtensions) ? "/Za" : ""); // defines macro "__STDC__" when compiling C
                Add(c.Cmd, "false".Equals(TreatWChar_tAsBuiltInType) ? "/Zc:wchar_t-" : ""); // undefines macros "_NATIVE_WCHAR_T_DEFINED" and "_WCHAR_T_DEFINED"
                Add(c.Cmd, "false".Equals(ForceConformanceInForLoopScope) ? "/Zc:forScope-" : "");
                Add(c.Cmd, "true".Equals(OpenMPSupport) ? "/openmp" : "");

                Add(c.Cmd, ConvertRuntimeLibrary(RuntimeLibrary));
                Add(c.Cmd, ConvertExceptionHandling(ExceptionHandling));
                Add(c.Cmd, ConvertEnableEnhancedInstructionSet(EnableEnhancedInstructionSet));
                Add(c.Cmd, "true".Equals(OmitDefaultLibName) ? "/Zl" : ""); // defines macro "_VC_NODEFAULTLIB"
                Add(c.Cmd, "false".Equals(RuntimeTypeInfo) ? "/GR-" : ""); // undefines macro "_CPPRTTI"
                Add(c.Cmd, ConvertBasicRuntimeChecks(BasicRuntimeChecks));
                Add(c.Cmd, ConvertLanguageStandard(LanguageStandard));
                AddRange(c.Cmd, GetAdditionalOptions(AdditionalOptions));

                c.Cmd.Add(AbsoluteFilePath);

                return new Capture[] { p, c };
            }

            public Request ToRequest(string path)
            {
                Capture[] c = ToCaptures(path, out string cfamilyLanguage);
                var request = MsvcDriver.ToRequest(c);
                request.CFamilyLanguage = cfamilyLanguage;
                return request;
            }

            internal /* for testing */ static bool IsPlatformX64(string platformName)
            {
                switch (platformName)
                {
                    default:
                        throw new ArgumentException($"Unsupported PlatformName: {platformName}", nameof(platformName));
                    case "Win32":
                        return false;
                    case "x64":
                        return true;
                }
            }


            internal /* for testing */ static string[] GetAdditionalOptions(string options)
            {
                var additionalOptions = AdditionalOptionsSplitPattern.Split(options);

                return additionalOptions;
            }

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

            internal /* for testing */ static string ConvertCompileAsAndGetLanguage(string value, string path, out string cfamilyLanguage)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported CompileAs: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                    case "Default":
                        // Compile files with extensions ".cpp", ".cxx" and ".cc" as Cpp and files with extension ".c" as C
                        if (path.ToLowerInvariant().EndsWith(".cpp") || path.ToLowerInvariant().EndsWith(".cxx") || path.ToLowerInvariant().EndsWith(".cc"))
                        {
                            cfamilyLanguage = CPP_LANGUAGE_KEY;
                        }
                        else if (path.ToLowerInvariant().EndsWith(".c"))
                        {
                            cfamilyLanguage = C_LANGUAGE_KEY;
                        }
                        else
                        {
                            cfamilyLanguage = null;
                        }
                        return "";
                    case "CompileAsC":
                        cfamilyLanguage = "c";
                        return "/TC";
                    case "CompileAsCpp":
                        cfamilyLanguage = CPP_LANGUAGE_KEY;
                        return "/TP";
                }
            }

            internal /* for testing */ static string ConvertCompileAsManaged(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported CompileAsManaged: {value}", nameof(value));
                    case "":
                    case "false":
                        return "";
                    case "true":
                        return "/clr";
                    case "Pure":
                        return "/clr:pure";
                    case "Safe":
                        return "/clr:safe";
                }
            }

            internal /* for testing */ static string ConvertRuntimeLibrary(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported RuntimeLibrary: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                        return "";
                    case "MultiThreaded":
                        return "/MT"; // defines macro "_MT"
                    case "MultiThreadedDebug":
                        return "/MTd"; // defines macros "_MT" and "_DEBUG"
                    case "MultiThreadedDLL":
                    case "MultiThreadedDll":
                        return "/MD"; // defines macros "_MT" and "_DLL"
                    case "MultiThreadedDebugDLL":
                    case "MultiThreadedDebugDll":
                        return "/MDd"; // defines macros "_MT", "_DLL" and "_DEBUG"
                }
            }

            internal /* for testing */ static string ConvertEnableEnhancedInstructionSet(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported EnableEnhancedInstructionSet: {value}", nameof(value));
                    case "NotSet":
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                        return "";
                    case "AdvancedVectorExtensions":
                        return "/arch:AVX";
                    case "AdvancedVectorExtensions2":
                        return "/arch:AVX2";
                    case "StreamingSIMDExtensions":
                        return "/arch:SSE";
                    case "StreamingSIMDExtensions2":
                        return "/arch:SSE2";
                    case "NoExtensions":
                        return "/arch:IA32";
                }
            }

            internal /* for testing */ static string ConvertExceptionHandling(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported ExceptionHandling: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                    case "false":
                        return "";
                    // all options below define macro "_CPPUNWIND":
                    case "Async":
                        return "/EHa";
                    case "Sync":
                        return "/EHsc";
                    case "SyncCThrow":
                        return "/EHs";
                }
            }

            internal /* for testing */ static string ConvertBasicRuntimeChecks(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported BasicRuntimeChecks: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                    case "Default":
                        return "";
                    // all options below define macro "__MSVC_RUNTIME_CHECKS":
                    case "StackFrameRuntimeCheck":
                        return "/RTCs";
                    case "UninitializedLocalUsageCheck":
                        return "/RTCu";
                    case "EnableFastChecks":
                        return "/RTC1";
                }
            }

            internal /* for testing */ static string ConvertPrecompiledHeader(string value, string header)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported PrecompiledHeader: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                    case "NotUsing": // https://github.com/SonarSource/sonarlint-visualstudio/issues/992
                        return "";
                    case "Create":
                        return "/Yc" + header;
                    case "Use":
                        return "/Yu" + header;
                }
            }

            internal /* for testing */ static string ConvertLanguageStandard(string value)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported LanguageStandard: {value}", nameof(value));
                    case null:
                    case "":
                    case "Default":
                        return "";
                    case "stdcpplatest":
                        return "/std:c++latest";
                    case "stdcpp17":
                        return "/std:c++17";
                    case "stdcpp14":
                        return "/std:c++14";
                }
            }
        }
    }
}
