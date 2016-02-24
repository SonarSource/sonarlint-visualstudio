//-----------------------------------------------------------------------
// <copyright file="RuleSetInjectorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Binding.RuleSetInjection;
using System;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class RuleSetInjectorTests
    {
        private DTEMock dte; 
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsGeneralOutputWindowPane outputPane;
        private SolutionMock solutionMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte);
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane = new ConfigurableVsGeneralOutputWindowPane());
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
        }

        #region Test
        [TestMethod]
        public void RuleSetInjector_ArgChecks()
        {
            // Constructor checks
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInjector(null, NoSolutionRuleSet, NoProjectRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInjector(this.projectSystemHelper, null, NoProjectRuleSet));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, null));
            Exceptions.Expect<ArgumentNullException>(() => new RuleSetInjector(null, null, null));
        }

        [TestMethod]
        public void RuleSetInjector_InitializeHandlers_SingleManagedProject()
        {
            // Setup
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            project1.SetCSProjectKind();
            ProjectMock project3 = this.solutionMock.AddOrGetProject("project3");
            var managedProjects = new Project[] { project1 };
            this.projectSystemHelper.ManagedProjects = managedProjects;

            // Project1 - has configuration level property defined
            ConfigurationMock project1_config1 = new ConfigurationMock(nameof(project1_config1));
            project1_config1.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            ConfigurationMock project1_config2 = new ConfigurationMock(nameof(project1_config2));
            project1_config2.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            project1.ConfigurationManager.Configurations.AddRange(new[] { project1_config1, project1_config2 });

            // Act
            RuleSetInjector testSubject = new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, NoProjectRuleSet);

            // Verify
            var solutionHandlers = testSubject.Handlers.OfType<SolutionRuleSetHandler>().ToArray();
            Assert.AreEqual(1, solutionHandlers.Length, "Unexpected number of solution handlers (CS and VB expected)");
            Assert.AreSame(solutionHandlers[0], testSubject.Handlers[0], "Solution handler must be first in the list");
            Assert.AreSame(this.solutionMock, solutionHandlers[0].Solution, "Unexpected solution");

            var projectHandlers = testSubject.Handlers.OfType<ProjectRuleSetHandler>().ToArray();
            Assert.AreEqual(2 /*2 configuration level*/, projectHandlers.Length, "Unexpected number of project handlers");
            AssertHandlerExists(projectHandlers, project1, project1_config1);
            AssertHandlerExists(projectHandlers, project1, project1_config2);
        }

        [TestMethod]
        public void RuleSetInjector_InitializeHandlers_OneOfEachManagedProjectsTypes()
        {
            // Setup
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            project1.SetCSProjectKind();
            ProjectMock project2 = this.solutionMock.AddOrGetProject("project2");
            project2.SetVBProjectKind();
            ProjectMock project3 = this.solutionMock.AddOrGetProject("project3");
            var managedProjects = new Project[] { project1, project2 };
            this.projectSystemHelper.ManagedProjects = managedProjects;

            // Project1 - has configuration level property defined
            ConfigurationMock project1_config1 = new ConfigurationMock(nameof(project1_config1));
            project1_config1.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            ConfigurationMock project1_config2 = new ConfigurationMock(nameof(project1_config2));
            project1_config2.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            project1.ConfigurationManager.Configurations.AddRange(new[] { project1_config1, project1_config2 });

            // Project2 - has configuration level and project level property defined
            ConfigurationMock project2_config1 = new ConfigurationMock(nameof(project2_config1));
            project2_config1.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            ConfigurationMock project2_config2 = new ConfigurationMock(nameof(project2_config2));
            project2_config2.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            project2.ConfigurationManager.Configurations.AddRange(new[] { project2_config1, project2_config2 });
            project2.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);

            // Act
            RuleSetInjector testSubject = new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, NoProjectRuleSet);

            // Verify
            var solutionHandlers = testSubject.Handlers.OfType<SolutionRuleSetHandler>().ToArray();
            Assert.AreEqual(2, solutionHandlers.Length, "Unexpected number of solution handlers (CS and VB expected)");
            Assert.AreSame(solutionHandlers[0], testSubject.Handlers[0], "Solution handlers must precede the project handlers");
            Assert.AreSame(solutionHandlers[1], testSubject.Handlers[1], "Solution handlers must precede the project handlers");
            Assert.AreSame(this.solutionMock, solutionHandlers[0].Solution, "Unexpected solution");
            Assert.AreSame(this.solutionMock, solutionHandlers[1].Solution, "Unexpected solution");

            var projectHandlers = testSubject.Handlers.OfType<ProjectRuleSetHandler>().ToArray();
            Assert.AreEqual(3 /*2 configuration level + 1 project level*/, projectHandlers.Length, "Unexpected number of project handlers");
            AssertHandlerExists(projectHandlers, project1, project1_config1);
            AssertHandlerExists(projectHandlers, project1, project1_config2);
            AssertHandlerExists(projectHandlers, project2, null);
        }

        [TestMethod]
        public void RuleSetInjector_PrepareUpdates()
        {
            // Setup
            RuleSetInjector testSubject = new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, NoProjectRuleSet);
            testSubject.Handlers.Clear();
            var handler1 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file1" };
            testSubject.Handlers.Add(handler1);
            var handler2 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file2" };
            testSubject.Handlers.Add(handler2);
            var handler3 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = null };
            testSubject.Handlers.Add(handler3);
            var handler4 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = " \t" };
            testSubject.Handlers.Add(handler3);

            // Act
            testSubject.PrepareUpdates(CancellationToken.None);

            // Verify
            Assert.AreEqual(2, testSubject.Updates.Count, "Unexpected number of updates. Handlers returning null or whitespace should be ignored");
            AssertUpdateExists(testSubject, handler1, "file1");
            AssertUpdateExists(testSubject, handler2, "file2");
            handler1.AssertGetUpdatedRuleSetCoreCalled(1);
            handler2.AssertGetUpdatedRuleSetCoreCalled(1);
        }

        [TestMethod]
        public void RuleSetInjector_PrepareUpdates_Cancellation()
        {
            // Setup
            RuleSetInjector testSubject = new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, NoProjectRuleSet);
            testSubject.Handlers.Clear();
            CancellationTokenSource cts = new CancellationTokenSource();
            var handler1 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file1", GetUpdatedRuleSetCoreAction = cts.Cancel };
            testSubject.Handlers.Add(handler1);
            var handler2 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file2" };
            testSubject.Handlers.Add(handler2);

            // Act
            testSubject.PrepareUpdates(cts.Token);

            // Verify
            Assert.AreEqual(1, testSubject.Updates.Count, "Unexpected number of updates. Last handler wasn't expected to run");
            AssertUpdateExists(testSubject, handler1, "file1");
            handler1.AssertGetUpdatedRuleSetCoreCalled(1);
            handler2.AssertGetUpdatedRuleSetCoreCalled(0);
        }

        [TestMethod]
        public void RuleSetInjector_CommitUpdates()
        {
            // Setup
            RuleSetInjector testSubject = new RuleSetInjector(this.projectSystemHelper, NoSolutionRuleSet, NoProjectRuleSet);
            testSubject.Handlers.Clear();
            var handler1 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file1" };
            testSubject.Handlers.Add(handler1);
            var handler2 = new TestRuleSetHandler(this.projectSystemHelper) { GetUpdatedRuleSetCoreResult = "file2" };
            testSubject.Handlers.Add(handler2);
            testSubject.PrepareUpdates(CancellationToken.None);

            // Act
            testSubject.CommitUpdates();

            // Verify
            handler1.AssertCommitRuleSetCoreCalled(1);
            handler2.AssertCommitRuleSetCoreCalled(1);
        }
        #endregion

        #region Helpers
        private static void AssertUpdateExists(RuleSetInjector testSubject, TestRuleSetHandler expectedHandler, string expectedUpdatedFile)
        {
            RuleSetHandlerBase hanlder;
            Assert.IsTrue(testSubject.Updates.TryGetValue(expectedUpdatedFile, out hanlder), "No updated found for:{0}", expectedUpdatedFile);
            Assert.AreSame(expectedHandler, hanlder, "Unexpected handler handling the update");
        }

        private static void AssertHandlerExists(ProjectRuleSetHandler[] allHandlers, Project project, Configuration configuration)
        {
            ProjectRuleSetHandler handler = allHandlers.Where(h => h.Project == project && h.Configuration == configuration).SingleOrDefault();
            Assert.IsNotNull(handler, "Handler not found for project: {0}, configuration: {1}", project.FileName, configuration?.ConfigurationName?? "null");
            Assert.IsNotNull(handler.CodeAnalysisRuleSetProperty, "Property not found");
        }

        private static string NoSolutionRuleSet(RuleSetGroup group, string solutionPath)
        {
            Assert.IsNotNull(solutionPath, "Not expected null");

            return null;
        }

        private static string NoProjectRuleSet(RuleSetGroup group, string projectPath, string configuration, string currentRuleSet)
        {
            Assert.IsNotNull(projectPath, "Not expected null");

            return null;
        }

        private class TestRuleSetHandler : RuleSetHandlerBase
        {
            private int commitRuleSetCoreCalled = 0;
            private int getUpdatedRuleSetCoreCalled = 0;

            public TestRuleSetHandler(ConfigurableVsProjectSystemHelper projectSystemHelper)
                : base(projectSystemHelper)
            {
            }

            #region RuleSetHandlerBase
            protected override void CommitRuleSetCore(string ruleSetFullFilePath)
            {
                this.commitRuleSetCoreCalled++;
            }

            protected override string GetUpdatedRuleSetCore()
            {
                this.getUpdatedRuleSetCoreCalled++;
                this.GetUpdatedRuleSetCoreAction?.Invoke();
                return this.GetUpdatedRuleSetCoreResult;
            }
            #endregion

            #region Test helpers
            public string GetUpdatedRuleSetCoreResult
            {
                get;
                set;
            }

            public Action GetUpdatedRuleSetCoreAction
            {
                get;
                set;
            }

            public void AssertGetUpdatedRuleSetCoreCalled(int expectedNumberOfTimes)
            {
                Assert.AreEqual(this.getUpdatedRuleSetCoreCalled, expectedNumberOfTimes, "{0} was called unexpected number of times", nameof(GetUpdatedRuleSetCore));
            }

            public void AssertCommitRuleSetCoreCalled(int expectedNumberOfTimes)
            {
                Assert.AreEqual(this.commitRuleSetCoreCalled, expectedNumberOfTimes, "{0} was called unexpected number of times", nameof(CommitRuleSetCore));
            }
            #endregion
        }
        #endregion
    }
}
