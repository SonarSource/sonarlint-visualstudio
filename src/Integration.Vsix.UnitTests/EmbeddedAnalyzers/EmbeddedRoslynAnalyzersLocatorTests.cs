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

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.EmbeddedAnalyzers;

[TestClass]
public class EmbeddedRoslynAnalyzersLocatorTests
{
    private const string PathInsideVsix = "C:\\somePath";

    private EmbeddedRoslynAnalyzersLocator testSubject;
    private IVsixRootLocator vsixRootLocator;
    private IFileSystem fileSystem;

    [TestInitialize]
    public void TestInitialize()
    {
        vsixRootLocator = Substitute.For<IVsixRootLocator>();
        fileSystem = Substitute.For<IFileSystem>();
        testSubject = new EmbeddedRoslynAnalyzersLocator(vsixRootLocator, fileSystem);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<EmbeddedRoslynAnalyzersLocator, IEmbeddedRoslynAnalyzersLocator>(
            MefTestHelpers.CreateExport<IVsixRootLocator>());
    }

    [TestMethod]
    public void MefCtor_IsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<EmbeddedRoslynAnalyzersLocator>();
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

    private static string GetAnalyzerFullPath(string pathInsideVsix, string analyzerFile)
    {
        return Path.Combine(pathInsideVsix, analyzerFile);
    }
}
