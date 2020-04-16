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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    internal static partial class CFamilyHelper
    {
        public const string CPP_LANGUAGE_KEY = "cpp";
        public const string C_LANGUAGE_KEY = "c";

        public static readonly string CFamilyFilesDirectory = Path.Combine(
            Path.GetDirectoryName(typeof(CFamilyHelper).Assembly.Location),
            "lib");

        private static readonly string analyzerExeFilePath = Path.Combine(
            CFamilyFilesDirectory, "subprocess.exe");

        private const int DefaultAnalysisTimeoutMs = 20 * 1000;
        private const string TimeoutEnvVar = "SONAR_INTERNAL_CFAMILY_ANALYSIS_TIMEOUT_MS";

        public static Request CreateRequest(ILogger logger, ProjectItem projectItem, string absoluteFilePath, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, IAnalyzerOptions analyzerOptions)
        {
            if (IsHeaderFile(absoluteFilePath))
            {
                // We can't analyze header files currently because we can't get all
                // of the required configuration information
                logger.WriteLine($"Cannot analyze header files. File: '{absoluteFilePath}'");
                return null;
            }

            if (!IsFileInSolution(projectItem))
            {
                logger.WriteLine($"Unable to retrieve the configuration for file '{absoluteFilePath}'. Check the file is part of a project in the current solution.");
                return null;
            }

            var fileConfig = TryGetConfig(logger, projectItem, absoluteFilePath);
            if (fileConfig == null)
            {
                return null;
            }

            return CreateRequest(fileConfig, absoluteFilePath, cFamilyRulesConfigProvider, analyzerOptions);
        }

        private static Request CreateRequest(FileConfig fileConfig, string absoluteFilePath, ICFamilyRulesConfigProvider cFamilyRulesConfigProvider, IAnalyzerOptions analyzerOptions)
        {
            var request = fileConfig.ToRequest(absoluteFilePath);
            if (request?.File == null || request?.CFamilyLanguage == null)
            {
                return null;
            }

            request.RulesConfiguration = cFamilyRulesConfigProvider.GetRulesConfiguration(request.CFamilyLanguage);
            Debug.Assert(request.RulesConfiguration != null, "RulesConfiguration should be set for the analysis request");
            request.Options = GetKeyValueOptionsList(request.RulesConfiguration);

            if (analyzerOptions is CFamilyAnalyzerOptions cFamilyAnalyzerOptions && cFamilyAnalyzerOptions.RunReproducer)
            {
                request.Flags |= Request.CreateReproducer;
            }

            return request;
        }

        internal /* for testing */ static string[]GetKeyValueOptionsList(ICFamilyRulesConfig rulesConfiguration)
        {
            var options = GetDefaultOptions(rulesConfiguration);
            options.Add("internal.qualityProfile", string.Join(",", rulesConfiguration.ActivePartialRuleKeys));
            var data = options.Select(kv => kv.Key + "=" + kv.Value).ToArray();
            return data;
        }

        private static Dictionary<string, string> GetDefaultOptions(ICFamilyRulesConfig rulesConfiguration)
        {
            Dictionary<string, string> defaults = new Dictionary<string, string>();
            foreach (string ruleKey in rulesConfiguration.AllPartialRuleKeys)
            {
                if (rulesConfiguration.RulesParameters.TryGetValue(ruleKey, out IDictionary<string, string> ruleParams))
                {
                    foreach (var param in ruleParams)
                    {
                        string optionKey = ruleKey + "." + param.Key;
                        defaults.Add(optionKey, param.Value);
                    }
                }
            }

            return defaults;
        }

        internal /* for testing */ static Response CallClangAnalyzer(Request request, IProcessRunner runner, ILogger logger, CancellationToken cancellationToken)
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

                var workingDirectory = Path.GetTempPath();
                var success = ExecuteAnalysis(runner, tempFileName, workingDirectory, logger, cancellationToken);

                if (success)
                {
                    if ((request.Flags & Request.CreateReproducer) != 0)
                    {
                        logger.WriteLine(CFamilyStrings.MSG_ReproducerSaved,
                            Path.Combine(workingDirectory, "sonar-cfamily.reproducer"));
                    }

                    using (var readStream = new FileStream(tempFileName, FileMode.Open))
                    {
                        Response response = Protocol.Read(new BinaryReader(readStream), request.File);
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

        internal /* for testing */ static Sonarlint.Issue ToSonarLintIssue(Message cfamilyIssue, string sqLanguage, ICFamilyRulesConfig rulesConfiguration)
        {
            // Lines and character positions are 1-based
            Debug.Assert(cfamilyIssue.Line > 0);

            // BUT special case of EndLine=0, Column=0, EndColumn=0 meaning "select the whole line"
            Debug.Assert(cfamilyIssue.EndLine >= 0);
            Debug.Assert(cfamilyIssue.Column > 0 || cfamilyIssue.Column == 0);
            Debug.Assert(cfamilyIssue.EndColumn > 0 || cfamilyIssue.EndLine == 0);

            // Look up default severity and type
            var defaultSeverity = rulesConfiguration.RulesMetadata[cfamilyIssue.RuleKey].DefaultSeverity;
            var defaultType = rulesConfiguration.RulesMetadata[cfamilyIssue.RuleKey].Type;

            return new Sonarlint.Issue()
            {
                FilePath = cfamilyIssue.Filename,
                Message = cfamilyIssue.Text,
                RuleKey = sqLanguage + ":" + cfamilyIssue.RuleKey,
                Severity = Convert(defaultSeverity),
                Type = Convert(defaultType),
                StartLine = cfamilyIssue.Line,
                EndLine = cfamilyIssue.EndLine,

                // We don't care about the columns in the special case EndLine=0
                StartLineOffset = cfamilyIssue.EndLine == 0 ? 0 : cfamilyIssue.Column - 1,
                EndLineOffset = cfamilyIssue.EndLine == 0 ? 0 : cfamilyIssue.EndColumn - 1
            };
        }

        /// <summary>
        /// Converts from the Core issue severity enum to the equivalent daemon protofbuf generated enum
        /// </summary>
        internal /* for testing */ static Sonarlint.Issue.Types.Severity Convert(IssueSeverity issueSeverity)
        {
            switch (issueSeverity)
            {
                case IssueSeverity.Blocker:
                    return Sonarlint.Issue.Types.Severity.Blocker;
                case IssueSeverity.Critical:
                    return Sonarlint.Issue.Types.Severity.Critical;
                case IssueSeverity.Info:
                    return Sonarlint.Issue.Types.Severity.Info;
                case IssueSeverity.Major:
                    return Sonarlint.Issue.Types.Severity.Major;
                case IssueSeverity.Minor:
                    return Sonarlint.Issue.Types.Severity.Minor;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueSeverity));
            }
        }

        /// <summary>
        /// Converts from the Core issue type enum to the equivalent daemon protofbuf generated enum
        /// </summary>
        internal /* for testing */static Sonarlint.Issue.Types.Type Convert(IssueType issueType)
        {
            switch (issueType)
            {
                case IssueType.Bug:
                    return Sonarlint.Issue.Types.Type.Bug;
                case IssueType.CodeSmell:
                    return Sonarlint.Issue.Types.Type.CodeSmell;
                case IssueType.Vulnerability:
                    return Sonarlint.Issue.Types.Type.Vulnerability;

                default:
                    throw new ArgumentOutOfRangeException(nameof(issueType));
            }
        }

        private static bool ExecuteAnalysis(IProcessRunner runner, string fileName, string workingDirectory, ILogger logger, CancellationToken cancellationToken)
        {
            if (analyzerExeFilePath == null)
            {
                logger.WriteLine("Unable to locate the CFamily analyzer exe");
                return false;
            }

            var args = new ProcessRunnerArguments(analyzerExeFilePath, false)
            {
                CmdLineArgs = new[] {fileName},
                TimeoutInMilliseconds = GetTimeoutInMs(),
                CancellationToken = cancellationToken,
                WorkingDirectory = workingDirectory
            };

            var success = runner.Execute(args);

            return success;
        }

        internal /* for testing*/ static int GetTimeoutInMs()
        {
            var setting = Environment.GetEnvironmentVariable(TimeoutEnvVar);

            if (int.TryParse(setting, System.Globalization.NumberStyles.Integer, System.Globalization.NumberFormatInfo.InvariantInfo,  out int userSuppliedTimeout)
                && userSuppliedTimeout > 0)
            {
                return userSuppliedTimeout;
            }

            return DefaultAnalysisTimeoutMs;
        }

        internal static FileConfig TryGetConfig(ILogger logger, ProjectItem projectItem, string absoluteFilePath)
        {
            Debug.Assert(!IsHeaderFile(absoluteFilePath),
                $"Not expecting TryGetConfig to be called for header files: {absoluteFilePath}");

            Debug.Assert(IsFileInSolution(projectItem),
                $"Not expecting to be called for files that are not in the solution: {absoluteFilePath}");

            try
            {
                return FileConfig.TryGet(projectItem, absoluteFilePath);
            }
            catch (Exception e)
            {
                logger.WriteLine($"Unable to collect C/C++ configuration for {absoluteFilePath}: {e.ToString()}");
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
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                // Suppress non-critical exceptions
            }
            return false;
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

            public string CompilerVersion { get; set; }

            public bool X64 { get; set; }

        }
    }
}
