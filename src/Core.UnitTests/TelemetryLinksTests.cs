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

using SonarLint.VisualStudio.Core.Telemetry;

namespace SonarLint.VisualStudio.Core.UnitTests;

[TestClass]
public class TelemetryLinksTests
{
    [TestMethod]
    public void SonarQubeCloudFreeSignUpCreateNewConnection_HasCorrectProperties()
    {
        var telemetryLink = TelemetryLinks.SonarQubeCloudFreeSignUpCreateNewConnection;

        telemetryLink.Id.Should().Be("sonarqubeCloudFreeSignUp");
        telemetryLink.Url.Should().Be("https://www.sonarsource.com/products/sonarcloud/signup-free/");
        telemetryLink.Utm.Should().NotBeNull();
        telemetryLink.Utm.Content.Should().Be("create-new-connection");
        telemetryLink.Utm.Term.Should().Be("create-sonarqube-cloud-free-tier");
    }

    [TestMethod]
    public void SonarQubeCloudFreeSignUpPromoteConnectedModeLanguages_HasCorrectProperties()
    {
        var telemetryLink = TelemetryLinks.SonarQubeCloudFreeSignUpPromoteConnectedModeLanguages;

        telemetryLink.Id.Should().Be("sonarqubeCloudFreeSignUp");
        telemetryLink.Url.Should().Be("https://www.sonarsource.com/products/sonarcloud/signup-free/");
        telemetryLink.Utm.Should().NotBeNull();
        telemetryLink.Utm.Content.Should().Be("promote-connected-mode-languages");
        telemetryLink.Utm.Term.Should().Be("create-sonarqube-cloud-free-tier");
    }

    [TestMethod]
    public void SonarQubeCloudCreateEditConnectionGenerateToken_HasCorrectProperties()
    {
        var utm = TelemetryLinks.SonarQubeCloudCreateEditConnectionGenerateToken;

        utm.Should().NotBeNull();
        utm.Content.Should().Be("create-edit-sqc-connection");
        utm.Term.Should().Be("generate-token");
    }

    [TestMethod]
    public void SonarQubeServerCreateEditConnectionGenerateToken_HasCorrectProperties()
    {
        var utm = TelemetryLinks.SonarQubeServerCreateEditConnectionGenerateToken;

        utm.Should().NotBeNull();
        utm.Content.Should().Be("create-edit-sqs-connection");
        utm.Term.Should().Be("generate-token");
    }

    [TestMethod]
    public void TelemetryLink_GetUtmLink_WithUtm_ReturnsUtmLink()
    {
        var utm = new TelemetryLinks.Utm("test-content", "test-term");
        var telemetryLink = new TelemetryLinks.TelemetryLink("test-id", "https://sonarcloud.io", utm);

        var result = telemetryLink.GetUtmLink;

        result.Should().Be("https://sonarcloud.io?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term");
    }

    [TestMethod]
    public void TelemetryLink_GetUtmLink_WithoutUtm_ReturnsOriginalUrl()
    {
        var telemetryLink = new TelemetryLinks.TelemetryLink("test-id", "https://sonarcloud.io", null);

        var result = telemetryLink.GetUtmLink;

        result.Should().Be("https://sonarcloud.io");
    }

    [TestMethod]
    public void Utm_ToLink_WithUrlWithoutQueryParameters_AddsUtmParameters()
    {
        var utm = new TelemetryLinks.Utm("test-content", "test-term");
        var url = "https://sonarcloud.io/path";

        var result = utm.ToLink(url);

        result.Should().Be("https://sonarcloud.io/path?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term");
    }

    [TestMethod]
    public void Utm_ToLink_WithUrlWithExistingQueryParameters_AppendsUtmParameters()
    {
        var utm = new TelemetryLinks.Utm("test-content", "test-term");
        var url = "https://sonarcloud.io/path?existing=param";

        var result = utm.ToLink(url);

        result.Should().Be("https://sonarcloud.io/path?existing=param&utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term");
    }

    [TestMethod]
    public void Utm_ToLink_WithUrlWithMultipleQueryParameters_AppendsUtmParameters()
    {
        var utm = new TelemetryLinks.Utm("test-content", "test-term");
        var url = "https://sonarcloud.io/path?param1=value1&param2=value2";

        var result = utm.ToLink(url);

        result.Should().Be("https://sonarcloud.io/path?param1=value1&param2=value2&utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term");
    }

    [TestMethod]
    public void Utm_Constants_HaveCorrectValues()
    {
        TelemetryLinks.Utm.Medium.Should().Be("referral");
        TelemetryLinks.Utm.Source.Should().Be("sq-ide-product-visual-studio");
    }

    [TestMethod]
    public void Utm_ToLink_WithEmptyContent_AddsEmptyContentParameter()
    {
        var utm = new TelemetryLinks.Utm("", "test-term");
        var url = "https://sonarcloud.io";

        var result = utm.ToLink(url);

        result.Should().Be("https://sonarcloud.io?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=&utm_term=test-term");
    }

    [TestMethod]
    public void Utm_ToLink_WithEmptyTerm_AddsEmptyTermParameter()
    {
        var utm = new TelemetryLinks.Utm("test-content", "");
        var url = "https://sonarcloud.io";

        var result = utm.ToLink(url);

        result.Should().Be("https://sonarcloud.io?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=");
    }

    [TestMethod]
    public void SonarQubeCloudFreeSignUpCreateNewConnection_GetUtmLink_ReturnsCorrectUrl()
    {
        var result = TelemetryLinks.SonarQubeCloudFreeSignUpCreateNewConnection.GetUtmLink;

        result.Should().Be("https://www.sonarsource.com/products/sonarcloud/signup-free/?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=create-new-connection&utm_term=create-sonarqube-cloud-free-tier");
    }

    [TestMethod]
    public void SonarQubeCloudFreeSignUpPromoteConnectedModeLanguages_GetUtmLink_ReturnsCorrectUrl()
    {
        var result = TelemetryLinks.SonarQubeCloudFreeSignUpPromoteConnectedModeLanguages.GetUtmLink;

        result.Should().Be("https://www.sonarsource.com/products/sonarcloud/signup-free/?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=promote-connected-mode-languages&utm_term=create-sonarqube-cloud-free-tier");
    }
}
