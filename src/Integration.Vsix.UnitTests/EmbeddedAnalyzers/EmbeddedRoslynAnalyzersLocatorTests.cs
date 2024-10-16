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

using SonarLint.VisualStudio.Infrastructure.VS.Roslyn;
using SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.EmbeddedAnalyzers;

[TestClass]
public class EmbeddedRoslynAnalyzersLocatorTests
{
    private IVsixRootLocator vsixRootLocator;
    private EmbeddedRoslynAnalyzersLocator testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        vsixRootLocator = Substitute.For<IVsixRootLocator>();
        testSubject = new EmbeddedRoslynAnalyzersLocator(vsixRootLocator);
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
    public void GetPathToParentFolder_ReturnsCorrectLocationInsideVsix()
    {
        vsixRootLocator.GetVsixRoot().Returns(@"C:\SomePath");

        var result = testSubject.GetPathToParentFolder();

        result.Should().Be(@"C:\SomePath\EmbeddedRoslynAnalyzers");
    }
}
