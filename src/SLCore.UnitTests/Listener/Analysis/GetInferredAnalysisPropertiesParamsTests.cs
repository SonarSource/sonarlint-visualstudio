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

using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class GetInferredAnalysisPropertiesParamsTests
{
    [TestMethod]
    public void Deserialized_AsExpected()
    {
        var json = """
        {
          "configurationScopeId": "scope-123",
          "filePathsToAnalyze": [
            "file:///C:/project/file1.cs",
            "file:///C:/project/file2.cs"
          ]
        }
        """;
        var expected = new GetInferredAnalysisPropertiesParams(
            "scope-123",
            [
                new FileUri("file:///C:/project/file1.cs"),
                new FileUri("file:///C:/project/file2.cs")
            ]);

        var result = JsonConvert.DeserializeObject<GetInferredAnalysisPropertiesParams>(json);

        result.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<GetInferredAnalysisPropertiesParams>());
    }

    [TestMethod]
    public void Deserialized_NoFiles_AsExpected()
    {
        var json = """
        {
          "configurationScopeId": "scope-456",
          "filePathsToAnalyze": []
        }
        """;
        var expected = new GetInferredAnalysisPropertiesParams("scope-456", []);

        var result = JsonConvert.DeserializeObject<GetInferredAnalysisPropertiesParams>(json);

        result.Should().BeEquivalentTo(expected, options => options.ComparingByMembers<GetInferredAnalysisPropertiesParams>());
    }
}
