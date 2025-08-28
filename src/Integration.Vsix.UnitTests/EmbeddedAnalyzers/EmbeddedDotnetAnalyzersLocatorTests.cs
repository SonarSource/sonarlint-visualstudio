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
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.EmbeddedAnalyzers;

[TestClass]
public class EmbeddedDotnetAnalyzersLocatorTests
{
    private const string PathInsideVsix = "C:\\somePath";

    private static readonly string CSharpRegularAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.CSharp.dll");
    private static readonly string VbRegularAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.VisualBasic.dll");
    private static readonly string CSharpEnterpriseAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.CSharp.dll");
    private static readonly string VbEnterpriseAnalyzer = GetAnalyzerFullPath(PathInsideVsix, "SonarAnalyzer.Enterprise.VisualBasic.dll");
    private IFileSystemService fileSystem;
    private ILanguageProvider languageProvider;

    private EmbeddedDotnetAnalyzersLocator testSubject;
    private IVsixRootLocator vsixRootLocator;

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
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedDotnetAnalyzersLocator, IEmbeddedDotnetAnalyzersLocator>(
            MefTestHelpers.CreateExport<IVsixRootLocator>(),
            MefTestHelpers.CreateExport<ILanguageProvider>(),
            MefTestHelpers.CreateExport<IFileSystemService>());

    [TestMethod]
    public void MefCtor_IsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<EmbeddedDotnetAnalyzersLocator>();

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
    public void GetAnalyzerFullPathsByLanguage_BothEnterprise_GroupsEnterpriseDllsByLanguage()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetAnalyzerFullPathsByLanguage(new AnalyzerInfoDto(true, true)).Should().BeEquivalentTo(
            new Dictionary<RoslynLanguage, List<string>> { [Language.CSharp] = [CSharpRegularAnalyzer, CSharpEnterpriseAnalyzer], [Language.VBNET] = [VbRegularAnalyzer, VbEnterpriseAnalyzer] });
    }

    [TestMethod]
    public void GetAnalyzerFullPathsByLanguage_BothBasic_GroupsBasicDllsByLanguage()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetAnalyzerFullPathsByLanguage(new AnalyzerInfoDto(false, false)).Should().BeEquivalentTo(
            new Dictionary<RoslynLanguage, List<string>> { [Language.CSharp] = [CSharpRegularAnalyzer], [Language.VBNET] = [VbRegularAnalyzer] });
    }

    [TestMethod]
    public void GetAnalyzerFullPathsByLanguage_OnlyCsharpEnterprise_GroupsDllsByLanguage()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetAnalyzerFullPathsByLanguage(new AnalyzerInfoDto(true, false)).Should().BeEquivalentTo(
            new Dictionary<RoslynLanguage, List<string>> { [Language.CSharp] = [CSharpRegularAnalyzer, CSharpEnterpriseAnalyzer], [Language.VBNET] = [VbRegularAnalyzer] });
    }

    [TestMethod]
    public void GetAnalyzerFullPathsByLanguage_OnlyVbEnterprise_GroupsDllsByLanguage()
    {
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetAnalyzerFullPathsByLanguage(new AnalyzerInfoDto(false, true)).Should().BeEquivalentTo(
            new Dictionary<RoslynLanguage, List<string>> { [Language.CSharp] = [CSharpRegularAnalyzer], [Language.VBNET] = [VbRegularAnalyzer, VbEnterpriseAnalyzer] });
    }

    private static string GetAnalyzerFullPath(string pathInsideVsix, string analyzerFile) => Path.Combine(pathInsideVsix, analyzerFile);
}
