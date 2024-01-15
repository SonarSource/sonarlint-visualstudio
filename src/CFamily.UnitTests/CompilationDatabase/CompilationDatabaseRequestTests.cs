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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;

namespace SonarLint.VisualStudio.CFamily.CompilationDatabase.UnitTests
{
    [TestClass]
    public class CompilationDatabaseRequestTests
    {
        private readonly RequestContext ValidContext = new RequestContext("cpp", Mock.Of<ICFamilyRulesConfig>(), "file.txt", "pchFile.txt", null, false);
        private readonly CompilationDatabaseEntry ValidDbEntry = new CompilationDatabaseEntry { File = "file.txt", Directory = "c:\\", Command = "a command" };
        private readonly IReadOnlyDictionary<string, string> ValidEnvVars = new Dictionary<string, string> { { "key1", "value1" } };

        [TestMethod]
        public void Ctor_InvalidArguments_Throws()
        {
            Action act = () => new CompilationDatabaseRequest(null, ValidContext, ValidEnvVars);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("databaseEntry");

            act = () => new CompilationDatabaseRequest(ValidDbEntry, null, ValidEnvVars);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("context");
        }

        [TestMethod]
        public void Ctor_NullEnvVars_DoesNotThrow()
        {
            var testSubject = new CompilationDatabaseRequest(ValidDbEntry, ValidContext, null);
            testSubject.EnvironmentVariables.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null, "args")]
        [DataRow("", "args")]
        [DataRow("cmd", null)]
        [DataRow("cmd", "")]
        public void Ctor_ValidCommandArgsCombination_ShouldNotThrow(string command, string args)
        {
            var dbEntry = new CompilationDatabaseEntry
            {
                File = "file",
                Directory = "dir",
                Command = command,
                Arguments = args
            };

            Action act = () => new CompilationDatabaseRequest(dbEntry, ValidContext, ValidEnvVars);
            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("", "")]
        [DataRow(null, "")]
        [DataRow("", null)]
        [DataRow("command", "args")] // can't have both
        public void Ctor_InvalidCommandArgsCombination_ShouldThrow(string command, string args)
        {
            var dbEntry = new CompilationDatabaseEntry
            {
                File = "file",
                Directory = "dir",
                Command = command,
                Arguments = args
            };

            Action act = () => new CompilationDatabaseRequest(dbEntry, ValidContext, ValidEnvVars);
            act.Should().ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public void WriteRequest_HeaderFile_WritesTheFileFromContext()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.cpp", Directory = "c:\\aaa", Command = "any" };
            var context = new RequestContext("any", Mock.Of<ICFamilyRulesConfig>(), "file.h", "d:\\preamble.txt", null, true);

            var tokens = WriteRequest(dbEntry, context);

            // File name should be taken from context, to support header files
            CheckExpectedSetting(tokens, "File", "file.h");
        }

        [TestMethod]
        public void WriteRequest_HeaderFile_WritesHeaderFileLanguage()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.cpp", Directory = "c:\\aaa", Command = "any" };
            var context = new RequestContext("any.language", Mock.Of<ICFamilyRulesConfig>(), "file.h", "d:\\preamble.txt", null, true);

            var tokens = WriteRequest(dbEntry, context);

