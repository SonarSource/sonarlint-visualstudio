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

using System.Threading.Tasks;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;

namespace SonarLint.VisualStudio.CFamily.CMake.UnitTests
{
    [TestClass]
    public class CMakeRequestFactoryTests
    {
        private static readonly IEnvironmentVarsProvider ValidEnvVarsProvider = CreateEnvVarsProvider(new Dictionary<string, string> { { "key", "value" } }).Object;
        private static readonly ICFamilyRulesConfigProvider ValidRulesConfigProvider_Cpp = CreateRulesProvider(SonarLanguageKeys.CPlusPlus, new DummyCFamilyRulesConfig((SonarLanguageKeys.CPlusPlus))).Object;
        private const string ValidFileName_Cpp = "any.cpp";
        private static readonly ICompilationConfigProvider ValidCompilationConfigProvider = CreateCompilationProvider(ValidFileName_Cpp, CreateCompilationDatabaseEntry(ValidFileName_Cpp)).Object;
        private static readonly CFamilyAnalyzerOptions ValidAnalyzerOptions = new CFamilyAnalyzerOptions();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CMakeRequestFactory, IRequestFactory>(
                MefTestHelpers.CreateExport<ICompilationConfigProvider>(),
                MefTestHelpers.CreateExport<ICFamilyRulesConfigProvider>(),
                MefTestHelpers.CreateExport<IEnvironmentVarsProvider>());
        }

        [TestMethod]
        public async Task TryGet_NoConfig_ReturnsNull()
        {
            const string fileName = "c:\\file.cpp";

            var compilationConfigProvider = CreateCompilationProvider(fileName, null);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object, ValidEnvVarsProvider);

            var actual = await testSubject.TryCreateAsync(fileName, new CFamilyAnalyzerOptions());

            actual.Should().BeNull();
            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_NoEnvVars_ReturnsNull()
        {
            var envVarsProvider = CreateEnvVarsProvider(null);
            var testSubject = CreateTestSubject(ValidCompilationConfigProvider, ValidRulesConfigProvider_Cpp, envVarsProvider.Object);

            var actual = await testSubject.TryCreateAsync(ValidFileName_Cpp, ValidAnalyzerOptions);

            actual.Should().BeNull();
            envVarsProvider.VerifyAll();
        }

        [TestMethod]
        public async Task TryGet_HasEnvVars_ReturnsExpectedValue()
        {
            var envVarsProvider = CreateEnvVarsProvider(new Dictionary<string, string>
            {
                { "key1", "value1"},
                { "INCLUDE", "some paths..." }
            });
            var testSubject = CreateTestSubject(ValidCompilationConfigProvider, ValidRulesConfigProvider_Cpp, envVarsProvider.Object);

            var actual = await testSubject.TryCreateAsync(ValidFileName_Cpp, ValidAnalyzerOptions);

            actual.Should().NotBeNull();
            envVarsProvider.VerifyAll();

            actual.EnvironmentVariables.Should().NotBeNull();
            actual.EnvironmentVariables.Should().HaveCount(2);
            actual.EnvironmentVariables["key1"].Should().Be("value1");
            actual.EnvironmentVariables["INCLUDE"].Should().Be("some paths...");
        }

        [TestMethod]
        [Description("Check support for header files")]
        public async Task TryGet_LanguageCalculatedBasedOnCompilationEntry()
        {
            const string fileName = "c:\\file.h";

            var compilationDatabaseEntry = CreateCompilationDatabaseEntry("file.c");
            var compilationConfigProvider = CreateCompilationProvider(fileName, compilationDatabaseEntry);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object, ValidEnvVarsProvider);
            await testSubject.TryCreateAsync(fileName, new CFamilyAnalyzerOptions());

            compilationConfigProvider.VerifyAll();

