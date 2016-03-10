//-----------------------------------------------------------------------
// <copyright file="ProjectRuleSetHandlerTests.cs" company="SonarSource SA and Microsoft Corporation">
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
    public class ProjectRuleSetHandlerTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsGeneralOutputWindowPane outputPane;
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
        }

        [TestMethod]
        public void ProjectRuleSetHandler_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetHandler(null, new ProjectMock("file"), new ConfigurationMock("config"), new PropertyMock("prop", null), NoProjectRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetHandler(this.projectSystemHelper, null, new ConfigurationMock("config"), new PropertyMock("prop", null), NoProjectRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetHandler(this.projectSystemHelper, new ProjectMock("file"), new ConfigurationMock("config"),null, NoProjectRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetHandler(this.projectSystemHelper, new ProjectMock("file"), new ConfigurationMock("config"), new PropertyMock("prop", null), null));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectRuleSetHandler(null, null, null, null, null));
        }

        [TestMethod]
        public void ProjectRuleSetHandler_Ctor()
        {
            // Setup
            var project = new ProjectMock("file");
            project.SetVBProjectKind();
            var config = new ConfigurationMock("config");
            var prop = new PropertyMock("prop", null) { Value = "otherFile" };

            // Act
            var testSubject = new ProjectRuleSetHandler(this.projectSystemHelper, project, config, prop, NoProjectRuleSet);

            // Verify
            Assert.AreSame(project, testSubject.Project);
            Assert.AreSame(config, testSubject.Configuration);
            Assert.AreSame(prop, testSubject.CodeAnalysisRuleSetProperty);
            Assert.AreEqual(project.FilePath, testSubject.ThreadSafeData.ProjectFullPath);
            Assert.AreEqual(config.ConfigurationName, testSubject.ThreadSafeData.ConfigurationName);
            Assert.AreEqual(prop.Value, testSubject.ThreadSafeData.CodeAnalysisRuleSetPropertyValue);
        }

        [TestMethod]
        public void ProjectRuleSetHandler_GetUpdatedRuleSet()
        {
            // Setup
            var project = new ProjectMock("file");
            project.SetCSProjectKind();
            var config = new ConfigurationMock("config");
            var prop = new PropertyMock("prop", null) { Value = "otherFile" };
            var testSubject = new ProjectRuleSetHandler(this.projectSystemHelper, project, config, prop, (g, p,c,v)=> "newRuleSet.ruleset");

            // Act
            string ruleSet = testSubject.GetUpdatedRuleSet();

            // Verify
            Assert.AreEqual("newRuleSet.ruleset", ruleSet);
        }

        [TestMethod]
        public void ProjectRuleSetHandler_CommitRuleSet()
        {
            // Setup
            var project = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\myProject.xxx"));
            project.SetCSProjectKind();
            var config = new ConfigurationMock("config");
            var prop = new PropertyMock("prop", null);
            var testSubject = new ProjectRuleSetHandler(this.projectSystemHelper, project, config, prop, NoProjectRuleSet);
            string ruleSetFile = Path.Combine(SolutionRoot, @"Project\newFile.ruleset");

            // Act
            using (new AssertIgnoreScope()) // File doesn't really exists
            {
                testSubject.CommitRuleSet(ruleSetFile);
            }

            // Verify
            Assert.IsTrue(project.Files.ContainsKey(ruleSetFile), "RuleSet file was not added to solution items folder");
            Assert.AreEqual("newFile.ruleset", prop.Value, "Expected relative to project rule set path");
        }

        #region Helpers
        private static string NoProjectRuleSet(RuleSetGroup group, string projectPath, string configuration, string currentRuleSet)
        {
            Assert.IsNotNull(projectPath, "Not expected null");

            return null;
        }
        #endregion
    }
}
