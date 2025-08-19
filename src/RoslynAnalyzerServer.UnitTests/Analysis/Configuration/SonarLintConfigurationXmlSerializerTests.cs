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

using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class SonarLintConfigurationXmlSerializerTests
{
    private SonarLintConfigurationXmlSerializer testSubject = null!;

    [TestInitialize]
    public void TestInitialize() => testSubject = new SonarLintConfigurationXmlSerializer();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<SonarLintConfigurationXmlSerializer, ISonarLintConfigurationXmlSerializer>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SonarLintConfigurationXmlSerializer>();

    [TestMethod]
    public void Generate_Serialized_ReturnsExpectedXml()
    {
        var config = new SonarLintConfiguration
        {
            Settings =
            [
                new SonarLintKeyValuePair { Key = "sonar.cs.prop1", Value = "value 1" },
                new SonarLintKeyValuePair { Key = "sonar.cs.prop2", Value = "value 2" }
            ],
            Rules =
            [
                new SonarLintRule
                {
                    Key = "s222",
                    Parameters =
                    [
                        new SonarLintKeyValuePair { Key = "AAA", Value = "param value2" },
                        new SonarLintKeyValuePair { Key = "ZZZ", Value = "param value1" },
                    ]
                },
                new SonarLintRule
                {
                    Key = "s555",
                    Parameters =
                    [
                        new SonarLintKeyValuePair { Key = "x", Value = "y y" }
                    ]
                },
                new SonarLintRule
                {
                    Key = "s777",
                    Parameters = []
                },
                new SonarLintRule
                {
                    Key = "s888",
                    Parameters = null
                }
            ]
        };

        var serialized = testSubject.Serialize(config);

        serialized.Should().Be(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <AnalysisInput xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <Settings>
                <Setting>
                  <Key>sonar.cs.prop1</Key>
                  <Value>value 1</Value>
                </Setting>
                <Setting>
                  <Key>sonar.cs.prop2</Key>
                  <Value>value 2</Value>
                </Setting>
              </Settings>
              <Rules>
                <Rule>
                  <Key>s222</Key>
                  <Parameters>
                    <Parameter>
                      <Key>AAA</Key>
                      <Value>param value2</Value>
                    </Parameter>
                    <Parameter>
                      <Key>ZZZ</Key>
                      <Value>param value1</Value>
                    </Parameter>
                  </Parameters>
                </Rule>
                <Rule>
                  <Key>s555</Key>
                  <Parameters>
                    <Parameter>
                      <Key>x</Key>
                      <Value>y y</Value>
                    </Parameter>
                  </Parameters>
                </Rule>
                <Rule>
                  <Key>s777</Key>
                  <Parameters />
                </Rule>
                <Rule>
                  <Key>s888</Key>
                  <Parameters />
                </Rule>
              </Rules>
            </AnalysisInput>
            """);
    }
}
