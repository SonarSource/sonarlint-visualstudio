/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

    private EmbeddedDotnetAnalyzersLocator testSubject;
    private IVsixRootLocator vsixRootLocator;
    private ILanguageProvider languageProvider;

    [TestInitialize]
    public void TestInitialize()
    {
        vsixRootLocator = Substitute.For<IVsixRootLocator>();
        fileSystem = Substitute.For<IFileSystemService>();
        languageProvider = Substitute.For<ILanguageProvider>();
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
    public void GetAnalyzerFullPathsByLanguage_ReturnsExpectedPaths()
    {
        languageProvider.RoslynLanguages.Returns([Language.CSharp, Language.VBNET]);
        fileSystem.Directory.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns([
            CSharpRegularAnalyzer,
            VbRegularAnalyzer,
            CSharpEnterpriseAnalyzer,
            VbEnterpriseAnalyzer
        ]);

        testSubject.GetAnalyzerFullPathsByLicensedLanguage().Should().BeEquivalentTo(
            new Dictionary<LicensedRoslynLanguage, List<string>>
            {
                [new LicensedRoslynLanguage(Language.CSharp, false)] = [CSharpRegularAnalyzer],
                [new LicensedRoslynLanguage(Language.CSharp, true)] = [CSharpRegularAnalyzer, CSharpEnterpriseAnalyzer],
                [new LicensedRoslynLanguage(Language.VBNET, false)] = [VbRegularAnalyzer],
                [new LicensedRoslynLanguage(Language.VBNET, true)] = [VbRegularAnalyzer, VbEnterpriseAnalyzer],
            });
    }

    private static string GetAnalyzerFullPath(string pathInsideVsix, string analyzerFile) => Path.Combine(pathInsideVsix, analyzerFile);
}
