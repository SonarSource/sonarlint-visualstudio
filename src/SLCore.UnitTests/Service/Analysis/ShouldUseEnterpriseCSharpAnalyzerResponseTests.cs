﻿/*
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


using Newtonsoft.Json;
using SonarLint.VisualStudio.SLCore.Service.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Service.Telemetry;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Service.Analysis;

[TestClass]
public class ShouldUseEnterpriseCSharpAnalyzerResponseTests
{
    [TestMethod]
    public void Serialize_AsExpected()
    {
        var testSubject = new ShouldUseEnterpriseCSharpAnalyzerResponse(true);
        const string expectedString = """
                                      {
                                        "shouldUseEnterpriseAnalyzer": true
                                      }
                                      """;

        var serializedString = JsonConvert.SerializeObject(testSubject, Formatting.Indented);

        serializedString.Should().Be(expectedString);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Deserialized_AsExpected(bool shouldUseEnterpriseAnalyzer)
    {
        var expected = new ShouldUseEnterpriseCSharpAnalyzerResponse(shouldUseEnterpriseAnalyzer);
        var serialized = $"{{\"shouldUseEnterpriseAnalyzer\":{shouldUseEnterpriseAnalyzer.ToString().ToLower()}}}";

        JsonConvert.DeserializeObject<ShouldUseEnterpriseCSharpAnalyzerResponse>(serialized).Should().BeEquivalentTo(expected);
    }
}