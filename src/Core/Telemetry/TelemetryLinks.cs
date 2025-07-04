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

using System.Text;

namespace SonarLint.VisualStudio.Core.Telemetry;

public static class TelemetryLinks
{
    private const string SonarQubeCloudFreeSignUpId = "sonarqubeCloudFreeSignUp";
    private const string SonarQubeCloudFreeSignUpUrl = "https://www.sonarsource.com/products/sonarcloud/signup-free/";

    public static readonly TelemetryLink SonarQubeCloudFreeSignUpCreateNewConnection = new (
        SonarQubeCloudFreeSignUpId,
        SonarQubeCloudFreeSignUpUrl,
        new Utm("create-new-connection", "create-sonarqube-cloud-free-tier"));

    public static readonly TelemetryLink SonarQubeCloudFreeSignUpPromoteConnectedModeLanguages = new (
        SonarQubeCloudFreeSignUpId,
        SonarQubeCloudFreeSignUpUrl,
        new Utm("promote-connected-mode-languages", "create-sonarqube-cloud-free-tier"));

    public static readonly Utm SonarQubeCloudCreateEditConnectionGenerateToken = new("create-edit-sqc-connection", "generate-token");
    public static readonly Utm SonarQubeServerCreateEditConnectionGenerateToken = new("create-edit-sqs-connection", "generate-token");

    public record TelemetryLink(string Id, string Url, Utm Utm)
    {
        public string GetUtmLink => Utm == null ? Url : Utm.ToLink(Url);
    }

    public record Utm(
        string Content,
        string Term)
    {
        public const string Medium = "referral";
        public const string Source = "sq-ide-product-visual-studio";

        public string ToLink(string url)
        {
            var sb = new StringBuilder(url);
            sb.Append(url.Contains("?") ? "&" : "?");
            sb.Append("utm_medium=");
            sb.Append(Medium);
            sb.Append("&utm_source=");
            sb.Append(Source);
            sb.Append("&utm_content=");
            sb.Append(Content);
            sb.Append("&utm_term=");
            sb.Append(Term);
            return sb.ToString();
        }
    }
}
