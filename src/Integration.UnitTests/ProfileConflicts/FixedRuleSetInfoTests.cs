//-----------------------------------------------------------------------
// <copyright file="FixedRuleSetInfoTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests.ProfileConflicts
{
    [TestClass]
    public class FixedRuleSetInfoTests
    {
        [TestMethod]
        public void FixedRuleSetInfo_Ctor_ArgChecks()
        {
            // Setup
            IEnumerable<string> includesReset = new string[0];
            IEnumerable<string> rulesDeleted = new string[0];

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => new FixedRuleSetInfo(null, includesReset, rulesDeleted));
        }
    }
}
