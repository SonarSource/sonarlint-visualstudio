/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal static class RoslynExportProfileHelper
    {
        public static RoslynExportProfile CreateExport(RuleSet ruleSet)
        {
            return CreateExport(ruleSet, Enumerable.Empty<PackageName>());
        }

        public static RoslynExportProfile CreateExport(RuleSet ruleSet, IEnumerable<PackageName> packages)
        {
            return CreateExport(ruleSet, Enumerable.Empty<PackageName>(), Enumerable.Empty<AdditionalFile>());
        }

        public static RoslynExportProfile CreateExport(RuleSet ruleSet, IEnumerable<PackageName> packages, IEnumerable<AdditionalFile> additionalFiles)
        {
            string xml = TestRuleSetHelper.RuleSetToXml(ruleSet);
            var ruleSetXmlDoc = new XmlDocument();
            ruleSetXmlDoc.LoadXml(xml);

            var export = new RoslynExportProfile
            {
                Configuration = new Configuration
                {
                    RuleSet = ruleSetXmlDoc.DocumentElement,
                    AdditionalFiles = additionalFiles.ToList()
                },
                Deployment = new Deployment
                {
                    NuGetPackages = packages.Select(x => new NuGetPackageInfo { Id = x.Id, Version = x.Version.ToNormalizedString() }).ToList()
                }
            };

            return export;
        }

        public static void AssertAreEqual(RoslynExportProfile expected, RoslynExportProfile actual)
        {
            actual.Version.Should().Be(expected.Version, "Unexpected export version");
            AssertConfigSectionEqual(expected.Configuration, actual.Configuration);
            AssertDeploymentSectionEqual(expected.Deployment, actual.Deployment);
        }

        private static void AssertConfigSectionEqual(Configuration expected, Configuration actual)
        {
            CollectionAssert.AreEqual(expected.AdditionalFiles, actual.AdditionalFiles, "Additional files differ");

            RuleSet expectedRuleSet = TestRuleSetHelper.XmlToRuleSet(expected.RuleSet.OuterXml);
            RuleSet actualRuleSet = TestRuleSetHelper.XmlToRuleSet(actual.RuleSet.OuterXml);
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet, "Rule sets differ");
        }

        private static void AssertDeploymentSectionEqual(Deployment expected, Deployment actual)
        {
            CollectionAssert.AreEqual(expected.NuGetPackages, actual.NuGetPackages, "NuGet package information differs");
        }
    }
}