            // When analyzing header files, the analyzed file will be ".h", which is not a known rules' language.
            // However, the compilation entry is a ".c" file, so we expect the code to calculate the rules based on the entry.
            rulesConfigProvider.Verify(x=> x.GetRulesConfiguration(SonarLanguageKeys.C), Times.Once());
        }

        [TestMethod]
        [Description("Check support for header files")]
        [DataRow("c:\\file.h", true)]
        [DataRow("c:\\file.c", false)]
        public async Task TryGet_IsHeaderFileCalculatedCorrectly(string analyzedFilePath, bool expectedIsHeaderFile)
        {
            var compilationDatabaseEntry = CreateCompilationDatabaseEntry("file.c");
            var compilationConfigProvider = CreateCompilationProvider(analyzedFilePath, compilationDatabaseEntry);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object, ValidEnvVarsProvider);
            var request = await testSubject.TryCreateAsync(analyzedFilePath, new CFamilyAnalyzerOptions());

            // When analyzing header files, the analyzed file will be ".h" but the compilation entry is a ".c" file.
            // We expected the property IsHeaderFile to be calculated based of the analyzed file, and not the compilation entry
            request.Context.IsHeaderFile.Should().Be(expectedIsHeaderFile);
        }

        [TestMethod]
        public async Task TryGet_UnrecognizedLanguage_ReturnsNull()
        {
            const string fileName = "c:\\file.txt";

            var compilationDatabaseEntry = CreateCompilationDatabaseEntry(fileName);
            var compilationConfigProvider = CreateCompilationProvider(fileName, compilationDatabaseEntry);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object, ValidEnvVarsProvider);

            var actual = await testSubject.TryCreateAsync(fileName, new CFamilyAnalyzerOptions());

            actual.Should().BeNull();
            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task TryGet_ValidFile_ReturnsExpectedValue()
        {
            const string fileName = "c:\\file.c";

            var compilationDatabaseEntry = CreateCompilationDatabaseEntry(fileName);
            var compilationConfigProvider = CreateCompilationProvider(fileName, compilationDatabaseEntry);

            var rulesConfig = new DummyCFamilyRulesConfig(SonarLanguageKeys.C);
            var rulesConfigProvider = CreateRulesProvider(SonarLanguageKeys.C, rulesConfig);

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object, ValidEnvVarsProvider);

            var analyzerOptions = new CFamilyAnalyzerOptions();
            var actual = await testSubject.TryCreateAsync(fileName, analyzerOptions);

            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.VerifyAll();
            actual.Should().NotBeNull();
            actual.Context.File.Should().Be(fileName);
            actual.Context.PchFile.Should().Be(SubProcessFilePaths.PchFilePath);
            actual.Context.CFamilyLanguage.Should().Be(SonarLanguageKeys.C);
            actual.Context.AnalyzerOptions.Should().BeSameAs(analyzerOptions);
            actual.Context.RulesConfiguration.Should().BeSameAs(rulesConfig);
        }

        private static CMakeRequestFactory CreateTestSubject(
            ICompilationConfigProvider compilationConfigProvider = null,
            ICFamilyRulesConfigProvider rulesConfigProvider = null,
            IEnvironmentVarsProvider envVarsProvider = null)
        {
            compilationConfigProvider ??= Mock.Of<ICompilationConfigProvider>();
            rulesConfigProvider ??= Mock.Of<ICFamilyRulesConfigProvider>();
            envVarsProvider ??= Mock.Of<IEnvironmentVarsProvider>();

            return new CMakeRequestFactory(compilationConfigProvider, rulesConfigProvider, envVarsProvider);
        }

        private static Mock<ICompilationConfigProvider> CreateCompilationProvider(string fileName, CompilationDatabaseEntry entryToReturn)
        {
            var compilationConfigProvider = new Mock<ICompilationConfigProvider>();
            compilationConfigProvider.Setup(x => x.GetConfig(fileName)).Returns(entryToReturn);

            return compilationConfigProvider;
        }

        private static Mock<ICFamilyRulesConfigProvider> CreateRulesProvider(string languageKey, ICFamilyRulesConfig rulesConfig)
        {
            var rulesProvider = new Mock<ICFamilyRulesConfigProvider>();
            rulesProvider.Setup(x => x.GetRulesConfiguration(languageKey)).Returns(rulesConfig);
            return rulesProvider;
        }

        private static Mock<IEnvironmentVarsProvider> CreateEnvVarsProvider(IReadOnlyDictionary<string, string> envVars = null)
        {
            var envVarsProvider = new Mock<IEnvironmentVarsProvider>();
            envVarsProvider.Setup(x => x.GetAsync()).Returns(Task.FromResult(envVars));
            return envVarsProvider;
        }

        private static CompilationDatabaseEntry CreateCompilationDatabaseEntry(string filePath) =>
            new CompilationDatabaseEntry
            {
                File = filePath,
                Command = "cmd"
            };
    }
}
