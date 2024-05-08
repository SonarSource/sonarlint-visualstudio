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

using SonarLint.VisualStudio.CFamily.CMake;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class MacroEvaluatorTests
    {
        [TestMethod]
        [DataRow("", "not recognised")]
        [DataRow("wrongPrefix", "name")]
        [DataRow("wrongPrefix", "projectDir")]
        [DataRow("projectDir", "")] // projectDir is a name, not a prefix
        public void Evaluate_UnrecognizedParameters_Null(string macroPrefix, string macroName)
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext();

            var result = testSubject.TryEvaluate(macroPrefix, macroName, context);

            result.Should().BeNull();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Evaluate_NoMacroName_Null(string macroName)
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext();

            var result = testSubject.TryEvaluate(string.Empty, macroName, context);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Evaluate_WorkspaceRoot_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(rootDirectory: "rootDir");

            var result = testSubject.TryEvaluate(string.Empty, "workspaceRoot", context);

            result.Should().Be("rootDir");
        }

        [TestMethod]
        public void Evaluate_ProjectFile_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: "c:\\someFile.txt");

            var result = testSubject.TryEvaluate(string.Empty, "projectFile", context);

            result.Should().Be("c:\\someFile.txt");
        }

        [TestMethod]
        [DataRow("c:\\someFile.txt", "c:\\")]
        [DataRow("c:/someFile.txt", "c:\\")]
        [DataRow("c:\\projectDir\\someFile.txt", "c:\\projectDir")]
        [DataRow("c:\\projectDir\\sub\\someFile.txt", "c:\\projectDir\\sub")]
        [DataRow("someFile.txt", "")]
        public void Evaluate_ProjectDir_ExpectedValueReturned(string cmakeListsFilePath, string expected)
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: cmakeListsFilePath);

            var result = testSubject.TryEvaluate(string.Empty, "projectDir", context);

            result.Should().Be(expected);
        }

        [TestMethod]
        [DataRow("c:\\someFile.txt", "c:\\")]
        [DataRow("c:/someFile.txt", "c:\\")]
        [DataRow("c:\\projectDir\\someFile.txt", "projectDir")]
        [DataRow("c:\\projectDir\\sub\\someFile.txt", "sub")]
        [DataRow("a\\someFile.txt", "a")]
        [DataRow("a\\b\\someFile.txt", "b")]
        [DataRow("a/someFile.txt", "a")]
        public void Evaluate_ProjectDirName_ExpectedValueReturned(string cmakeListsFilePath, string expected)
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: cmakeListsFilePath);

            var result = testSubject.TryEvaluate(string.Empty, "projectDirName", context);

            result.Should().Be(expected);
        }

        [TestMethod]

        public void Evaluate_ProjectDirName_NoFolder_Null()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: "no folder.txt");

            var result = testSubject.TryEvaluate(string.Empty, "projectDirName", context);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Evaluate_ThisFile_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(settingsJson: "c:\\someFile.txt");

            var result = testSubject.TryEvaluate(string.Empty, "thisFile", context);

            result.Should().Be("c:\\someFile.txt");
        }

        [TestMethod]
        [DataRow("c:\\someFile.txt", "c:\\")]
        [DataRow("c:/someFile.txt", "c:\\")]
        [DataRow("c:\\projectDir\\someFile.txt", "c:\\projectDir")]
        [DataRow("c:\\projectDir\\sub\\someFile.txt", "c:\\projectDir\\sub")]
        [DataRow("someFile.txt", "")]
        public void Evaluate_ThisFileDir_ExpectedValueReturned(string settingsFilePath, string expected)
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(settingsJson: settingsFilePath);

            var result = testSubject.TryEvaluate(string.Empty, "thisFileDir", context);

            result.Should().Be(expected);
        }

        [TestMethod]
        public void Evaluate_Name_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(activeConfig: "activeConfig");

            var result = testSubject.TryEvaluate(string.Empty, "name", context);

            result.Should().Be("activeConfig");
        }

        [TestMethod]
        public void Evaluate_Generator_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(generator: "dummy-generator");

            var result = testSubject.TryEvaluate(string.Empty, "generator", context);

            result.Should().Be("dummy-generator");
        }

        [TestMethod]
        public void Evaluate_WorkspaceHash_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: "c:\\aaa\\bbb\\foo.txt");

            var result = testSubject.TryEvaluate(string.Empty, "workspaceHash", context);

            result.Should().Be("d44ebc76-6f0d-4eb9-b244-2654f9acb2c1");
        }

        [TestMethod]
        public void Evaluate_ProjectHash_ExpectedValueReturned()
        {
            var testSubject = new MacroEvaluator();
            var context = CreateContext(cmakeLists: "c:\\aaa\\bbb\\foo.txt");

            var result = testSubject.TryEvaluate(string.Empty, "projectHash", context);

            result.Should().Be("d44ebc76-6f0d-4eb9-b244-2654f9acb2c1");
        }

        [TestMethod]
        [DataRow("env")]
        [DataRow("ENV")]
        [DataRow("Env")]
        public void Evaluate_EnvPrefix_EnvVariableReturned(string prefix)
        {
            var envVariableProvider = new Mock<IEnvironmentVariableProvider>();
            envVariableProvider.Setup(x => x.TryGet("var name")).Returns("some value");

            var testSubject = new MacroEvaluator(envVariableProvider.Object);
            var context = CreateContext();

            var result = testSubject.TryEvaluate(prefix, "var name", context);

            result.Should().Be("some value");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Evaluate_EnvPrefix_NoMacroName_Null(string macroName)
        {
            var envVariableProvider = new Mock<IEnvironmentVariableProvider>();

            var testSubject = new MacroEvaluator(envVariableProvider.Object);
            var context = CreateContext();

            var result = testSubject.TryEvaluate("env", macroName, context);

            result.Should().BeNull();

            envVariableProvider.Invocations.Count.Should().Be(0);
        }

        private EvaluationContext CreateContext(string activeConfig = "config",
            string rootDirectory = "root",
            string generator = "generator",
            string cmakeLists = "c:\\someFile.txt",
            string settingsJson = "c:\\someFile.txt") =>
            new(activeConfig, rootDirectory, generator, cmakeLists, settingsJson);
    }
}
