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
        public void TryGet_NoConfig_ReturnsNull()
        {
            const string fileName = "c:\\file.cpp";
            var compilationConfigProvider = CreateCompilationProvider(fileName, null);
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

            var actual = testSubject.TryGet(fileName, new CFamilyAnalyzerOptions());

            actual.Should().BeNull();
            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void TryGet_UnrecognizedLanguage_ReturnsNull()
        {
            const string fileName = "c:\\file.txt";
            var compilationConfigProvider = CreateCompilationProvider(fileName, new CompilationDatabaseEntry());
            var rulesConfigProvider = new Mock<ICFamilyRulesConfigProvider>();

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

            var actual = testSubject.TryGet(fileName, new CFamilyAnalyzerOptions());

            actual.Should().BeNull();
            compilationConfigProvider.VerifyAll();
            rulesConfigProvider.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public void TryGet_ValidFile_ReturnsExpectedValue()
        {
            const string fileName = "c:\\file.c";
            var compilationConfigProvider = CreateCompilationProvider(fileName,
                new CompilationDatabaseEntry { Command = "foo" });

            var rulesConfig = new DummyCFamilyRulesConfig("c");
            var rulesConfigProvider = CreateRulesProvider(SonarLanguageKeys.C, rulesConfig);

            var testSubject = CreateTestSubject(compilationConfigProvider.Object, rulesConfigProvider.Object);

            var analyzerOptions = new CFamilyAnalyzerOptions();
            var actual = testSubject.TryGet(fileName, analyzerOptions);

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
    }
}
