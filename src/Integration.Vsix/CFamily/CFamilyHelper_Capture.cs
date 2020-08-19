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

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal static partial class CFamilyHelper
    {
        internal class Capture
        {
            private static readonly Regex AdditionalOptionsSplitPattern = new Regex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            #region Properties

            public string Compiler { get { return "msvc-cl"; } }

            public string Cwd { get; set; }

            public string Executable { get; set; }

            public List<string> Cmd { get; set; }

            public List<string> Env { get; set; }

            public string StdOut { get; set; }

            public string CompilerVersion { get; set; }

            public bool X64 { get; set; }

            #endregion

            public static Capture[] ToCaptures(FileConfig fileConfig, string path, out string cfamilyLanguage)
            {
                var p = new Capture
                {
                    Executable = "cl.exe",
                    Cwd = Path.GetDirectoryName(fileConfig.AbsoluteProjectPath),
                    CompilerVersion = fileConfig.CompilerVersion,
                    X64 = IsPlatformX64(fileConfig.PlatformName),
                    StdOut = "",
                };

                var c = new Capture
                {
                    Executable = p.Executable,
                    Cwd = p.Cwd,
                    Env = new List<string>(),
                    Cmd = new List<string>(),
                };
                c.Env.Add("INCLUDE=" + fileConfig.IncludeDirectories);
                c.Cmd.Add(c.Executable);
                Add(c.Cmd, "true".Equals(fileConfig.IgnoreStandardIncludePath) ? "/X" : "");
                AddRange(c.Cmd, "/I", fileConfig.AdditionalIncludeDirectories.Split(';'));
                AddRange(c.Cmd, "/FI", fileConfig.ForcedIncludeFiles.Split(';'));
                Add(c.Cmd, ConvertPrecompiledHeader(fileConfig.PrecompiledHeader, fileConfig.PrecompiledHeaderFile));

                Add(c.Cmd, "true".Equals(fileConfig.UndefineAllPreprocessorDefinitions) ? "/u" : "");
                AddRange(c.Cmd, "/D", fileConfig.PreprocessorDefinitions.Split(';'));
                AddRange(c.Cmd, "/U", fileConfig.UndefinePreprocessorDefinitions.Split(';'));

                Add(c.Cmd, ConvertCompileAsAndGetLanguage(fileConfig.CompileAs, path, out cfamilyLanguage));
                Add(c.Cmd, ConvertCompileAsManaged(fileConfig.CompileAsManaged));
                Add(c.Cmd, "true".Equals(fileConfig.CompileAsWinRT) ? "/ZW" : "");
                Add(c.Cmd, "true".Equals(fileConfig.DisableLanguageExtensions) ? "/Za" : ""); // defines macro "__STDC__" when compiling C
                Add(c.Cmd, "false".Equals(fileConfig.TreatWChar_tAsBuiltInType) ? "/Zc:wchar_t-" : ""); // undefines macros "_NATIVE_WCHAR_T_DEFINED" and "_WCHAR_T_DEFINED"
                Add(c.Cmd, "false".Equals(fileConfig.ForceConformanceInForLoopScope) ? "/Zc:forScope-" : "");
                Add(c.Cmd, "true".Equals(fileConfig.OpenMPSupport) ? "/openmp" : "");

                Add(c.Cmd, ConvertRuntimeLibrary(fileConfig.RuntimeLibrary));
                Add(c.Cmd, ConvertExceptionHandling(fileConfig.ExceptionHandling));
                Add(c.Cmd, ConvertEnableEnhancedInstructionSet(fileConfig.EnableEnhancedInstructionSet));
                Add(c.Cmd, "true".Equals(fileConfig.OmitDefaultLibName) ? "/Zl" : ""); // defines macro "_VC_NODEFAULTLIB"
                Add(c.Cmd, "false".Equals(fileConfig.RuntimeTypeInfo) ? "/GR-" : ""); // undefines macro "_CPPRTTI"
                Add(c.Cmd, ConvertBasicRuntimeChecks(fileConfig.BasicRuntimeChecks));
                Add(c.Cmd, ConvertLanguageStandard(fileConfig.LanguageStandard));
                AddRange(c.Cmd, GetAdditionalOptions(fileConfig.AdditionalOptions));

                c.Cmd.Add(fileConfig.AbsoluteFilePath);

                return new Capture[] { p, c };
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
