/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Reflection;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    partial class CFamilyHelper
    {
        internal class FileConfig
        {
            public static FileConfig TryGet(ProjectItem projectItem, string absoluteFilePath)
            {
                string configurationName = projectItem.ConfigurationManager.ActiveConfiguration.ConfigurationName; // "Debug" or "Release"
                string platformName = projectItem.ConfigurationManager.ActiveConfiguration.PlatformName; // "Win32" or "x64"
                string pattern = configurationName + '|' + platformName;

                dynamic project = projectItem.ContainingProject.Object; // Microsoft.VisualStudio.VCProjectEngine.VCProject
                dynamic config = project.Configurations.Item(pattern); // Microsoft.VisualStudio.VCProjectEngine.VCConfiguration

                dynamic file = projectItem.Object; // Microsoft.VisualStudio.VCProjectEngine.VCFile
                dynamic fileConfig = file.FileConfigurations.Item(configurationName); // Microsoft.VisualStudio.VCProjectEngine.VCFileConfiguration
                dynamic fileTool = fileConfig.Tool; // Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool

                return new FileConfig()
                {
                    AbsoluteFilePath = absoluteFilePath,

                    PlatformName = platformName,
                    PlatformToolset = config.Rules.Item("ConfigurationGeneral").GetEvaluatedPropertyValue("PlatformToolset"),

                    IncludeDirectories = config.Rules.Item("ConfigurationDirectories").GetEvaluatedPropertyValue("IncludePath"),
                    AdditionalIncludeDirectories = GetEvaluatedPropertyValue(fileTool, "AdditionalIncludeDirectories"),
                    IgnoreStandardIncludePath = GetEvaluatedPropertyValue(fileTool, "IgnoreStandardIncludePath"),

                    UndefineAllPreprocessorDefinitions = GetEvaluatedPropertyValue(fileTool, "UndefineAllPreprocessorDefinitions"),
                    PreprocessorDefinitions = GetEvaluatedPropertyValue(fileTool, "PreprocessorDefinitions"),
                    UndefinePreprocessorDefinitions = GetEvaluatedPropertyValue(fileTool, "UndefinePreprocessorDefinitions"),

                    ForcedIncludeFiles = GetEvaluatedPropertyValue(fileTool, "ForcedIncludeFiles"),
                    PrecompiledHeader = GetEvaluatedPropertyValue(fileTool, "PrecompiledHeader"),
                    PrecompiledHeaderFile = GetEvaluatedPropertyValue(fileTool, "PrecompiledHeaderFile"),

                    CompileAs = GetEvaluatedPropertyValue(fileTool, "CompileAs"),
                    CompileAsManaged = GetEvaluatedPropertyValue(fileTool, "CompileAsManaged"),
                    CompileAsWinRT = GetEvaluatedPropertyValue(fileTool, "CompileAsWinRT"),
                    DisableLanguageExtensions = GetEvaluatedPropertyValue(fileTool, "DisableLanguageExtensions"),
                    TreatWChar_tAsBuiltInType = GetEvaluatedPropertyValue(fileTool, "TreatWChar_tAsBuiltInType"),
                    ForceConformanceInForLoopScope = GetEvaluatedPropertyValue(fileTool, "ForceConformanceInForLoopScope"),
                    OpenMPSupport = GetEvaluatedPropertyValue(fileTool, "OpenMPSupport"),

                    RuntimeLibrary = GetEvaluatedPropertyValue(fileTool, "RuntimeLibrary"),
                    ExceptionHandling = GetEvaluatedPropertyValue(fileTool, "ExceptionHandling"),
                    EnableEnhancedInstructionSet = GetEvaluatedPropertyValue(fileTool, "EnableEnhancedInstructionSet"),
                    OmitDefaultLibName = GetEvaluatedPropertyValue(fileTool, "OmitDefaultLibName"),
                    RuntimeTypeInfo = GetEvaluatedPropertyValue(fileTool, "RuntimeTypeInfo"),
                    BasicRuntimeChecks = GetEvaluatedPropertyValue(fileTool, "BasicRuntimeChecks"),
                    LanguageStandard = GetEvaluatedPropertyValue(fileTool, "LanguageStandard"),

                    AdditionalOptions = GetEvaluatedPropertyValue(fileTool, "AdditionalOptions"),
                };
            }

            private static MethodInfo getEvaluatedPropertyValue;

            /// <summary>
            /// Computes property value taking into account inheritance, property sheets and macros.
            /// </summary>
            /// <param name="propertyName">name of the property (note that name corresponds to xml tag in vcxproj)</param>
            private static string GetEvaluatedPropertyValue(object fileTool, string propertyName)
            {
                if (getEvaluatedPropertyValue == null)
                {
                    var theType = fileTool.GetType();
                    getEvaluatedPropertyValue = theType.GetMethod(
                        "Microsoft.VisualStudio.VCProjectEngine.IVCRulePropertyStorage.GetEvaluatedPropertyValue",
                        BindingFlags.Instance | BindingFlags.NonPublic
                    );
                }
                return (string)getEvaluatedPropertyValue.Invoke(fileTool, new object[] { propertyName });
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
            
            #endregion

            public Capture[] ToCaptures(string path, out string cfamilyLanguage)
            {
                var p = new Capture()
                {
                    Executable = "cl.exe",
                    Cwd = Path.GetDirectoryName(AbsoluteFilePath),
                    StdErr = string.Format("{0} for {1}", ConvertPlatformToolset(PlatformToolset), ConvertPlatformName(PlatformName)),
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

                // TODO Q: what if it contains space in double quotes?
                AddRange(c.Cmd, AdditionalOptions.Split(' '));

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

            internal /* for testing */ static string ConvertPlatformName(string platformName)
            {
                switch (platformName)
                {
                    default:
                        throw new ArgumentException($"Unsupported PlatformName: {platformName}", nameof(platformName));
                    case "Win32":
                        return "x86";
                    case "x64":
                        return "x64";
                }
            }

            internal /* for testing */ static string ConvertPlatformToolset(string platformToolset)
            {
                switch (platformToolset)
                {
                    default:
                        throw new ArgumentException($"Unsupported PlatformToolset: {platformToolset}", nameof(platformToolset));
                    case "": // Bug https://github.com/SonarSource/sonarlint-visualstudio/issues/804
                        throw new ArgumentException(Strings.Daemon_PlatformToolsetNotSpecified);
                    case "v142":
                        return "19.20.00";
                    case "v141":
                    case "v141_xp":
                        return "19.10.00";
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
