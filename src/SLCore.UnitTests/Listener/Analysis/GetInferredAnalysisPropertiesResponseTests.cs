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

using SonarLint.VisualStudio.SLCore.Listener.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Listener.Analysis;

[TestClass]
public class GetInferredAnalysisPropertiesResponseTests
{
    [TestMethod]
    public void Serialized_AsExpected()
    {
        const string expected = """{"properties":{"prop1":"val1","prop2":"val2"}}""";
        var testSubject = new GetInferredAnalysisPropertiesResponse(new Dictionary<string, string> { { "prop1", "val1" }, { "prop2", "val2" } });

        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(testSubject);

        serialized.Should().Be(expected);
    }

    [TestMethod]
    public void Serialized_NoProperties_AsExpected()
    {
        const string expected = """{"properties":{}}""";
        var testSubject = new GetInferredAnalysisPropertiesResponse([]);

        var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(testSubject);

        serialized.Should().Be(expected);
    }
}