            CheckExpectedSetting(tokens, "HeaderFileLanguage", "any.language");
        }

        [TestMethod]
        public void WriteRequest_NotHeaderFile_HeaderFileLanguageIsNotWritten()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.cpp", Directory = "c:\\aaa", Command = "any" };
            var context = new RequestContext("any.language", Mock.Of<ICFamilyRulesConfig>(), "file.cpp", "d:\\preamble.txt", null, false);

            var tokens = WriteRequest(dbEntry, context);

            CheckSettingDoesNotExist(tokens, "HeaderFileLanguage");
        }

        [TestMethod]
        public void WriteRequest_ValidRequest_ExpectedHeaderFooterAndSimpleProperties()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.txt", Directory = "c:\\aaa", Command = "any" };
            var context = new RequestContext("any", Mock.Of<ICFamilyRulesConfig>(), "file.h", "d:\\preamble.txt", null, true);

            var tokens = WriteRequest(dbEntry, context);

            // Header and footer
            tokens.First().Should().Be("SL-IN");
            tokens.Last().Should().Be("SL-END");

            // Simple properties i.e. ones that are just written as-is
            CheckExpectedSetting(tokens, "File", "file.h");
            CheckExpectedSetting(tokens, "Directory", "c:\\aaa");
            CheckExpectedSetting(tokens, "PreambleFile", "d:\\preamble.txt");
        }

        [TestMethod]
        public void WriteRequest_WithCommand_ExpectedSettingWritten()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.txt", Directory = "c:\\aaa", Command = "cmd1 cmd2" };

            var tokens = WriteRequest(dbEntry, ValidContext);

            CheckExpectedSetting(tokens, "Command", "cmd1 cmd2");
            CheckSettingDoesNotExist(tokens, "Arguments");
        }

        [TestMethod]
        public void WriteRequest_WithArguments_ExpectedSettingWritten()
        {
            var dbEntry = new CompilationDatabaseEntry { File = "file.txt", Directory = "c:\\aaa", Arguments = "arg1\narg2" };

            var tokens = WriteRequest(dbEntry, ValidContext);

            CheckExpectedSetting(tokens, "Arguments", "arg1\narg2");
            CheckSettingDoesNotExist(tokens, "Commands");
        }

        [TestMethod]
        [DataRow(true, "true")]
        [DataRow(false, "false")]
        public void WriteRequest_CreateReproducer_ExpectedSettingWritten(bool createReproducer, string expectedValue)
        {
            var analyzerOptions = new CFamilyAnalyzerOptions { CreateReproducer = createReproducer };
            var context = CreateContext(analyzerOptions: analyzerOptions);

            var tokens = WriteRequest(ValidDbEntry, context);

            CheckExpectedSetting(tokens, "CreateReproducer", expectedValue);
        }

        [TestMethod]
        [DataRow(true, "true")]
        [DataRow(false, "false")]
        public void WriteRequest_CreatePreamble_ExpectedSettingWritten(bool createPch, string expectedValue)
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("active1", isActive: true);

            var analyzerOptions = new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = createPch };
            var context = CreateContext(rulesConfig: rulesConfig, analyzerOptions: analyzerOptions);

            var tokens = WriteRequest(ValidDbEntry, context);

            CheckExpectedSetting(tokens, "BuildPreamble", expectedValue);

            if (createPch)
            {
                CheckExpectedSetting(tokens, "QualityProfile", string.Empty);
            }
            else
            {
                CheckExpectedSetting(tokens, "QualityProfile", "active1");
            }
        }

        [TestMethod]
        public void WriteRequest_QualityProfile_ExpectedSettingWritten()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("inactive1",  isActive: false)
                    .AddRule("active1", isActive: true)
                    .AddRule("active2", isActive: true)
                    .AddRule("inactive2", isActive: false);

            var context = CreateContext(rulesConfig: rulesConfig);

            var tokens = WriteRequest(ValidDbEntry, context);

            CheckExpectedSetting(tokens, "QualityProfile", "active1,active2");
        }

        [TestMethod]
        public void WriteRequest_WithRuleParameters_ExpectedSettingWritten()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("inactive1", isActive: false,
                        parameters: new Dictionary<string, string>
                        {
                            { "XXX1", "VVV1"}
                        })

                    // Active rule with no parameters
                    .AddRule("active1", isActive: true)

                    // Active rule, one parameter
                    .AddRule("active2", isActive: true,
                        parameters: new Dictionary<string, string>
                        {
                            { "key2_1", "value2_1" }
                        })

                    .AddRule("inactive2", isActive: false,
                        parameters: new Dictionary<string, string>
                        {
                            { "XXX2", "VVV2"},
                            { "XXX3", "VVV3"}
                        })

                    // Active rule, multiple parameters
                    .AddRule("active3", isActive: true,
                        parameters: new Dictionary<string, string>
                        {
                            { "key3_1", "value3_1" },
                            { "key3_2", "value3_2" }
                        });

            var context = CreateContext(rulesConfig: rulesConfig);

            var tokens = WriteRequest(ValidDbEntry, context);

            CheckExpectedSetting(tokens, "active2.key2_1", "value2_1");
            CheckExpectedSetting(tokens, "active3.key3_1", "value3_1");
            CheckExpectedSetting(tokens, "active3.key3_2", "value3_2");

            CheckSettingDoesNotExist(tokens, "inactive1");
            CheckSettingDoesNotExist(tokens, "inactive2");
            CheckSettingDoesNotExist(tokens, "active1");
            CheckSettingDoesNotExist(tokens, "active4");

        }

        [TestMethod]
        public void WriteRequest_NoRuleParameters_NoErrors()
        {
            // Active rules with no parameters
            var rulesConfig = new DummyCFamilyRulesConfig("cpp")
                    .AddRule("active1", isActive: true)
                    .AddRule("active2", isActive: true);

            var context = CreateContext(rulesConfig: rulesConfig);

            var tokens = WriteRequest(ValidDbEntry, context);

            tokens.Where(x => x.StartsWith("active1.")).Should().BeEmpty();
            tokens.Where(x => x.StartsWith("active2.")).Should().BeEmpty();
        }

        [TestMethod]
        public void WriteDiagnostics_ExpectedDataWritten()
        {
            // Expecting the context to be ignored
            var context = CreateContext("foo", Mock.Of<ICFamilyRulesConfig>(), "some file", "some pch file",
                new CFamilyAnalyzerOptions { CreatePreCompiledHeaders = true });

            var dbEntry = new CompilationDatabaseEntry
            {
                File = "c:\\file.txt", Directory = "d:\\", Command = "1\n2\n"
            };
            var expected = @"{
  ""directory"": ""d:\\"",
  ""command"": ""1\n2\n"",
  ""file"": ""c:\\file.txt"",
  ""arguments"": null
}";

            var testSubject = new CompilationDatabaseRequest(dbEntry, context, ValidEnvVars);

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                testSubject.WriteRequestDiagnostics(writer);
            };

            var actual = sb.ToString();
            actual.Should().Be(expected);
        }

        [TestMethod]
        public void EnvironmentVariables_ReturnsExpectedValues()
        {
            var envVars = new Dictionary<string, string> { { "INCLUDE", "" }, { "PATH", "any"} };
            var testSubject = new CompilationDatabaseRequest(ValidDbEntry, ValidContext, envVars);

            var actual = testSubject.EnvironmentVariables;

            actual.Count.Should().Be(2);
            actual.Keys.Should().BeEquivalentTo("INCLUDE", "PATH");
            actual["INCLUDE"].Should().BeEmpty();
            actual["PATH"].Should().Be("any");
        }

        private static RequestContext CreateContext(
            string language = "c",
            ICFamilyRulesConfig rulesConfig = null,
            string file = "file.txt",
            string pchFile = "pch.txt",
            CFamilyAnalyzerOptions analyzerOptions = null)
        {
            return new RequestContext(
                language: language,
                rulesConfig: rulesConfig ?? Mock.Of<ICFamilyRulesConfig>(),
                file: file,
                pchFile: pchFile,
                analyzerOptions: analyzerOptions, false);
        }

        /// <summary>
        /// Executes the request, and returns the ordered list of strings that were
        /// written to the binary stream
        /// </summary>
        private IList<string> WriteRequest(CompilationDatabaseEntry dbEntry, RequestContext context)
        {
            var tokens = new List<string>();

            var testSubject = new CompilationDatabaseRequest(dbEntry, context, ValidEnvVars);

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                {
                    testSubject.WriteRequest(writer);
                }
                stream.Flush();
                var endOfStreamPosition = stream.Position;
                stream.Position = 0;

                using (var reader = new BinaryReader(stream))
                {
                    while(stream.Position != endOfStreamPosition)
                    {
                        tokens.Add(Protocol.ReadUTF(reader));
                    }
                }
            }

            return tokens;
        }

        private void CheckExpectedSetting(IList<string> tokens, string key, string value)
        {
            var keyIndex = tokens.IndexOf(key);
            keyIndex.Should().NotBe(-1);
            keyIndex.Should().NotBe(tokens.Count - 2);
            tokens[keyIndex + 1].Should().Be(value);
        }

        private void CheckSettingDoesNotExist(IList<string> tokens, string key) =>
            tokens.IndexOf(key).Should().Be(-1);
    }
}
