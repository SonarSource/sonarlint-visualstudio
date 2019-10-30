/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using NuGet;
using SonarQube.Client.Messages;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal static class RoslynExportProfileHelper
    {
        public static RoslynExportProfileResponse CreateExport(RuleSet ruleSet)
        {
            return CreateExport(ruleSet, Enumerable.Empty<PackageName>());
        }

        public static RoslynExportProfileResponse CreateExport(RuleSet ruleSet, IEnumerable<PackageName> packages)
        {
            return CreateExport(ruleSet, Enumerable.Empty<PackageName>(), Enumerable.Empty<AdditionalFileResponse>());
        }

        public static RoslynExportProfileResponse CreateExport(RuleSet ruleSet, IEnumerable<PackageName> packages, IEnumerable<AdditionalFileResponse> additionalFiles)
        {
            string xml = TestRuleSetHelper.RuleSetToXml(ruleSet);
            var ruleSetXmlDoc = new XmlDocument();
            ruleSetXmlDoc.LoadXml(xml);

            var export = new RoslynExportProfileResponse
            {
                Configuration = new ConfigurationResponse
                {
                    RuleSet = ruleSetXmlDoc.DocumentElement,
                    AdditionalFiles = additionalFiles.ToList()
                },
                Deployment = new DeploymentResponse
                {
                    NuGetPackages = packages.Select(x => new NuGetPackageInfoResponse { Id = x.Id, Version = x.Version.ToNormalizedString() }).ToList()
                }
            };

            return export;
        }

        public static void AssertAreEqual(RoslynExportProfileResponse expected, RoslynExportProfileResponse actual)
        {
            actual.Version.Should().Be(expected.Version, "Unexpected export version");
            AssertConfigSectionEqual(expected.Configuration, actual.Configuration);
            AssertDeploymentSectionEqual(expected.Deployment, actual.Deployment);
        }

        private static void AssertConfigSectionEqual(ConfigurationResponse expected, ConfigurationResponse actual)
        {
            actual.AdditionalFiles.Should().BeEquivalentTo(expected.AdditionalFiles, "Additional files differ");

            RuleSet expectedRuleSet = TestRuleSetHelper.XmlToRuleSet(expected.RuleSet.OuterXml);
            RuleSet actualRuleSet = TestRuleSetHelper.XmlToRuleSet(actual.RuleSet.OuterXml);
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet, "Rule sets differ");
        }

        private static void AssertDeploymentSectionEqual(DeploymentResponse expected, DeploymentResponse actual)
        {
            actual.NuGetPackages.Should().BeEquivalentTo(expected.NuGetPackages, "NuGet package information differs");
        }
    }
}
