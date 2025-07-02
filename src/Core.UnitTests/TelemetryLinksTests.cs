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
    public void TrackLink_UrlWithoutQueryParameters_AddsUtmParameters()
    {
        const string url = "https://sonarcloud.io";
        const string content = "test-content";
        const string term = "test-term";

        var result = TelemetryLinks.Utm.Link(url, content, term);

        const string expectedUrl = "https://sonarcloud.io?utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term";
        result.Should().Be(expectedUrl);
    }

    [TestMethod]
    public void TrackLink_UrlWithExistingQueryParameters_AddsUtmParametersWithAmpersand()
    {
        const string url = "https://sonarcloud.io?existing=param";
        const string content = "test-content";
        const string term = "test-term";

        var result = TelemetryLinks.Utm.Link(url, content, term);

        const string expectedUrl = "https://sonarcloud.io?existing=param&utm_medium=referral&utm_source=sq-ide-product-visual-studio&utm_content=test-content&utm_term=test-term";
        result.Should().Be(expectedUrl);
    }
}
