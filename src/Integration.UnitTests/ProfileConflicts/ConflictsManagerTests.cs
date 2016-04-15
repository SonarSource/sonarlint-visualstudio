//-----------------------------------------------------------------------
// <copyright file="ConflictsManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConflictsManagerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectHelper;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfoProvider;
        private ConfigurableFileSystem fileSystem;
        private ConfigurableSolutionBindingSerializer solutionBinding;
        private ConfigurableRuleSetInspector inspector;
        private ConfigurableVsGeneralOutputWindowPane outputWindow;
        private DTEMock dte;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectHelper);

            this.ruleSetInfoProvider = new ConfigurableSolutionRuleSetsInformationProvider();
            this.ruleSetInfoProvider.SolutionRootFolder = this.TestContext.TestRunDirectory;
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfoProvider);

            this.fileSystem = new ConfigurableFileSystem();
            this.serviceProvider.RegisterService(typeof(IFileSystem), this.fileSystem);

            this.solutionBinding = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(ISolutionBindingSerializer), this.solutionBinding);

            this.inspector = new ConfigurableRuleSetInspector();
            this.serviceProvider.RegisterService(typeof(IRuleSetInspector), this.inspector);

            this.outputWindow = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindow);

            this.dte = new DTEMock();
            this.projectHelper.CurrentActiveSolution = new SolutionMock(dte);
        }

        [TestMethod]
        public void ConflictsManager_Ctor()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ConflictsManager(null));

            var testSubject = new ConflictsManager(this.serviceProvider);
            Assert.IsNotNull(testSubject, "Avoid code analysis warning when testSubject is unused");
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NotBound()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidProjects();
            this.solutionBinding.CurrentBinding = null;

            // Act + Verify
            Assert.AreEqual(0, testSubject.GetCurrentConflicts().Count, "Not expecting any conflicts since solution is not bound");
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoValidProjects()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();

            // Act + Verify
            Assert.AreEqual(0, testSubject.GetCurrentConflicts().Count, "Not expecting any conflicts since there are no projects");
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_MissingBaselineFile()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects();

            // Act + Verify
            Assert.AreEqual(0, testSubject.GetCurrentConflicts().Count, "Not expecting any conflicts since the solution baseline is missing");
            this.outputWindow.AssertOutputStrings(1);
            this.outputWindow.AssertMessageContainsAllWordsCaseSensitive(0,
                words: new[] { Constants.SonarQubeManagedFolderName },
                splitter: new[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar });
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoRuleSetDeclaration()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects();
            this.SetValidSolutionRuleSetPerProjectKind();

            // Act + Verify
            Assert.AreEqual(0, testSubject.GetCurrentConflicts().Count, "Not expecting any conflicts since there are no project rulesets specified");
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_NoConflicts()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects();
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();

            bool findConflictsWasCalled = false;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictsWasCalled = true;
                return new RuleConflictInfo();
            });

            // Act + Verify
            Assert.AreEqual(0, testSubject.GetCurrentConflicts().Count, "Not expecting any conflicts since there are no conflicts");
            this.outputWindow.AssertOutputStrings(0);
            Assert.IsTrue(findConflictsWasCalled);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_ExceptionDuringFindConflicts()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects(2);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();
            int findConflictCalls = 0;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictCalls++;
                throw new Exception($"Hello world{findConflictCalls} ");
            });

            // Act + Verify
            int conflicts = 0;
            using (new AssertIgnoreScope()) // Ignore asserts on exception
            {
                conflicts = testSubject.GetCurrentConflicts().Count;
            }

            Assert.AreEqual(0, conflicts, "Expect 0 conflicts since failed each time");
            this.outputWindow.AssertOutputStrings(2);
            this.outputWindow.AssertMessageContainsAllWordsCaseSensitive(0, new[] { "Hello", "world1" });
            this.outputWindow.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "Hello", "world2" });
            Assert.AreEqual(2, findConflictCalls);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects(4);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetSingleValidRuleSetDeclarationPerProject();
            int findConflictCalls = 0;
            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                findConflictCalls++;
                return CreateConflictsBasedOnNumberOfCalls(findConflictCalls);
            });

            // Act + Verify
            Assert.AreEqual(3, testSubject.GetCurrentConflicts().Count, "Expect 3 conflicts (1st call is not a conflict)");
            this.outputWindow.AssertOutputStrings(0);
            Assert.AreEqual(4, findConflictCalls);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts_WithoutAggregationApplied()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects(3);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetValidRuleSetDeclarationPerProjectAndConfiguration(
                useUniqueProjectRuleSets: true,
                configurationNames: new[] { "Debug", "Release" });

            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(1);
                return new RuleConflictInfo(temp.Rules); // Missing rules
            });

            // Act + Verify
            Assert.AreEqual(6, testSubject.GetCurrentConflicts().Count, "Expecting 6 conflicts (3 projects x 2 configuration)");
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ConflictsManager_GetCurrentConflicts_HasConflicts_WithAggregationApplied()
        {
            // Setup
            var testSubject = new ConflictsManager(this.serviceProvider);
            this.SetValidSolutionBinding();
            this.SetValidProjects(3);
            this.SetValidSolutionRuleSetPerProjectKind();
            IEnumerable<RuleSetDeclaration> knownDeclarations = this.SetValidRuleSetDeclarationPerProjectAndConfiguration(
              useUniqueProjectRuleSets: false,
              configurationNames: new[] { "Debug", "Release" });

            this.ConfigureInspectorWithConflicts(knownDeclarations, () =>
            {
                RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(1);
                return new RuleConflictInfo(temp.Rules); // Missing rules
            });

            // Act + Verify
            Assert.AreEqual(3, testSubject.GetCurrentConflicts().Count, "Expecting 3 conflicts (1 per project) since the ruleset declaration for project configuration are the same");
            this.outputWindow.AssertOutputStrings(0);
        }

        #region Helpers
        private void SetValidSolutionBinding()
        {
            this.solutionBinding.CurrentBinding = new BoundSonarQubeProject { ProjectKey = "ProjectKey" };
        }

        private void SetValidProjects(int numberOfProjects = 1)
        {
            List<ProjectMock> projects = new List<ProjectMock>();
            for(int i=0;i< numberOfProjects;i++)
            {
                ProjectMock project = this.dte.Solution.AddOrGetProject($@"X:\Solution\Project\Project{i}.csproj");
                project.SetCSProjectKind();
                projects.Add(project);
            }

            this.projectHelper.FilteredProjects = projects;
        }

        private void SetValidSolutionRuleSetPerProjectKind()
        {
            ISolutionRuleSetsInformationProvider rsInfoProvider = this.ruleSetInfoProvider;
            foreach (var project in this.projectHelper.FilteredProjects)
            {
                string suffix = SolutionBindingOperation.GetProjectRuleSetSuffix(ProjectBindingOperation.GetProjectGroup(project));
                string solutionRuleSet = rsInfoProvider.CalculateSolutionSonarQubeRuleSetFilePath(this.solutionBinding.CurrentBinding.ProjectKey, suffix);
                this.fileSystem.RegisterFile(solutionRuleSet);
            }
        }

        private IEnumerable<RuleSetDeclaration> SetSingleValidRuleSetDeclarationPerProject(string configurationName = "Configuration", bool useUniqueProjectRuleSets = false)
        {
            List<RuleSetDeclaration> declarations = new List<RuleSetDeclaration>();

            foreach (ProjectMock project in this.projectHelper.FilteredProjects.OfType<ProjectMock>())
            {
                var configuration = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(configuration);

                PropertyMock ruleSetProperty = configuration.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
                ruleSetProperty.Value = project.FilePath.ToUpperInvariant(); // Catch cases where file paths are compared without OrdinalIgnoreCase 
                if (useUniqueProjectRuleSets)
                {
                    ruleSetProperty.Value = Path.ChangeExtension(project.FilePath, configurationName + ".ruleSet");
                }

                PropertyMock ruleSetDirectories = configuration.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
                ruleSetDirectories.Value = ConflictsManager.CombineDirectories(new[] { this.TestContext.TestRunDirectory, "." });

                RuleSetDeclaration declaration = new RuleSetDeclaration(
                    project,
                    ruleSetProperty,
                    ruleSetProperty.Value.ToString().ToLowerInvariant(), // Catch cases where file paths are compared without OrdinalIgnoreCase
                    configuration.ConfigurationName,
                    ruleSetDirectories.Value.ToString().Split(';'));

                declarations.Add(declaration);

                this.ruleSetInfoProvider.RegisterProjectInfo(project, declaration);
            }

            return declarations;
        }

        private IEnumerable<RuleSetDeclaration> SetValidRuleSetDeclarationPerProjectAndConfiguration(bool useUniqueProjectRuleSets, params string[] configurationNames)
        {
            List<RuleSetDeclaration> declarations = new List<RuleSetDeclaration>();

            foreach (string configurationName in configurationNames)
            {
                declarations.AddRange(this.SetSingleValidRuleSetDeclarationPerProject(configurationName, useUniqueProjectRuleSets));
            }

            return declarations;
        }

        private void ConfigureInspectorWithConflicts(IEnumerable<RuleSetDeclaration> knownDeclarations, Func<RuleConflictInfo> conflictFactory)
        {
            this.inspector.FindConflictingRulesAction = (baseline, ruleset, directories) =>
            {
                Assert.IsNotNull(baseline);
                Assert.IsNotNull(ruleset);
                this.fileSystem.AssertFileExists(baseline);

                Assert.IsTrue(knownDeclarations.Any(dec =>
                {
                    if (dec.RuleSetPath == ruleset)
                    {
                        return new HashSet<string>(dec.RuleSetDirectories).SetEquals(directories);
                    }

                    return false;
                }));

                return conflictFactory.Invoke();
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability",
            "S1541:Methods should not be too complex",
            Justification = "False positive, nothing complex about it, should not impact Maintainability",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.UnitTests.ConflictsManagerTests.CreateConflictsBasedOnNumberOfCalls(System.Int32)~SonarLint.VisualStudio.Integration.ProfileConflicts.RuleConflictInfo")]
        private static RuleConflictInfo CreateConflictsBasedOnNumberOfCalls(int findConflictCalls)
        {
            RuleSet temp = TestRuleSetHelper.CreateTestRuleSet(2);
            Dictionary<RuleReference, RuleAction> map = new Dictionary<RuleReference, RuleAction>();

            switch (findConflictCalls)
            {
                case 1:
                    return new RuleConflictInfo(); // No conflicts

                case 2:
                    return new RuleConflictInfo(temp.Rules); // Missing rules

                case 3:
                    map[temp.Rules[0]] = RuleAction.Error;
                    return new RuleConflictInfo(map); // Weakened rules

                case 4:
                    map[temp.Rules[0]] = RuleAction.Error;
                    return new RuleConflictInfo(temp.Rules.Skip(1), map); // One of each

                default:
                    Assert.Fail("Called unexpected number of times");
                    return null;
            }
        }
        #endregion
    }
}
