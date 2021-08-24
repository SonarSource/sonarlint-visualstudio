/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.CFamily.CMake;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.CMake
{
    [TestClass]
    public class CompilationDatabaseRequestFactoryTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CompilationDatabaseRequestFactory, IRequestFactory>(null, new[]
            {
                MefTestHelpers.CreateExport<ICompilationConfigProvider>(Mock.Of<ICompilationConfigProvider>()),
                MefTestHelpers.CreateExport<ICFamilyRulesConfigProvider>(Mock.Of<ICFamilyRulesConfigProvider>())
            });
        }

        [TestMethod]
        public async Task TryGet_NoConfig_ReturnsNull()
        {
            const string fileName = "c:\\file.cpp";

            var compilationConfigProvider = CreateCompilationProvider(fileName, null);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

            var actual = await testSubject.TryCreateAsync(fileName, new CFamilyAnalyzerOptions());

            actual.Should().BeNull();
            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [Description("Check support for header files")]
        public async Task TryGet_LanguageCalculatedBasedOnCompilationEntry()
        {
            const string fileName = "c:\\file.h";

            var compilationDatabaseEntry = CreateCompilationDatabaseEntry("file.c");
            var compilationConfigProvider = CreateCompilationProvider(fileName, compilationDatabaseEntry);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);
            await testSubject.TryCreateAsync(fileName, new CFamilyAnalyzerOptions());

            compilationConfigProvider.VerifyAll();

            // When analyzing header files, the analyzed file will be ".h", which is not a known rules' language.
            // However, the compilation entry is a ".c" file, so we expect the code to calculate the rules based on the entry.
            rulesConfigProvider.Verify(x=> x.GetRulesConfiguration(SonarLanguageKeys.C), Times.Once());
        }

        [TestMethod]
        public async Task TryGet_UnrecognizedLanguage_ReturnsNull()
        {
            const string fileName = "c:\\file.txt";

            var compilationDatabaseEntry = CreateCompilationDatabaseEntry(fileName);
            var compilationConfigProvider = CreateCompilationProvider(fileName, compilationDatabaseEntry);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

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

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

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

        private static CompilationDatabaseRequestFactory CreateTestSubject(ICompilationConfigProvider compilationConfigProvider = null,
            ICFamilyRulesConfigProvider rulesConfigProvider = null)
        {
            compilationConfigProvider ??= Mock.Of<ICompilationConfigProvider>();
            rulesConfigProvider ??= Mock.Of<ICFamilyRulesConfigProvider>();

            return new CompilationDatabaseRequestFactory(compilationConfigProvider, rulesConfigProvider);
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

        private CompilationDatabaseEntry CreateCompilationDatabaseEntry(string filePath) =>
            new CompilationDatabaseEntry
            {
                File = filePath,
                Command = "cmd"
            };
    }
}
