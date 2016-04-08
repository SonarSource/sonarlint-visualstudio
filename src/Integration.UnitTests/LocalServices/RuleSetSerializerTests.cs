//-----------------------------------------------------------------------
// <copyright file="RuleSetSerializerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom.Compiler;
using System.IO;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", 
        "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"", 
        Justification = "Not required for test projects, disposed on test cleanup.", 
        Scope = "type", 
        Target = "~T:SonarLint.VisualStudio.Integration.UnitTests.RuleSetSerializerTests")]
    [TestClass]
    public class RuleSetSerializerTests
    {
        private TempFileCollection temporaryFiles;
        private RuleSetSerializer testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            this.temporaryFiles = new TempFileCollection(this.TestContext.TestRunDirectory, keepFiles: false);
            this.testSubject = new RuleSetSerializer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ((IDisposable)this.temporaryFiles).Dispose();
        }
        #endregion

        #region Tests
        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet()
        {
            // Setup
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            ruleSet.WriteToFile(ruleSet.FilePath);

            // Act
            RuleSet loaded = this.testSubject.LoadRuleSet(ruleSet.FilePath);

            // Verify
            Assert.IsNotNull(loaded, "Expected to load a rule set file");
            RuleSetAssert.AreEqual(ruleSet, loaded, "Loaded unexpected rule set");
        }


        [TestMethod]
        public void RuleSetSerializer_WriteRuleSetFile()
        {
            // Setup
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            string expectedPath = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName + "Other.ruleset");

            // Act
            this.testSubject.WriteRuleSetFile(ruleSet, expectedPath);

            // Verify
            Assert.IsTrue(File.Exists(expectedPath), "File not exists where expected");
            Assert.IsFalse(File.Exists(ruleSet.FilePath), "Expected to save only to the specified file path");
            RuleSet loaded = RuleSet.LoadFromFile(expectedPath);
            RuleSetAssert.AreEqual(ruleSet, loaded, "Written unexpected rule set");
        }
        #endregion
    }
}
