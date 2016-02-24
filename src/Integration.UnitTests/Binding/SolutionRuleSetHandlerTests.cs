//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Binding.RuleSetInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class SolutionRuleSetHandlerTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsGeneralOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private const string SolutionRoot = @"c:\solution";

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot,"xxx.sln"));
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane = new ConfigurableVsGeneralOutputWindowPane());
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
        }

        [TestMethod]
        public void SolutionRuleSetHandler_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetHandler(RuleSetGroup.VB, null, this.solutionMock, NoSolutionRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetHandler(RuleSetGroup.CSharp, this.projectSystemHelper, null, NoSolutionRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetHandler(RuleSetGroup.CSharp, this.projectSystemHelper, this.solutionMock, null));
        }

        [TestMethod]
        public void SolutionRuleSetHandler_Ctor()
        {
            // Setup + Act
            var testSubject = new SolutionRuleSetHandler(RuleSetGroup.VB, this.projectSystemHelper, this.solutionMock, NoSolutionRuleSet);

            // Verify
            Assert.AreSame(this.solutionMock, testSubject.Solution);
            Assert.AreEqual(this.solutionMock.FilePath, testSubject.ThreadSafeData.SolutionFullPath);
        }

        [TestMethod]
        public void SolutionRuleSetHandler_GetUpdatedRuleSet()
        {
            // Setup
            var testSubject = new SolutionRuleSetHandler(RuleSetGroup.VB, this.projectSystemHelper, this.solutionMock, (group, solution) => "newFile.ruleset");

            // Act
            string ruleSet = testSubject.GetUpdatedRuleSet();

            // Verify
            Assert.AreEqual("newFile.ruleset", ruleSet);
        }

        [TestMethod]
        public void SolutionRuleSetHandler_CommitRuleSet()
        {
            // Setup
            var testSubject = new SolutionRuleSetHandler(RuleSetGroup.VB, this.projectSystemHelper, this.solutionMock, NoSolutionRuleSet);
            string ruleSetFile = Path.Combine(SolutionRoot, "newFile.ruleset");

            // Act
            using (new AssertIgnoreScope()) // File doesn't really exists
            {
                testSubject.CommitRuleSet(ruleSetFile);
            }

            // Verify
            Assert.IsTrue(this.solutionItemsProject.Files.ContainsKey(ruleSetFile), "RuleSet file was not added to solution items folder");
        }

        #region Helpers
        private static string NoSolutionRuleSet(RuleSetGroup group, string solutionPath)
        {
            Assert.AreEqual(RuleSetGroup.VB, group);

            Assert.IsNotNull(solutionPath, "Not expected null");

            return null;
        }
        #endregion
    }
}
