/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.IO;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.EmbeddedAnalyzers;

[TestClass]
public class EmbeddedDotnetAnalyzersLocatorTests
{
    private const string PathInsideVsix = "C:\\somePath";

    private readonly string CSharpRegularAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.CSharp.dll");
    private readonly string VbRegularAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.VisualBasic.dll");
    private readonly string CSharpEnterpriseAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.CSharp.dll");
    private readonly string VbEnterpriseAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.VisualBasic.dll");

    private EmbeddedDotnetAnalyzersLocator testSubject;
    private IVsixRootLocator vsixRootLocator;
    private IFileSystemService fileSystem;
    private ILanguageProvider languageProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        vsixRootLocator = Substitute.For<IVsixRootLocator>();
        languageProvider = Substitute.For<ILanguageProvider>();
        languageProvider.RoslynLanguages.Returns([Language.CSharp, Language.VBNET]);
        fileSystem = Substitute.For<IFileSystemService>();
        testSubject = new EmbeddedDotnetAnalyzersLocator(vsixRootLocator, languageProvider, fileSystem);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedDotnetAnalyzersLocator, IEmbeddedDotnetAnalyzersLocator>(
            MefTestHelpers.CreateExport<IVsixRootLocator>(),
            MefTestHelpers.CreateExport<ILanguageProvider>(),
            MefTestHelpers.CreateExport<IFileSystemService>());
    }

    [TestMethod]
    public void MefCtor_IsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<EmbeddedDotnetAnalyzersLocator>();
    }

    [TestMethod]
    public void GetBasicAnalyzerFullPaths_AnalyzersExists_ReturnsFullPathsToAnalyzers()
    {
        string[] expectedPaths =
        [
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.1.dll"),
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.2.dll")
        ];
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedPaths);

        var paths = testSubject.GetBasicAnalyzerFullPaths();

        paths.Should().BeEquivalentTo(expectedPaths);
    }

    [TestMethod]
    public void GetBasicAnalyzerFullPaths_EnterpriseAnalyzersExists_Skips()
    {
        string[] filePaths =
        [
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.1.dll"),
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.2.dll")
        ];
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(filePaths);

        var paths = testSubject.GetBasicAnalyzerFullPaths();

        paths.Should().BeEquivalentTo(filePaths[0]);
    }

    [TestMethod]
    public void GetBasicAnalyzerFullPaths_SearchesForFilesInsideVsix()
    {
        vsixRootLocator.GetVsixRoot().Returns(PathInsideVsix);

        testSubject.GetBasicAnalyzerFullPaths();

        fileSystem.Directory.Received(1).GetFiles(Path.Combine(PathInsideVsix, "EmbeddedDotnetAnalyzerDLLs"), "SonarAnalyzer.*.dll");
    }

    [TestMethod]
    public void GetEnterpriseAnalyzerFullPaths_AnalyzersExists_ReturnsFullPathsToAnalyzers()
    {
        string[] expectedPaths =
        [
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.1.dll"),
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.2.dll")
        ];
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedPaths);

        var paths = testSubject.GetEnterpriseAnalyzerFullPaths();

        paths.Should().BeEquivalentTo(expectedPaths);
    }

    [TestMethod]
    public void GetEnterpriseAnalyzerFullPaths_EnterpriseAnalyzersExists_ReturnsFullPathsToAnalyzers()
    {
        string[] expectedPaths =
        [
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.1.dll"),
            GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.2.dll")
        ];
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(expectedPaths);

        var paths = testSubject.GetEnterpriseAnalyzerFullPaths();

        paths.Should().BeEquivalentTo(expectedPaths);
    }

    [TestMethod]
    public void GetAnalyzerFullPaths_SearchesForFilesInsideVsix()
    {
        vsixRootLocator.GetVsixRoot().Returns(PathInsideVsix);

        testSubject.GetEnterpriseAnalyzerFullPaths();

        fileSystem.Directory.Received(1).GetFiles(Path.Combine(PathInsideVsix, "EmbeddedDotnetAnalyzerDLLs"), "SonarAnalyzer.*.dll");
    }

    [TestMethod]
    public void GetBasicAnalyzerFullPathsByLanguage_GroupsDllsByLanguageAndFiltersEnterprise()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer
        ]);

        testSubject.GetBasicAnalyzerFullPathsByLanguage().Should().BeEquivalentTo(new Dictionary<Language, List<string>>
        {
            [Language.CSharp] = [CSharpRegularAnalyzer], [Language.VBNET] = [VbRegularAnalyzer]
        });
    }

    [TestMethod]
    public void GetBasicAnalyzerFullPathsByLanguage_IncludesAllLanguagesEvenWithNoAnalyzers()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([CSharpRegularAnalyzer]);

        testSubject.GetBasicAnalyzerFullPathsByLanguage().Should().BeEquivalentTo(new Dictionary<Language, List<string>> { [Language.CSharp] = [CSharpRegularAnalyzer], [Language.VBNET] = [] });
    }

    [TestMethod]
    public void GetEnterpriseAnalyzerFullPathsByLanguage_GroupsDllsByLanguageIncludingEnterprise()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetEnterpriseAnalyzerFullPathsByLanguage().Should().BeEquivalentTo(new Dictionary<Language, List<string>>
        {
            [Language.CSharp] = [CSharpRegularAnalyzer, CSharpEnterpriseAnalyzer], [Language.VBNET] = [VbRegularAnalyzer, VbEnterpriseAnalyzer]
        });
    }

    [TestMethod]
    public void GetEnterpriseAnalyzerFullPathsByLanguage_IncludesAllLanguagesEvenWithNoAnalyzers()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([VbEnterpriseAnalyzer]);

        testSubject.GetEnterpriseAnalyzerFullPathsByLanguage().Should().BeEquivalentTo(new Dictionary<Language, List<string>> { [Language.CSharp] = [], [Language.VBNET] = [VbEnterpriseAnalyzer] });
    }

    [TestMethod]
    public void GetEnterpriseAnalyzerFullPathsByLanguage_ExcludesLanguagesNotInRoslynLanguages()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);
        // Only C# is in the Roslyn languages, VB.NET is not
        languageProvider.RoslynLanguages.Returns([Language.CSharp]);

        testSubject.GetEnterpriseAnalyzerFullPathsByLanguage().Should().BeEquivalentTo(new Dictionary<Language, List<string>>
        {
            [Language.CSharp] = [CSharpRegularAnalyzer, CSharpEnterpriseAnalyzer]
        });
    }

    private static string GetAnalyzerFullPath(string pathInsideVsix, string analyzerFile)
    {
        return Path.Combine(pathInsideVsix, analyzerFile);
    }
}
