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

using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

[ExcludeFromCodeCoverage] // TODO by https://sonarsource.atlassian.net/browse/SLVS-2382: add tests for this class
public class DependencyRiskDetailsViewModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public string VulnerabilityId { get; } = "CVE-2023-44487";
    public string VulnerabilityDescription { get; } = "Uncontrolled Resource Consumption";
    public DependencyRiskImpactSeverity Severity { get; } = DependencyRiskImpactSeverity.Blocker;
    public string SeverityNumber { get; } = "1";
    public DependencyRiskStatus Status { get; } = DependencyRiskStatus.Accepted;
    public string Description { get; }
        = "Time-of-check Time-of-use (TOCTOU) Race Condition vulnerability in Apache Tomcat.\r\n\r\nThis issue affects Apache Tomcat: from 11.0.0-M1 through 11.0.1, from 10.1.0-M1 through 10.1.33, from 9.0.0.M1 through 9.0.97.\r\n\r\nThe mitigation for CVE-2024-50379 was incomplete.\r\n\r\nUsers running Tomcat on a case insensitive file system with the default servlet write enabled (readonly initialisation \r\nparameter set to the non-default value of false) may need additional configuration to fully mitigate CVE-2024-50379 depending on which version of Java they are using with Tomcat:\r\n- running on Java 8 or Java 11: the system property\u00a0sun.io.useCanonCaches must be explicitly set to false (it defaults to true)\r\n- running on Java 17: the\u00a0system property sun.io.useCanonCaches, if set, must be set to false\u00a0(it defaults to false)\r\n- running on Java 21 onwards: no further configuration is required\u00a0(the system property and the problematic cache have been removed)\r\n\r\nTomcat 11.0.3, 10.1.35 and 9.0.99 onwards will include checks that\u00a0sun.io.useCanonCaches is set appropriately before allowing the default servlet to be write enabled on a case insensitive file system. Tomcat will also set\u00a0sun.io.useCanonCaches to false by default where it can.";
    public string PackageName { get; } = "org.apache.tomcat.embed:tomcat-embed-core";
    public string PackageVersion { get; } = "9.0.70";
    public int ImpactScore { get; } = 7;
    public string ImpactDescription { get; } = "High";
}
