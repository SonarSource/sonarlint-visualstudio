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

using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Analysis;

[TestClass]
public class AnalysisConfigurationProviderListenerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<AnalysisConfigurationProviderListener, ISLCoreListener>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<AnalysisConfigurationProviderListener>();
    }

    [DataRow(null)]
    [DataRow("")]
    [DataRow("configScopeId")]
    [DataRow("configScopeId123")]
    [DataTestMethod]
    public void GetBaseDirAsync_AnyValue_ReturnsNull(string configScopeId)
    {
        var testSubject = new AnalysisConfigurationProviderListener();

        var result = testSubject.GetBaseDirAsync(new GetBaseDirParams(configScopeId)).Result;

        result.baseDir.Should().BeNull();
    }

    [DataRow(null, [new string[0]])]
    [DataRow("", [new string[0]])]
    [DataRow("configScopeId", [new[]{@"C:\file1"}])]
    [DataRow("configScopeId123", [new[]{@"C:\file1", @"D:\file"}])]
    [DataTestMethod]
    public void GetInferredAnalysisProperties_AnyValue_ReturnsEmptySet(string configScopeId, string[] files)
    {
        var testSubject = new AnalysisConfigurationProviderListener();

        var result = testSubject.GetInferredAnalysisPropertiesAsync(new GetInferredAnalysisPropertiesParams(configScopeId,
                files.Select(x => new FileUri(x)).ToList()))
            .Result;
        
        result.Should().BeEquivalentTo(new GetInferredAnalysisPropertiesResponse([]),config:options => options.ComparingByMembers<GetInferredAnalysisPropertiesResponse>());
    }
}
