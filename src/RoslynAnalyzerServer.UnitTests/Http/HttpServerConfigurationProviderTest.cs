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

using System.Net;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class HttpServerConfigurationProviderTest
{
    private HttpServerConfigurationProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize() => testSubject = new HttpServerConfigurationProvider();

    [TestMethod]
    public void MefCtor_IHttpServerConfigurationProvider_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<HttpServerConfigurationProvider, IHttpServerConfigurationProvider>();

    [TestMethod]
    public void MefCtor_IHttpServerConfigurationFactory_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<HttpServerConfigurationProvider, IHttpServerConfigurationFactory>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisHttpServer>();

    [TestMethod]
    public void Ctor_CurrentConfigurationNotNull() => testSubject.CurrentConfiguration.Should().NotBeNull();

    [TestMethod]
    public void Ctor_PropertiesAreInitialized()
    {
        VerifyValidPort(testSubject.CurrentConfiguration.Port);
        testSubject.CurrentConfiguration.Token.Should().NotBeNull();
    }

    [TestMethod]
    public void Port_MultipleCalls_ReturnsSameValue()
    {
        var port = testSubject.CurrentConfiguration.Port;

        testSubject.CurrentConfiguration.Port.Should().Be(port);
        testSubject.CurrentConfiguration.Port.Should().Be(port);
    }

    [TestMethod]
    public void Token_MultipleCalls_ReturnsSameValue()
    {
        var token = testSubject.CurrentConfiguration.Token;

        testSubject.CurrentConfiguration.Token.Should().Be(token);
        testSubject.CurrentConfiguration.Token.Should().Be(token);
    }

    [TestMethod]
    public void Token_HasLength32Bytes()
    {
        var token = testSubject.CurrentConfiguration.Token;

        Convert.FromBase64String(token.ToUnsecureString()).Length.Should().Be(32);
    }

    [TestMethod]
    public void SetNewConfiguration_UpdatesCurrentConfiguration()
    {
        var initialInstance = testSubject.CurrentConfiguration;

        var result = testSubject.SetNewConfiguration();

        result.Should().NotBeNull();
        result.Should().NotBeSameAs(initialInstance);
        testSubject.CurrentConfiguration.Should().BeSameAs(result);
    }

    [TestMethod]
    public void SetNewConfiguration_GeneratesDifferentProperties()
    {
        var originalConfiguration = testSubject.CurrentConfiguration;

        var newConfig = testSubject.SetNewConfiguration();

        newConfig.Port.Should().NotBe(originalConfiguration.Port);
        VerifyValidPort(newConfig.Port);
        newConfig.Token.Should().NotBe(originalConfiguration.Token);
    }

    [TestMethod]
    public void MapToInferredProperties_ReturnsExpectedProperties()
    {
        var portKey = "sonar.sqvsRoslynPlugin.internal.serverPort";
        var tokenKey = "sonar.sqvsRoslynPlugin.internal.serverToken";

        var analysisProperties = testSubject.CurrentConfiguration.MapToInferredProperties();

        analysisProperties.Count.Should().Be(2);
        analysisProperties.Should().ContainKey(portKey);
        analysisProperties.Should().ContainKey(tokenKey);
        analysisProperties[portKey].Should().Be(testSubject.CurrentConfiguration.Port.ToString());
        analysisProperties[tokenKey].Should().Be(testSubject.CurrentConfiguration.Token.ToUnsecureString());
    }

    private static void VerifyValidPort(int port)
    {
        port.Should().BeGreaterThan(IPEndPoint.MinPort);
        port.Should().BeLessThan(IPEndPoint.MaxPort);
    }
}
