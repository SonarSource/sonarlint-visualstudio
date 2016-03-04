//-----------------------------------------------------------------------
// <copyright file="TestRuleSetHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet;
using SonarLint.VisualStudio.Integration.Service;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System;

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
            Assert.AreEqual(expected.Version, actual.Version, "Unexpected export version");
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
