/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.IntegrationTests
{
    [TestClass]
    public class CFamily_CLangAnalyzer_IntegrationTests
    {
        private string testsDataDirectory;
        private string clExe;

        [TestInitialize]
        public void TestInitialize()
        {
            // Uri absolute path is used to make issues filename slashes consistent between expected and actual 
            testsDataDirectory = new Uri(Path.Combine(
                Path.GetDirectoryName(typeof(CFamily_CLangAnalyzer_IntegrationTests).Assembly.Location),
                "IntegrationTests\\")).AbsolutePath;

            // Subprocess.exe requires a valid path to an executable named cl.exe that prints something similar to the real compiler
            const string code = "Console.Error.WriteLine(\"Microsoft(R) C / C++ Optimizing Compiler Version 19.32.31114.2 for x64\");";
            clExe = DummyExeHelper.CreateDummyExe(testsDataDirectory, "cl.exe", 0, code);
        }

        [TestMethod]
        [DataRow("CLangAnalyzerTestFile_NoIssues_EmptyFile")]
        [DataRow("CLangAnalyzerTestFile_OneIssue")]
        [DataRow("CLangAnalyzerTestFile_TwoIssues")]
        [DataRow("CLangAnalyzerTestFile_OneIssue_HasSecondaryLocations")]
        public void CallAnalyzer_IntegrationTest(string testCaseFileName)
        {
            var testedFile = Path.Combine(testsDataDirectory, testCaseFileName + ".txt").Replace('/', '\\');

            // Sanity checks to help with debugging on the CI machine
            CheckFileExists(testedFile);
            CheckRulesMetadataFilesExist();

            var request = GetRequest(testedFile);
            var expectedMessages = GetExpectedMessages(testCaseFileName, testedFile);

            var messages = InvokeAnalyzer(request);

            messages.Where(x => !string.IsNullOrEmpty(x.Filename)).Should().BeEquivalentTo(expectedMessages, e => e.WithStrictOrdering());
        }

        private static void CheckRulesMetadataFilesExist()
        {
            var libDirectory = CFamilyShared.CFamilyFilesDirectory;
            Console.WriteLine($"[TEST SETUP] Checking CFamily lib directory exists: {libDirectory}");
            Directory.Exists(libDirectory).Should().BeTrue($"[TEST SETUP ERROR] CFamily lib directory could not be found: {libDirectory}");

            CheckFileExists(Path.Combine(libDirectory, "Sonar_way_profile.json"));
            CheckFileExists(Path.Combine(libDirectory, "RulesList.json"));
        }

        private static void CheckFileExists(string fileName)
        {
            Console.WriteLine($"[TEST SETUP] Checking for required file: {fileName}");
            File.Exists(fileName).Should().BeTrue($"[TEST SETUP ERROR] Could not find required test input file: {fileName}");
        }

        private CompilationDatabaseRequest GetRequest(string testedFile)
        {
            var command = "\"" + clExe + "\" /TP " + testedFile;
            var compilationDatabaseEntry = new CompilationDatabaseEntry
            {
                Directory = testsDataDirectory,
                Command = command,
                File = testedFile
            };

            var envVars = new ReadOnlyDictionary<string, string>(new Dictionary<string, string> {
                { "INCLUDE", "" }
            });

            var languageKey = SonarLanguageKeys.CPlusPlus;

            var config = new CFamilySonarWayRulesConfigProvider(CFamilyShared.CFamilyFilesDirectory).GetRulesConfiguration("cpp");
            var context = new RequestContext(
                languageKey, 
                config, 
                testedFile, 
                "",
                new CFamilyAnalyzerOptions(), 
                false);

            var request = new CompilationDatabaseRequest(compilationDatabaseEntry, context, envVars);

            return request;
        }

        private Message[] GetExpectedMessages(string testFileName, string testedFile)
        {
            var expectedResponseJson = File.ReadAllText(Path.Combine(testsDataDirectory, testFileName + "_response.json"));
            var expectedResponse = JsonConvert.DeserializeObject<Response>(expectedResponseJson);
            var messages = expectedResponse.Messages;
            foreach (var expectedResponseMessage in messages)
            {
                expectedResponseMessage.Filename = testedFile;

                foreach (var messagePart in expectedResponseMessage.Parts)
                {
                    messagePart.Filename = testedFile;
                }
            }

            return messages;
        }

        private static List<Message> InvokeAnalyzer(CompilationDatabaseRequest request)
        {
            var testLogger = new TestLogger(true);
            var processRunner = new ProcessRunner(new ConfigurableSonarLintSettings(), testLogger);

            var messages = new List<Message>();
            CLangAnalyzer.ExecuteSubProcess(messages.Add, request, processRunner, testLogger, CancellationToken.None, new FileSystem());
            messages = messages.Where(m => !m.RuleKey.StartsWith("internal.")).ToList();
            return messages;
        }
    }
}
