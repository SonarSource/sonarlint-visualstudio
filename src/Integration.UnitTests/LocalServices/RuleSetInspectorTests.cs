//-----------------------------------------------------------------------
// <copyright file="RuleSetInspectorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class RuleSetInspectorTests
    {
        /* Notes: "By default" is referred to the way we create rulesets for projects when binding.
        Also, please read the comments inside FindConflictingRules to better understand the merge logic (if needed to)
        */
        private const int DefaultNumberOfRules = 3;

        private RuleSetInspector testSubject;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsShell shell;
        private ConfigurableVsGeneralOutputWindowPane outputPane;

        #region Test plumbing
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.outputPane = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);

            this.shell = new ConfigurableVsShell();
            this.shell.RegisterPropertyGetter((int)__VSSPROPID2.VSSPROPID_InstallRootDir, () => this.SonarQubeRuleSetFolder);
            this.serviceProvider.RegisterService(typeof(SVsShell), this.shell);
           
            this.testSubject = new RuleSetInspector(this.serviceProvider);

            if (!Directory.Exists(VsRuleSetsDirectory))
            {
                Directory.CreateDirectory(VsRuleSetsDirectory);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Catch release-build issues that would otherwise be ignored because Debug.Assert will not be called
            this.outputPane.AssertOutputStrings(0); 
        }
        #endregion

        #region Properties
        /// <summary>
        /// Simulates the solution level SonarQube folder in which we store the fetched rulesets
        /// </summary>
        private string SonarQubeRuleSetFolder
        {
            get
            {
                return this.TestContext.TestRunDirectory;
            }
        }

        /// <summary>
        /// Simulates the usages of the vs ruleset folder for the default rulesets that ship with VS
        /// </summary>
        private string VsRuleSetsDirectory
        {
            get
            {
                return Path.Combine(this.TestContext.TestRunDirectory, RuleSetInspector.DefaultVSRuleSetsFolder);
            }
        }
        #endregion

        #region Tests
        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_ProjectLevelOverridesOfTheSolutionRuleset()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                string solutionRuleSet = this.CreateCommonRuleSet().FilePath;
                temps.AddFile(solutionRuleSet, false);

                // Check all supported RuleAction values
                RuleAction[] unsupportedActions = new[] { RuleAction.Default };
                foreach (RuleAction includeAllAction in Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions))
                {
                    foreach (IncludeType includeType in Enum.GetValues(typeof(IncludeType)).OfType<IncludeType>())
                    {
                        this.TestContext.WriteLine("Running test case, Project Rules are {0}, SolutionInclude is {1}", includeAllAction, includeType);

                        RuleSet projectRuleSet = CreateProjectRuleSetWithInclude(DefaultNumberOfRules, solutionRuleSet, includeType, includeAllAction);
                        temps.AddFile(projectRuleSet.FilePath, false);

                        // Act
                        RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                        // Verify
                        if (includeAllAction == RuleAction.None)
                        {
                            AssertMissingRulesByFullIds(conflicts, projectRuleSet.Rules);
                            AssertNoWeakRules(conflicts);
                        }
                        else if (includeAllAction == RuleAction.Info)
                        {
                            AssertWeakRulesByFullIds(conflicts, projectRuleSet.Rules);
                            AssertNoMissingRules(conflicts);
                        }
                        else
                        {
                            AssertNoConflicts(conflicts);
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_VsIncludesCannotCreateConflictsByDefault()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                string solutionRuleSet = this.CreateCommonRuleSet().FilePath;
                temps.AddFile(solutionRuleSet, false);

                // Check all supported RuleAction values
                RuleAction[] unsupportedActions = new[] { RuleAction.Default };
                foreach (RuleAction includeAllAction in Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions))
                {
                    string vsRuleSetName = $"VsRuleSet{includeAllAction}.ruleset";
                    RuleSet vsRuleSet = CreateVsRuleSet(DefaultNumberOfRules, vsRuleSetName, includeAllAction);
                    temps.AddFile(vsRuleSet.FilePath, false);

                    RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet, IncludeType.AsRelativeToProject, RuleAction.Default, vsRuleSetName);
                    temps.AddFile(projectRuleSet.FilePath, false);

                    // Act
                    RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                    // Verify
                    AssertNoConflicts(conflicts);
                }
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_UserIncludesCannotCreateConflictsByDefault()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                string solutionRuleSet = this.CreateCommonRuleSet().FilePath;
                temps.AddFile(solutionRuleSet, false);

                // Check all supported RuleAction values
                RuleAction[] unsupportedActions = new[] { RuleAction.Default };
                foreach (RuleAction includeAllAction in Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions))
                {
                    RuleSet otherRuleSet = this.CreateCommonRuleSet($"User{includeAllAction}.ruleset", includeAllAction);
                    temps.AddFile(otherRuleSet.FilePath, false);

                    RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
                    temps.AddFile(projectRuleSet.FilePath, false);

                    // Act
                    RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                    // Verify 
                    AssertNoConflicts(conflicts);
                }
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_IncludeAllCannotCreateConflictsByDefault()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                RuleSet solutionRuleSet = this.CreateCommonRuleSet();
                temps.AddFile(solutionRuleSet.FilePath, false);

                // Check all supported RuleAction values
                RuleAction[] unsupportedActions = new[] { RuleAction.None, RuleAction.Default };
                foreach (RuleAction includeAllAction in Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions))
                {
                    // Include with <IncludeAll ... />
                    RuleSet otherRuleSet = this.CreateCommonRuleSet($"User{includeAllAction}.ruleset", RuleAction.Info);
                    otherRuleSet.IncludeAll = new IncludeAll(includeAllAction);
                    temps.AddFile(otherRuleSet.FilePath, false);

                    // Target with <IncludeAll ... />
                    RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Info);
                    projectRuleSet.IncludeAll = new IncludeAll(includeAllAction);
                    projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                    temps.AddFile(projectRuleSet.FilePath, false);

                    // Act
                    RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                    // Verify
                    AssertNoConflicts(conflicts);
                }
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_ComplexStructureButNoConflicts()
        {
            // Make sure that the solution level has only one ruleset, and all the other rulesets have DefaultNumberOfRules (>1).
            // This is mainly to check the internal implementation of the RuleSetInjector and its RuleInfoProvider.

            using (var temps = new TempFileCollection())
            {
                // Setup
                string solutionRuleSet = this.CreateCommonRuleSet(rules: 1).FilePath; 
                temps.AddFile(solutionRuleSet, false);

                // Modifies all the solution rules to Info (should not impact the result)
                RuleSet otherRuleSet = this.CreateCommonRuleSet("User.ruleset", RuleAction.Info);
                temps.AddFile(otherRuleSet.FilePath, false);

                // Modifies all the solution rules to None (should not impact the result)
                const string BuiltInRuleSetName = "NoneAllRules.ruleset";
                RuleSet vsRuleSet = CreateVsRuleSet(DefaultNumberOfRules, BuiltInRuleSetName, RuleAction.None);
                temps.AddFile(vsRuleSet.FilePath, false);

                // The project has 3 modification -> error, info and warning
                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(DefaultNumberOfRules, solutionRuleSet, IncludeType.AsIs, RuleAction.Hidden, otherRuleSet.FilePath);
                projectRuleSet.Rules[1].Action = RuleAction.Error;
                projectRuleSet.Rules[2].Action = RuleAction.Warning;
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                // Verify
                AssertNoConflicts(conflicts);
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_ComplexStructureWithConflicts()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                string solutionRuleSet = this.CreateCommonRuleSet().FilePath;
                temps.AddFile(solutionRuleSet, false);

                // Modifies all the solution rules to Info (should not impact the result)
                RuleSet otherRuleSet = this.CreateCommonRuleSet("User.ruleset", RuleAction.Info);
                temps.AddFile(otherRuleSet.FilePath, false);

                // Modifies all the solution rules to None (should not impact the result)
                const string BuiltInRuleSetName = "NoneAllRules.ruleset";
                RuleSet vsRuleSet = CreateVsRuleSet(DefaultNumberOfRules, BuiltInRuleSetName, RuleAction.None);
                temps.AddFile(vsRuleSet.FilePath, false);

                // The project has 3 modification -> error, info and warning
                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(DefaultNumberOfRules, solutionRuleSet, IncludeType.AsIs, RuleAction.Error, otherRuleSet.FilePath);
                projectRuleSet.Rules[1].Action = RuleAction.Info;
                projectRuleSet.Rules[2].Action = RuleAction.None;
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act 
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                // Verify [verify that having all that with all that extra noise, since the project level overrides two of the values there will be conflicts]
                AssertWeakRulesByFullIds(conflicts, new[] { projectRuleSet.Rules[1] });
                AssertMissingRulesByFullIds(conflicts, new[] { projectRuleSet.Rules[2] });
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_BaselineRuleSetWasRemoved()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                RuleSet solutionRuleSet = this.CreateCommonRuleSet();
                temps.AddFile(solutionRuleSet.FilePath, false);

                RuleSet otherRuleSet = this.CreateCommonRuleSet($"User.ruleset", rules: 2);
                temps.AddFile(otherRuleSet.FilePath, false);

                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, otherRuleSet.FilePath, IncludeType.AsIs);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                // Verify [deleting the baseline and not providing any replacements means that the delta rules are missing]
                AssertMissingRulesByFullIds(conflicts, new [] { solutionRuleSet.Rules.Last() });
                AssertNoWeakRules(conflicts);
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_BaselineRuleSetWasIncludedAsNone()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                RuleSet solutionRuleSet = this.CreateCommonRuleSet();
                temps.AddFile(solutionRuleSet.FilePath, false);

                RuleSet otherRuleSet = this.CreateCommonRuleSet($"User.ruleset", RuleAction.Info);
                temps.AddFile(otherRuleSet.FilePath, false);

                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
                projectRuleSet.RuleSetIncludes[0].Action = RuleAction.None;
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                // Verify [since baseline is None, the other rulesets actions are then ones being used i.e. Info instead of Warning]
                AssertWeakRulesByFullIds(conflicts, solutionRuleSet.Rules);
                AssertNoMissingRules(conflicts);
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_OtherRuleSetWasIncludedAtLowerStrictnessThanWarning()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                RuleSet solutionRuleSet = this.CreateCommonRuleSet();
                temps.AddFile(solutionRuleSet.FilePath, false);

                RuleSet otherRuleSet = this.CreateCommonRuleSet($"User.ruleset", RuleAction.Info);
                temps.AddFile(otherRuleSet.FilePath, false);

                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
                projectRuleSet.RuleSetIncludes[1].Action = RuleAction.Info;
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                // Verify [since included with info the user rule set will remain info and once merged will become warning)
                AssertNoConflicts(conflicts);
            }
        }

        [TestMethod]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_OtherRuleSetWasIncludedAtHigherStrictnessThanWarning()
        {
            using (var temps = new TempFileCollection())
            {
                // Setup
                RuleSet solutionRuleSet = this.CreateCommonRuleSet();
                temps.AddFile(solutionRuleSet.FilePath, false);

                RuleSet otherRuleSet = this.CreateCommonRuleSet($"User.ruleset", RuleAction.Info);
                temps.AddFile(otherRuleSet.FilePath, false);

                RuleSet projectRuleSet = CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
                projectRuleSet.RuleSetIncludes[1].Action = RuleAction.Error;
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);
                temps.AddFile(projectRuleSet.FilePath, false);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                // Verify [since included with Error the user rule set will become error and once merged will become error, not a conflict)
                AssertNoConflicts(conflicts);
            }
        }
        #endregion

        #region Helpers
        enum IncludeType { AsIs, AsRelativeToProject };

        private RuleSet CreateCommonRuleSet(string ruleSetFileName = "SonarQube.ruleset", RuleAction defaultAction = RuleAction.Warning, int rules = DefaultNumberOfRules)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = Path.Combine(this.SonarQubeRuleSetFolder, ruleSetFileName);
            ruleSet.WriteToFile(ruleSet.FilePath);

            return ruleSet;
        }

        private RuleSet CreateVsRuleSet(int rules, string ruleSetFileName, RuleAction defaultAction = RuleAction.Warning)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = Path.Combine(this.VsRuleSetsDirectory, ruleSetFileName);
            ruleSet.WriteToFile(ruleSet.FilePath);

            return ruleSet;
        }

        private static RuleSet CreateProjectRuleSetWithIncludes(int rules, string solutionRuleSetToInclude, IncludeType solutionIncludeType, RuleAction defaultAction = RuleAction.Warning, params string[] otherIncludes)
        {
            string projectFile = Path.GetTempFileName();
            string solutionInclude = solutionIncludeType == IncludeType.AsIs ? solutionRuleSetToInclude : PathHelper.CalculateRelativePath(projectFile, solutionRuleSetToInclude);
            string[] includes = new[] { solutionInclude };
            if ((otherIncludes?.Length ?? 0) > 0)
            {
                includes = includes.Concat(otherIncludes).ToArray();
            }

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules, includes);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = projectFile;
            ruleSet.WriteToFile(ruleSet.FilePath);

            return ruleSet;
        }

        private static RuleSet CreateProjectRuleSetWithInclude(int rules, string solutionRuleSetToInclude, IncludeType solutionIncludeType, RuleAction defaultAction)
        {
            return CreateProjectRuleSetWithIncludes(rules, solutionRuleSetToInclude, solutionIncludeType, defaultAction);
        }

        /// <summary>
        /// Changes the first n number of action in <paramref name="ruleSet"/>,
        /// where n is the number of items in <paramref name="modifyActions"/> array.
        /// </summary>
        private static void ChangeRuleActions(RuleSet ruleSet, params RuleAction[] modifyActions)
        {
            for (int i = 0; i < modifyActions.Length; i++)
            {
                ruleSet.Rules[i].Action = modifyActions[i];
            }
        }

        private static void AssertWeakRulesByFullIds(RuleConflictInfo info, IEnumerable<RuleReference> expectedRules)
        {
            string[] expectedFullRuleIds = expectedRules.Select(r => r.FullId).ToArray();
            var actualFullIds = info.WeakActionRules.Select(r => r.FullId).ToArray();
            CollectionAssert.AreEquivalent(expectedFullRuleIds, actualFullIds, "Actually weak: {0}", string.Join(", ", actualFullIds));
        }

        private static void AssertMissingRulesByFullIds(RuleConflictInfo info, IEnumerable<RuleReference> expectedRules)
        {
            string[] expectedFullRuleIds = expectedRules.Select(r => r.FullId).ToArray();
            var actualFullIds = info.MissingRules.Select(r => r.FullId).ToArray();
            CollectionAssert.AreEquivalent(expectedFullRuleIds, actualFullIds, "Actually missing: {0}", string.Join(", ", actualFullIds));
        }

        private static void AssertNoConflicts(RuleConflictInfo info)
        {
            AssertNoMissingRules(info);
            AssertNoWeakRules(info);
        }

        private static void AssertNoMissingRules(RuleConflictInfo info)
        {
            Assert.AreEqual(0, info.MissingRules.Count, "Actually missing: {0}", string.Join(", ", info.MissingRules.Select(r => r.FullId)));
        }

        private static void AssertNoWeakRules(RuleConflictInfo info)
        {
            Assert.AreEqual(0, info.WeakActionRules.Count, "Actually weak: {0}", string.Join(", ", info.WeakActionRules.Select(r => r.FullId)));
        }
        #endregion Helpers
    }

}
