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
    [TestClass]
    public class RuleSetSerializerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableFileSystem fileSystem;
        private TempFileCollection temporaryFiles;
        private RuleSetSerializer testSubject;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            this.temporaryFiles = new TempFileCollection(this.TestContext.TestRunDirectory, keepFiles: false);

            this.serviceProvider = new ConfigurableServiceProvider();

            this.fileSystem = new ConfigurableFileSystem();
            this.serviceProvider.RegisterService(typeof(IFileSystem), this.fileSystem);

            this.testSubject = new RuleSetSerializer(this.serviceProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ((IDisposable)this.temporaryFiles).Dispose();
        }
        #endregion

        #region Tests
        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(()=> this.testSubject.LoadRuleSet(null));
            Exceptions.Expect<ArgumentNullException>(()=> this.testSubject.LoadRuleSet(""));
            Exceptions.Expect<ArgumentNullException>(()=> this.testSubject.LoadRuleSet("\t\n"));
        }

        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet()
        {
            // Setup
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            ruleSet.WriteToFile(ruleSet.FilePath);
            this.fileSystem.RegisterFile(ruleSet.FilePath);

            // Act
            RuleSet loaded = this.testSubject.LoadRuleSet(ruleSet.FilePath);

            // Verify
            Assert.IsNotNull(loaded, "Expected to load a rule set file");
            RuleSetAssert.AreEqual(ruleSet, loaded, "Loaded unexpected rule set");
        }

        [TestMethod]
        public void RuleSetSerializer_LoadRuleSet_Failures()
        {
            // Setup
            string existingRuleSet = Path.Combine(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");
            this.temporaryFiles.AddFile(existingRuleSet, false);

            // Case 1: file not exists
            RuleSet missing = testSubject.LoadRuleSet(existingRuleSet);

            // Verify
            Assert.IsNull(missing, "Expected no ruleset to be loaded when the file is missing");

            // Case 2: file exists, badly formed rule set (invalid xml)
            File.WriteAllText(existingRuleSet, "<xml>");
            this.fileSystem.RegisterFile(existingRuleSet);

            // Act
            RuleSet loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Verify
            Assert.IsNull(loadedBad, "Expected no ruleset to be loaded when its invalid XML");

            // Case 3: file exists, invalid rule set format (no RuleSet element)
            string xml =
"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
"<Rules AnalyzerId=\"SonarLint.CSharp\" RuleNamespace=\"SonarLint.CSharp\">\n" +
"</Rules>\n";

            File.WriteAllText(existingRuleSet, xml);

            // Act
            loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Verify
            Assert.IsNull(loadedBad, "Expected no ruleset to be loaded when the file in not a valid rule set");

            // Case 4: file exists, invalid rule set data (Default is not a valid action for rule)
            xml =
"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
"<RuleSet Name=\"RS\" ToolsVersion=\"14.0\">\n" +
"<Rules AnalyzerId=\"SonarLint.CSharp\" RuleNamespace=\"SonarLint.CSharp\">\n" +
"       <Rule Id=\"S2360\" Action=\"Default\" />\n" +
"</Rules>\n" +
"</RuleSet>\n";

            File.WriteAllText(existingRuleSet, xml);

            // Act
            loadedBad = testSubject.LoadRuleSet(existingRuleSet);

            // Verify
            Assert.IsNull(loadedBad, "Expected no ruleset to be loaded when the file in not a valid rule set");
        }

        [TestMethod]
        public void RuleSetSerializer_WriteRuleSetFile_ArgChecks()
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(this.TestContext.TestRunDirectory, this.TestContext.TestName + ".ruleset");

            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.WriteRuleSetFile(null, @"c:\file.ruleSet"));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.WriteRuleSetFile(ruleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.WriteRuleSetFile(ruleSet, ""));
            Exceptions.Expect<ArgumentNullException>(() => this.testSubject.WriteRuleSetFile(ruleSet, "\t\n"));
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
