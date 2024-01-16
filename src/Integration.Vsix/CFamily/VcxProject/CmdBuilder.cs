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

using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.VCProjectEngine;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.VcxProject
{
    internal class CmdBuilder
    {
        private static readonly Regex DoubleSeparatorRegEx = new Regex(@"\\+",
            RegexOptions.Compiled,
            Core.RegexConstants.DefaultTimeout);

        // To avoid paths with spaces
        private static readonly Regex DoubleQuotedRegEx = new Regex("\"[^\\\"]*(\\.[^\\\"]*)*\"",
            RegexOptions.Compiled,
            Core.RegexConstants.DefaultTimeout);

        StringBuilder Cmd { get; set; } = new StringBuilder();
        bool IsHeader { get; set; }
        public string HeaderFileLang { get; set; } = "";

        public CmdBuilder(bool isHeader)
        {
            IsHeader = isHeader;
        }

        public string GetFullCmd()
        {
            return Cmd.ToString();
        }

        public void AddFile(string path)
        {
            // No need for a space at the end after the file
            Cmd.Append(AdjustPath(path));
        }
        public void AddCompiler(string path)
        {
            AddCmdOpt(AddQuote(path));
        }

        internal /* for testing */ void AddCmdOpt(string opt)
        {
            if (!string.IsNullOrEmpty(opt))
            {
                // Always seperate option by space
                Cmd.Append(opt + " ");
            }
        }

        internal /* for testing */ static string AddQuote(string s)
        {
            if (!DoubleQuotedRegEx.IsMatch(s))
            {
                string quote = "\"";
                return quote + s + quote;
            }
            return s;
        }

        internal /* for testing */ static string AdjustPath(string path)
        {
            // path cannot be empty
            path = path.Replace("/", @"\");

            path = DoubleSeparatorRegEx.Replace(path, @"\");

            if (path[path.Length - 1] == '\\')
            {
                path = path.Substring(0, path.Length - 1);
            }
            return AddQuote(path);
        }

        internal void AddOptFromProperties(IVCRulePropertyStorage properties)
        {
            // We use GetEvaluatedPropertyValue for: options that are essential, that were available since the beginning of MSVC C++ support, aor where having a defalt value doesn't make sense.
            // We use GetPotentiallyUnsupportedPropertyValue for options that are newly introduced by MSVC, newly supported by SLVS, or options where there is a clear default

            // list options
            string[] separator = new string[] { ";" };
            AddPathListOptions(properties, "AdditionalIncludeDirectories", "/I", separator);
            AddListOptions(properties, "PreprocessorDefinitions", "/D", separator, true);
            AddListOptions(properties, "UndefinePreprocessorDefinitions", "/U", separator, false);
            AddPathListOptions(properties, "ForcedIncludeFiles", "/FI", separator);

            // PCH options
            var hasForcedInclude =
                properties.GetEvaluatedPropertyValue("ForcedIncludeFiles").Split(separator, StringSplitOptions.RemoveEmptyEntries).Length != 0;
            var precompiledHeader = properties.GetEvaluatedPropertyValue("PrecompiledHeader");
            var precompiledHeaderFile = properties.GetEvaluatedPropertyValue("PrecompiledHeaderFile");
            var hasPCH = !string.IsNullOrEmpty(precompiledHeaderFile);
            if (hasPCH)
            {
                if (precompiledHeader.Equals("Use"))
                {
                    AddCmdOpt("/Yu" + AdjustPath(precompiledHeaderFile));
                    if (IsHeader && !hasForcedInclude)
                    {
                        AddCmdOpt("/FI" + AdjustPath(precompiledHeaderFile));
                    }
                }
                else if (precompiledHeader.Equals("Create"))
                {
                    AddCmdOpt("/Yc" + AdjustPath(precompiledHeaderFile));
                }
            }

            // binary options
            AddBinaryOption(properties, "CompileAsWinRT", "/ZW", "true");
            AddBinaryOption(properties, "OpenMPSupport", "/openmp", "true");
            AddBinaryOption(properties, "RuntimeTypeInfo", "/GR-", "false");
            AddBinaryOption(properties, "DisableLanguageExtensions", "/Za", "true");
            AddBinaryOption(properties, "TreatWChar_tAsBuiltInType", "/Zc:wchar_t-", "false");
            AddBinaryOption(properties, "ForceConformanceInForLoopScope", "/Zc:forScope-", "false");
            AddBinaryOption(properties, "IgnoreStandardIncludePath", "/X", "true");
            AddBinaryOption(properties, "OmitDefaultLibName", "/Zl", "true");
            AddBinaryOption(properties, "UndefineAllPreprocessorDefinitions", "/u", "true");
            AddTrueFalse(properties, "UseStandardPreprocessor", "/Zc:preprocessor", "/Zc:preprocessor-");
            AddTrueFalse(properties, "ConformanceMode", "/permissive-", "/permissive");

            // enum options
            var cppStandard = GetPotentiallyUnsupportedPropertyValue(properties, "LanguageStandard", "");
            AddCmdOpt(ConvertCppStandard(cppStandard));

            var exceptionHandling = properties.GetEvaluatedPropertyValue("ExceptionHandling");
            AddCmdOpt(ConvertExceptionHandling(exceptionHandling));

            var enableEnhancedInstructionSet = properties.GetEvaluatedPropertyValue("EnableEnhancedInstructionSet");
            AddCmdOpt(ConvertEnableEnhancedInstructionSet(enableEnhancedInstructionSet));

            var compileAsManaged = GetPotentiallyUnsupportedPropertyValue(properties, "CompileAsManaged", "");
            AddCmdOpt(ConvertCompileAsManaged(compileAsManaged));

            var runtimeLibrary = properties.GetEvaluatedPropertyValue("RuntimeLibrary");
            AddCmdOpt(ConvertRuntimeLibrary(runtimeLibrary));

            var basicRuntimeChecks = GetPotentiallyUnsupportedPropertyValue(properties, "BasicRuntimeChecks", "");
            AddCmdOpt(ConvertBasicRuntimeChecks(basicRuntimeChecks));

            var structMemberAlignment = GetPotentiallyUnsupportedPropertyValue(properties, "StructMemberAlignment", "");
            AddCmdOpt(ConvertStructMemberAlignment(structMemberAlignment));

            var compileAs = properties.GetEvaluatedPropertyValue("CompileAs");
            if (IsHeader)
            {
                HeaderFileLang = (compileAs == "CompileAsC") ? "c" : "cpp";
            }
            else
            {
                AddCmdOpt(ConvertCompileAsAndGetLanguage(compileAs));
            }

            // Additional options
            var additionalOptions = properties.GetEvaluatedPropertyValue("AdditionalOptions");
            AddCmdOpt(additionalOptions);
        }

        private void AddListOptions(IVCRulePropertyStorage ivcRulePropertyStorage, string vsOption, string compileOption, string[] separator, bool addQuote = true)
        {
            var additionalIncludeDirectories = ivcRulePropertyStorage.GetEvaluatedPropertyValue(vsOption);
            string[] opts = additionalIncludeDirectories.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string opt in opts)
            {
                AddCmdOpt(addQuote ? compileOption + AddQuote(opt) : compileOption + opt);
            }
        }

        private void AddPathListOptions(IVCRulePropertyStorage ivcRulePropertyStorage, string vsOption, string compileOption, string[] separator)
        {
            var additionalIncludeDirectories = ivcRulePropertyStorage.GetEvaluatedPropertyValue(vsOption);
            string[] opts = additionalIncludeDirectories.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string opt in opts)
            {
                AddCmdOpt(compileOption + AdjustPath(opt));
            }
        }

        private void AddBinaryOption(IVCRulePropertyStorage ivcRulePropertyStorage, string vsOption, string compileOption, string addIfEqual)
        {
            var optVal = GetPotentiallyUnsupportedPropertyValue(ivcRulePropertyStorage, vsOption, "");
            if (addIfEqual.Equals(optVal))
            {
                AddCmdOpt(compileOption);
            }
        }
        private void AddTrueFalse(IVCRulePropertyStorage ivcRulePropertyStorage, string vsOption, string compileOptionTrue, string compileOptionFalse)
        {
            var optVal = GetPotentiallyUnsupportedPropertyValue(ivcRulePropertyStorage, vsOption, "");
            if ("true".Equals(optVal))
            {
                AddCmdOpt(compileOptionTrue);
            }
            else if ("false".Equals(optVal))
            {
                AddCmdOpt(compileOptionFalse);
            }
        }

        internal /* for testing */ static string ConvertStructMemberAlignment(string value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentException($"Unsupported StructMemberAlignment: {value}", nameof(value));
                case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                case "Default":
                    return "";
                case "16Bytes":
                    return "/Zp16";
                case "8Bytes":
                    return "/Zp8";
                case "4Bytes":
                    return "/Zp4";
                case "2Bytes":
                    return "/Zp2";
                case "1Bytes":
                    return "/Zp1";
            }
        }

        internal /* for testing */ static string ConvertCompileAsAndGetLanguage(string value)
        {
            switch (value)
            {
                default:
                    throw new ArgumentException($"Unsupported CompileAs: {value}", nameof(value));
                case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                case "Default":
                    return "";
                case "CompileAsC":
                    return "/TC";
                case "CompileAsCpp":
                    return "/TP";
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
                case "AdvancedVectorExtensions512":
                    return "/arch:AVX512";
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

        internal /* for testing */ static string ConvertCppStandard(string value)
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
                case "stdcpp20":
                    return "/std:c++20";
                case "stdcpp17":
                    return "/std:c++17";
                case "stdcpp14":
                    return "/std:c++14";
            }
        }
        /// <summary>
        /// Returns the value of a property that might not be supported by the current version of the compiler.
        /// If the property is not supported the default value is returned.
        /// </summary>
        /// <remarks>e.g. LanguageStandard is not supported by VS2015 so attempting to fetch the property will throw.
        /// We don't want the analysis to fail in that case so we'll catch the exception and return the default.</remarks>
        internal /* for testing */ static string GetPotentiallyUnsupportedPropertyValue(IVCRulePropertyStorage settings, string propertyName, string defaultValue)
        {
            try
            {
                return settings.GetEvaluatedPropertyValue(propertyName);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Property was not found
                return defaultValue;
            }
        }
    }
}
