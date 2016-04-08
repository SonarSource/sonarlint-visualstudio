//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetConflictTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ProjectRuleSetConflictTests
    {
        [TestMethod]
        public void ProjectRuleSetConflict_ArgChecks()
        {
            var info = new RuleSetInformation("projectFullName", "baselineRuleSet", "projectRuleSet", new string[0]);
            var conflict = new RuleConflictInfo();

            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(null, info));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetConflict(conflict, null));

            Assert.IsNotNull(new ProjectRuleSetConflict(conflict, info), "Not expecting this to fail, just to make the static analyzer happy");
        }
    }
}
