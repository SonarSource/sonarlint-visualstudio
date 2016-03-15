//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOpearation.WriterTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    public partial class SolutionBindingOpearationTests
    {
        private static readonly IFormatProvider TestCulture = CultureInfo.InvariantCulture;

        #region Tests

        [TestMethod]
        public void SolutionBindingOpearation_PendWriteSolutionLevelRuleSet_NullArgumentChecks()
        {
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 1);

            Exceptions.Expect<ArgumentNullException>(()=>
            {
                testSubject.PendWriteSolutionLevelRuleSet(null, ruleSet, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.PendWriteSolutionLevelRuleSet(string.Empty, ruleSet, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.PendWriteSolutionLevelRuleSet("X:\\bob.sln", null, string.Empty);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                testSubject.PendWriteSolutionLevelRuleSet("X:\\bob.sln", ruleSet, null);
            });
        }

        [TestMethod]
        public void SolutionBindingOpearation_PendWriteSolutionLevelRuleSet_CreatesRuleSetDirectory()
        {
            const string solutionFullPath = @"X:\myDirectory\mySubDirectory\Solution.sln";
            const string ruleSetRoot = @"X:\myDirectory\mySubDirectory\" + Constants.SonarQubeManagedFolderName;

            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Test case 1: directory not exists
            this.sccFileSystem.AssertDirectoryNotExists(ruleSetRoot);

            // Act
            testSubject.PendWriteSolutionLevelRuleSet(solutionFullPath, new RuleSet("rule set"), "Debug");

            // Verify
            this.sccFileSystem.AssertDirectoryExists(ruleSetRoot);

            // Test case 2: directory exist (should not crash)
            this.ruleFS.ClearRuleSets();

            // Act
            testSubject.PendWriteSolutionLevelRuleSet(solutionFullPath, new RuleSet("rule set"), "Release");

            // Verify
            this.sccFileSystem.AssertDirectoryExists(ruleSetRoot);
        }

        [TestMethod]
        public void SolutionBindingOpearation_PendWriteSolutionLevelRuleSet_WritesRuleSetToCorrectPath()
        {
            const string solutionFullPath = @"X:\myDirectory\mySubDirectory\Solution.sln";
            const string expectedOutputPath = @"X:\myDirectory\mySubDirectory\" + Constants.SonarQubeManagedFolderName + @"\keyMySuffix.ruleset";

            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(numRules: 3);

            // Act
            testSubject.PendWriteSolutionLevelRuleSet(solutionFullPath, ruleSet, "MySuffix");

            // Verify
            this.ruleFS.AssertRuleSetNotExists(expectedOutputPath);

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify
            this.ruleFS.AssertRuleSetsAreEqual(expectedOutputPath, ruleSet);
        }

        #endregion
    }
}
