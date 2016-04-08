//-----------------------------------------------------------------------
// <copyright file="RuleSetInformationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class RuleSetInformationTests
    {
        [TestMethod]
        public void RuleSetInformation_ArgChecks()
        {
            string projectFullName = "p";
            string baselineRuleSet = "br";
            string projectRuleSet = "pr";

            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(null, baselineRuleSet, projectRuleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(projectFullName, null, projectRuleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInformation(projectFullName, baselineRuleSet, null, null));

            Assert.IsNotNull(new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, null), "Not expecting this to fail, just to make the static analyzer happy");
            Assert.IsNotNull(new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, new string[0]), "Not expecting this to fail, just to make the static analyzer happy");
            Assert.IsNotNull(new RuleSetInformation(projectFullName, baselineRuleSet, projectRuleSet, new string[] { "file" }), "Not expecting this to fail, just to make the static analyzer happy");
        }
    }
}
