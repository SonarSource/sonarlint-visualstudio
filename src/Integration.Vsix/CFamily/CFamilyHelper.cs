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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal static class CFamilyHelper
    {
        public const string CPP_LANGUAGE_KEY = "cpp";
        public const string C_LANGUAGE_KEY = "c";

        private static readonly string analyzerExeFilePath = Path.Combine(
            Path.GetDirectoryName(typeof(RulesLoader).Assembly.Location),
            ".CFamilyEmbedded", "subprocess.exe");

        private const int AnalysisTimeoutMs = 10 * 1000;
        
        public static void ProcessFile(IProcessRunner runner, IIssueConsumer issueConsumer, ILogger logger,
            ProjectItem projectItem, string absoluteFilePath, string charset)
        {
            if (IsHeaderFile(absoluteFilePath))
            {
                // We can't analyze header files currently because we can't get all
                // of the required configuration information
                logger.WriteLine($"Cannot analyze header files. File: '{absoluteFilePath}'");
                return;
            }

            if (!IsFileInSolution(projectItem))
            {
                logger.WriteLine($"Unable to retrieve the configuration for file '{absoluteFilePath}'. Check the file is part of a project in the current solution.");
                return;
            }

            string sqLanguage;
            Request request = TryGetConfig(logger, projectItem, absoluteFilePath, out sqLanguage);
            if (request != null && request.File != null && sqLanguage != null)
            {
                request.Options = GetKeyValueOptionsList();
                var response = CallClangAnalyzer(request, runner, logger);

                if (response != null)
                {
                    issueConsumer.Accept(absoluteFilePath, response.Messages.Where(m => m.Filename == absoluteFilePath)
                                    .Select(m => ToSonarLintIssue(m, sqLanguage))
                                    .ToList());
                }

            }
        }

        internal /* for testing */ static string[]GetKeyValueOptionsList()
        {
            var options = GetDefaultOptions();
            options.Add("internal.qualityProfile", string.Join(",", RulesLoader.ReadActiveRulesList()));
            var data = options.Select(kv => kv.Key + "=" + kv.Value).ToArray();
            return data;
        }
        internal /* for testing */ static Response CallClangAnalyzer(Request request, IProcessRunner runner, ILogger logger)
        {
            string tempFileName = null;
            try
            {
                tempFileName = Path.GetTempFileName();

                // Create a FileInfo object to set the file's attributes
                FileInfo fileInfo = new FileInfo(tempFileName);

                // Set the Attribute property of this file to Temporary. 
                // Although this is not completely necessary, the .NET Framework is able 
                // to optimize the use of Temporary files by keeping them cached in memory.
                fileInfo.Attributes = FileAttributes.Temporary;

                using (var writeStream = new FileStream(tempFileName, FileMode.Open))
                {
                    Protocol.Write(new BinaryWriter(writeStream), request);
                }

                var success = ExecuteAnalysis(runner, tempFileName, logger);

                if (success)
                {
                    using (var readStream = new FileStream(tempFileName, FileMode.Open))
                    {
                        Response response = Protocol.Read(new BinaryReader(readStream));
                        return response;
                    }
                }

                return null;
            }
            finally
            {
                if (tempFileName != null)
                {
                    File.Delete(tempFileName);
                }
            }

        }

        private static Dictionary<string, string> GetDefaultOptions()
        {
            Dictionary<string, string> defaults = new Dictionary<string, string>();
            foreach (string ruleKey in RulesLoader.ReadRulesList())
            {
                Dictionary<string, string> ruleParams = RulesLoader.ReadRuleParams(ruleKey);
                foreach (var param in ruleParams)
                {
                    string optionKey = ruleKey + "." + param.Key;
                    defaults.Add(optionKey, param.Value);
                }
            }
            return defaults;
        }

        private static Sonarlint.Issue ToSonarLintIssue(Message cfamilyIssue, string sqLanguage)
        {
            return new Sonarlint.Issue()
            {
                FilePath = cfamilyIssue.Filename,
                Message = cfamilyIssue.Text,
                RuleKey = sqLanguage + ":" + cfamilyIssue.RuleKey,
                Severity = Sonarlint.Issue.Types.Severity.Blocker, // FIXME
                Type = Sonarlint.Issue.Types.Type.CodeSmell, //FIXME
                StartLine = cfamilyIssue.Line,
                StartLineOffset = cfamilyIssue.Column - 1,
                EndLine = cfamilyIssue.EndLine,
                EndLineOffset = cfamilyIssue.EndColumn - 1
            };
        }

        private static bool ExecuteAnalysis(IProcessRunner runner, string fileName, ILogger logger)
        {
            if (analyzerExeFilePath == null)
            {
                logger.WriteLine("Unable to locate the CFamily analyzer exe");
                return false;
            }

            ProcessRunnerArguments args = new ProcessRunnerArguments(analyzerExeFilePath, false);
            args.CmdLineArgs = new string[] { fileName };
            args.TimeoutInMilliseconds = AnalysisTimeoutMs;

            bool success = runner.Execute(args);
            return success;
        }

        internal static Request TryGetConfig(ILogger logger, ProjectItem projectItem, string absoluteFilePath,
            out string sqLanguage)
        {
            Debug.Assert(!IsHeaderFile(absoluteFilePath),
                $"Not expecting TryGetConfig to be called for header files: {absoluteFilePath}");

            Debug.Assert(IsFileInSolution(projectItem),
                $"Not expecting to be called for files that are not in the solution: {absoluteFilePath}");

            try
            {
                return FileConfig.TryGet(projectItem, absoluteFilePath).ToRequest(absoluteFilePath, out sqLanguage);
            }
            catch (Exception e)
            {
                logger.WriteLine($"Unable to collect C/C++ configuration for {absoluteFilePath}: {e.ToString()}");
                sqLanguage = null;
                return null;
            }
        }

        internal static bool IsHeaderFile(string absoluteFilePath)
        {
            var fileInfo = new FileInfo(absoluteFilePath);
            return fileInfo.Extension.Equals(".h", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsFileInSolution(ProjectItem projectItem)
        {
            try
            {
                // Issue 667:  https://github.com/SonarSource/sonarlint-visualstudio/issues/667
                // If you open a C++ file that is not part of the current solution then
                // VS will cruft up a temporary vcxproj so that it can provide language
                // services for the file (e.g. syntax highlighting). This means that
                // even though we have what looks like a valid project item, it might
                // not actually belong to a real project.
                var indexOfSingleFileString = projectItem?.ContainingProject?.FullName.IndexOf("SingleFileISense", StringComparison.OrdinalIgnoreCase);
                return indexOfSingleFileString.HasValue &&
                    indexOfSingleFileString.Value <= 0 &&
                    projectItem.ConfigurationManager != null &&
                    // the next line will throw if the file is not part of a solution
                    projectItem.ConfigurationManager.ActiveConfiguration != null;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
            }
            return false;
        }

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

            #endregion

            public Capture[] ToCaptures(string path, out string sqLanguage)
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

                Add(c.Cmd, ConvertCompileAsAndGetSqLanguage(CompileAs, path, out sqLanguage));
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

                // TODO Q: what if it contains space in double quotes?
                AddRange(c.Cmd, AdditionalOptions.Split(' '));

                c.Cmd.Add(AbsoluteFilePath);

                return new Capture[] { p, c };
            }

            public Request ToRequest(string path, out string sqLanguage)
            {
                Capture[] c = ToCaptures(path, out sqLanguage);
                return MsvcDriver.ToRequest(c);
            }

            internal static string ConvertPlatformName(string platformName)
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

            internal static string ConvertPlatformToolset(string platformToolset)
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

            internal static string ConvertCompileAsAndGetSqLanguage(string value, string path, out string sqLanguage)
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
                            sqLanguage = CPP_LANGUAGE_KEY;
                        }
                        else if (path.ToLowerInvariant().EndsWith(".c"))
                        {
                            sqLanguage = C_LANGUAGE_KEY;
                        }
                        else
                        {
                            sqLanguage = null;
                        }
                        return "";
                    case "CompileAsC":
                        sqLanguage = "c";
                        return "/TC";
                    case "CompileAsCpp":
                        sqLanguage = CPP_LANGUAGE_KEY;
                        return "/TP";
                }
            }

            internal static string ConvertCompileAsManaged(string value)
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

            internal static string ConvertRuntimeLibrary(string value)
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

            internal static string ConvertEnableEnhancedInstructionSet(string value)
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

            internal static string ConvertExceptionHandling(string value)
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

            internal static string ConvertBasicRuntimeChecks(string value)
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

            internal static string ConvertPrecompiledHeader(string value, string header)
            {
                switch (value)
                {
                    default:
                        throw new ArgumentException($"Unsupported PrecompiledHeader: {value}", nameof(value));
                    case "": // https://github.com/SonarSource/sonarlint-visualstudio/issues/738
                        return "";
                    case "Create":
                        return "/Yc" + header;
                    case "Use":
                        return "/Yu" + header;
                }
            }
        }

        private static void AddRange(IList<string> cmd, string[] values)
        {
            foreach (string value in values)
            {
                Add(cmd, value);
            }
        }

        private static void AddRange(IList<string> cmd, string prefix, string[] values)
        {
            foreach (string value in values)
            {
                Add(cmd, prefix, value);
            }
        }

        private static void Add(IList<String> cmd, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                cmd.Add(value);
            }
        }

        private static void Add(IList<string> cmd, string prefix, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                cmd.Add(prefix);
                cmd.Add(value);
            }
        }

        internal class Capture
        {
            public string Compiler { get { return "msvc-cl"; } }

            public string Cwd { get; set; }

            public string Executable { get; set; }

            public List<string> Cmd { get; set; }

            public List<string> Env { get; set; }

            public string StdOut { get; set; }

            public string StdErr { get; set; }

        }
    }
}
