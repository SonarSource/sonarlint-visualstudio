//-----------------------------------------------------------------------
// <copyright file="RuleConflictInfoTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class RuleConflictInfoTests
    {
        [TestMethod]
        public void RuleConflictInfo_Ctor_ArgChecks()
        {
            // Setup
            IEnumerable<RuleReference> ruleRefs = null;
            IDictionary<RuleReference, RuleAction> rulesMap = null;

            IEnumerable<RuleReference> ruleRefsNull = new RuleReference[0];
            IDictionary<RuleReference, RuleAction> rulesMapNull = new Dictionary<RuleReference, RuleAction>();

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefsNull, rulesMap));
            Exceptions.Expect<ArgumentNullException>(() => new RuleConflictInfo(ruleRefs, rulesMapNull));
        }
    }
}
