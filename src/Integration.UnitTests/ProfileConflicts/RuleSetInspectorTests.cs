/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;
using SonarLint.VisualStudio.Integration.UnitTests.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class RuleSetInspectorTests
    {
        /* Notes: "By default" is referred to the way we create rulesets for projects when binding.
        Also, please read the comments inside FindConflictsCore to better understand the merge logic (if needed to)
        */
        private const int DefaultNumberOfRules = 3;

        private RuleSetInspector testSubject;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsShell shell;
        private ConfigurableVsOutputWindowPane outputPane;
        private TempFileCollection temporaryFiles;

        public RuleSetInspectorTests()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            this.outputPane = new ConfigurableVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);

            this.shell = new ConfigurableVsShell();
            this.shell.RegisterPropertyGetter((int)__VSSPROPID2.VSSPROPID_InstallRootDir, () => this.VsInstallRoot);
            this.serviceProvider.RegisterService(typeof(SVsShell), this.shell);

            this.testSubject = new RuleSetInspector(this.serviceProvider);

            Directory.CreateDirectory(this.VsRuleSetsDirectory);
            Directory.CreateDirectory(this.SonarQubeRuleSetFolder);
            Directory.CreateDirectory(this.ProjectRuleSetFolder);
            Directory.CreateDirectory(this.SolutionSharedRuleSetFolder);

            this.temporaryFiles = new TempFileCollection();
        }

        #region Properties
        /// <summary>
        /// Simulates the solution level SonarQube folder in which we store the fetched rulesets
        /// </summary>
        private string SonarQubeRuleSetFolder
        {
            get
            {
                return Path.Combine(TestHelper.GetDeploymentDirectory(), "S");
            }
        }

        /// <summary>
        /// Simulates the project level folder in which we store the project rulesets
        /// </summary>
        private string ProjectRuleSetFolder
        {
            get
            {
                return Path.Combine(TestHelper.GetDeploymentDirectory(), "P");
            }
        }

        /// <summary>
        /// Simulates the solution level folder in which the user stores the shared rulesets
        /// </summary>
        private string SolutionSharedRuleSetFolder
        {
            get
            {
                return Path.Combine(TestHelper.GetDeploymentDirectory(), "~");
            }
        }

        private string VsInstallRoot
        {
            get
            {
                return TestHelper.GetDeploymentDirectory();
            }
        }

        /// <summary>
        /// Simulates the usages of the vs ruleset folder for the default rulesets that ship with VS
        /// </summary>
        private string VsRuleSetsDirectory
        {
            get
            {
                return Path.Combine(this.VsInstallRoot, RuleSetInspector.DefaultVSRuleSetsFolder);
            }
        }
        #endregion

        #region FindConflictingRules Tests

        [Fact]
        public void FindConflictingRules_WithNullOrWhiteSpaceBaselineRuleSet_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act1 = () => this.testSubject.FindConflictingRules(null, "notnull", "notnull");
            Action act2 = () => this.testSubject.FindConflictingRules(string.Empty, "notnull", "notnull");
            Action act3 = () => this.testSubject.FindConflictingRules(" ", "notnull", "notnull");

            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baselineRuleSet");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baselineRuleSet");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baselineRuleSet");
        }


        [Fact]
        public void FindConflictingRules_WithNullOrWhiteSpaceTargetRuleSet_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act1 = () => this.testSubject.FindConflictingRules("notnull", null, "notnull");
            Action act2 = () => this.testSubject.FindConflictingRules("notnull", string.Empty, "notnull");
            Action act3 = () => this.testSubject.FindConflictingRules("notnull", " ", "notnull");

            // Assert
            act1.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("targetRuleSet");
            act2.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("targetRuleSet");
            act3.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("targetRuleSet");
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_ProjectLevelOverridesOfTheSolutionRuleset()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            // Check all supported RuleAction values
            foreach (RuleAction ruleAction in GetSupportedRuleActions())
            {
                foreach (IncludeType includeType in Enum.GetValues(typeof(IncludeType)).OfType<IncludeType>())
                {
                    // TODO: Amaury
                    //this.TestContext.WriteLine("Running test case, Project Rules are {0}, SolutionInclude is {1}", ruleAction, includeType);

                    RuleSet projectRuleSet = this.CreateProjectRuleSetWithInclude(DefaultNumberOfRules, solutionRuleSet.FilePath, includeType, ruleAction);

                    // Act
                    RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                    // Assert
                    if (ruleAction == RuleAction.None)
                    {
                        AssertMissingRulesByFullIds(conflicts, solutionRuleSet.Rules);
                        AssertNoWeakRules(conflicts);
                    }
                    else if (ruleAction == RuleAction.Info || ruleAction == RuleAction.Hidden)
                    {
                        AssertWeakRulesByFullIds(conflicts, projectRuleSet.Rules, solutionRuleSet);
                        AssertNoMissingRules(conflicts);
                    }
                    else
                    {
                        AssertNoConflicts(conflicts);
                    }
                }
            }
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_BaselineNoneRulesAreNotTreatedAsMissing()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet(rules:2);
            ChangeRuleActions(solutionRuleSet, RuleAction.None, RuleAction.None);
            solutionRuleSet.WriteToFile(solutionRuleSet.FilePath);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Default);
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert
            AssertNoConflicts(conflicts);
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_VsIncludesCannotCreateConflictsByDefault()
        {
            // Arrange
            string solutionRuleSet = this.CreateCommonRuleSet().FilePath;

            // Check all supported RuleAction values
            foreach (RuleAction includeAllAction in GetSupportedRuleActions())
            {
                string vsRuleSetName = $"VsRuleSet{includeAllAction}.ruleset";
                this.CreateVsRuleSet(DefaultNumberOfRules, vsRuleSetName, includeAllAction);

                RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet, IncludeType.AsRelativeToProject, RuleAction.Default, vsRuleSetName);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                // Assert
                AssertNoConflicts(conflicts);

            }
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_UserIncludesCannotCreateConflictsByDefault()
        {
            // Arrange
            string solutionRuleSet = this.CreateCommonRuleSet().FilePath;

            // Check all supported RuleAction values
            foreach (RuleAction includeAllAction in GetSupportedRuleActions())
            {
                RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User{includeAllAction}.ruleset", includeAllAction);

                RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

                // Assert
                AssertNoConflicts(conflicts);
            }
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_IncludeAllCannotCreateConflictsByDefault()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            // Check all supported RuleAction values
            foreach (RuleAction includeAllAction in GetSupportedImportRuleActions())
            {
                // Include with <IncludeAll ... />
                RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User{includeAllAction}.ruleset", RuleAction.Info);
                otherRuleSet.IncludeAll = new IncludeAll(includeAllAction);

                // Target with <IncludeAll ... />
                RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Info);
                projectRuleSet.IncludeAll = new IncludeAll(includeAllAction);
                projectRuleSet.WriteToFile(projectRuleSet.FilePath);

                // Act
                RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

                // Assert
                AssertNoConflicts(conflicts);
            }
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_ComplexStructureButNoConflicts()
        {
            // Make sure that the solution level has only one ruleset, and all the other rulesets have DefaultNumberOfRules (>1).
            // This is mainly to check the internal implementation of the RuleSetInjector and its RuleInfoProvider.

            // Arrange
            string solutionRuleSet = this.CreateCommonRuleSet(rules: 1, defaultAction: RuleAction.Hidden).FilePath;

            // Modifies all the solution rules to Info (should not impact the result)
            RuleSet otherRuleSet = this.CreateUserSharedRuleSet("User.ruleset", RuleAction.Info);

            // Modifies all the solution rules to None (should not impact the result)
            const string BuiltInRuleSetName = "NoneAllRules.ruleset";
            this.CreateVsRuleSet(DefaultNumberOfRules, BuiltInRuleSetName, RuleAction.None);

            // The project has 3 modification -> error, info and warning
            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(DefaultNumberOfRules, solutionRuleSet, IncludeType.AsIs, RuleAction.Hidden, otherRuleSet.FilePath, BuiltInRuleSetName);
            projectRuleSet.Rules[0].Action = RuleAction.Info;
            projectRuleSet.Rules[1].Action = RuleAction.Error;
            projectRuleSet.Rules[2].Action = RuleAction.Warning;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet, projectRuleSet.FilePath);

            // Assert
            AssertNoConflicts(conflicts);
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_ComplexStructureWithConflicts()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            // Modifies all the solution rules to Info (should not impact the result)
            RuleSet otherRuleSet = this.CreateUserSharedRuleSet("User.ruleset", RuleAction.Info);

            // Modifies all the solution rules to None (should not impact the result)
            const string BuiltInRuleSetName = "NoneAllRules.ruleset";
            this.CreateVsRuleSet(DefaultNumberOfRules, BuiltInRuleSetName, RuleAction.None);

            // The project has 3 modification -> error, info and warning
            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(DefaultNumberOfRules, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Error, otherRuleSet.FilePath, BuiltInRuleSetName);
            projectRuleSet.Rules[1].Action = RuleAction.Info;
            projectRuleSet.Rules[2].Action = RuleAction.None;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert [verify that having all that with all that extra noise, since the project level overrides two of the values there will be conflicts]
            AssertWeakRulesByFullIds(conflicts, new[] { projectRuleSet.Rules[1] }, solutionRuleSet);
            AssertMissingRulesByFullIds(conflicts, new[] { projectRuleSet.Rules[2] });
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_BaselineRuleSetWasRemoved()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet(rules: 3);

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", rules: 2);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, otherRuleSet.FilePath, IncludeType.AsIs);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert [deleting the baseline and not providing any replacements means that the delta rules are missing]
            AssertMissingRulesByFullIds(conflicts, new[] { solutionRuleSet.Rules.Last() });
            AssertNoWeakRules(conflicts);
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_BaselineRuleSetWasIncludedAsNone()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", RuleAction.Info);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
            FindInclude(projectRuleSet, solutionRuleSet.FilePath).Action = RuleAction.None;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert [since baseline is None, the other rulesets actions are then ones being used i.e. Info instead of Warning]
            AssertWeakRulesByFullIds(conflicts, solutionRuleSet.Rules, solutionRuleSet);
            AssertNoMissingRules(conflicts);
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_OtherRuleSetWasIncludedAtLowerStrictnessThanWarning()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", RuleAction.Info);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert [since included with info the user rule set will remain info and once merged will become warning)
            AssertNoConflicts(conflicts);
        }

        [Fact]
        public void RuleSetInspector_FindConflictingRules_RuleSetFileCustomization_OtherRuleSetWasIncludedAtHigherStrictnessThanWarning()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", RuleAction.Info);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsIs, RuleAction.Default, otherRuleSet.FilePath);
            FindInclude(projectRuleSet, otherRuleSet.FilePath).Action = RuleAction.Error;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Act
            RuleConflictInfo conflicts = this.testSubject.FindConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert [since included with Error the user rule set will become error and once merged will become error, not a conflict)
            AssertNoConflicts(conflicts);
        }
        #endregion

        #region FixConflictingRules Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void FixConflictingRules_WithNullOrEmptyOrWhiteSpaceBaselineRuleSetPath_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => this.testSubject.FixConflictingRules(value, "notnull", "notnull");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("baselineRuleSetPath");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void FixConflictingRules_WithNullOrEmptyOrWhiteSpaceTargetRuleSetPath_ThrowsArgumentNullException(string value)
        {
            // Arrange + Act
            Action act = () => this.testSubject.FixConflictingRules("notnull", value, "notnull");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("targetRuleSetPath");
        }


        [Fact]
        public void RuleSetInspector_FixConflictingRules_NoConflicts()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Default);

            // Sanity
            AssertNoConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Act
            FixedRuleSetInfo fixedInfo = this.testSubject.FixConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert
            RuleSet target = fixedInfo.FixedRuleSet;
            RuleSetAssert.AreEqual(projectRuleSet, target);
            VerifyFix(fixedInfo, expectedIncludesReset: 0, expectedRulesDeleted: 0);
        }

        [Fact]
        public void RuleSetInspector_FixConflictingRules_IncludeConflict()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet();

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", RuleAction.Info);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Default, otherRuleSet.FilePath);
            string relativeInclude = PathHelper.CalculateRelativePath(projectRuleSet.FilePath, solutionRuleSet.FilePath);
            FindInclude(projectRuleSet, relativeInclude).Action = RuleAction.Hidden;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Sanity
            AssertConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath, "Expected 3 weakened rules since solution include was set to Hidden");

            // Act
            FixedRuleSetInfo fixedInfo = this.testSubject.FixConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert
            VerifyFix(fixedInfo, expectedIncludesReset: 1, expectedRulesDeleted: 0);
            RuleSet fixedTarget = fixedInfo.FixedRuleSet;
            RuleSet expectedRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Warning, otherRuleSet.FilePath);
            RuleSetAssert.AreEqual(expectedRuleSet, fixedTarget, "Expected the include action to be fixed");
            VerifyFixedRuleSetIsNotPersisted(solutionRuleSet, projectRuleSet, fixedTarget);
        }

        [Fact]
        public void RuleSetInspector_FixConflictingRules_RuleOverrideConflict()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet(rules: 5);

            // Create less rules than in the solution rule set and create conflicts not in all of them
            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(4, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject);
            projectRuleSet.Rules[1].Action = RuleAction.Hidden;
            projectRuleSet.Rules[2].Action = RuleAction.Info;
            projectRuleSet.Rules[3].Action = RuleAction.None;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Sanity
            AssertConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath, "Expected 2 weakened rules and 1 missing due to rule overrides at the project level");

            // Act
            FixedRuleSetInfo fixedInfo = this.testSubject.FixConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert
            VerifyFix(fixedInfo, expectedIncludesReset: 1, expectedRulesDeleted: 3);
            RuleSet fixedTarget = fixedInfo.FixedRuleSet;
            RuleSet expectedRuleSet = this.CreateProjectRuleSetWithIncludes(1, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject);
            RuleSetAssert.AreEqual(expectedRuleSet, fixedTarget, "Expected the conflicting rules to be deleted");
            VerifyFixedRuleSetIsNotPersisted(solutionRuleSet, projectRuleSet, fixedTarget);
        }

        [Fact]
        public void RuleSetInspector_FixConflictingRules_AllAtOnce()
        {
            // Arrange
            RuleSet solutionRuleSet = this.CreateCommonRuleSet(defaultAction: RuleAction.Error);

            RuleSet otherRuleSet = this.CreateUserSharedRuleSet($"User.ruleset", RuleAction.Info);

            RuleSet projectRuleSet = this.CreateProjectRuleSetWithIncludes(1, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, RuleAction.Warning, otherRuleSet.FilePath);
            string relativeInclude = PathHelper.CalculateRelativePath(projectRuleSet.FilePath, solutionRuleSet.FilePath);
            FindInclude(projectRuleSet, relativeInclude).Action = RuleAction.Hidden;
            projectRuleSet.WriteToFile(projectRuleSet.FilePath);

            // Sanity
            AssertConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath, "Expected 3 weakened rules since solution include was set to Hidden (and also a rule override with a weaker action)");

            // Act
            FixedRuleSetInfo fixedInfo = this.testSubject.FixConflictingRules(solutionRuleSet.FilePath, projectRuleSet.FilePath);

            // Assert
            VerifyFix(fixedInfo, expectedIncludesReset: 1, expectedRulesDeleted: 1);
            RuleSet fixedTarget = fixedInfo.FixedRuleSet;
            RuleSet expectedRuleSet = this.CreateProjectRuleSetWithIncludes(0, solutionRuleSet.FilePath, IncludeType.AsRelativeToProject, otherIncludes: otherRuleSet.FilePath);
            RuleSetAssert.AreEqual(expectedRuleSet, fixedTarget, "Expected the include action to change to default and the conflicting rules to be removed");
            VerifyFixedRuleSetIsNotPersisted(solutionRuleSet, projectRuleSet, fixedTarget);
        }
        #endregion

        #region Other Tests

        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new RuleSetInspector(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void RuleSetInspector_IsBaselineWeakend()
        {
            // X -> Error
            RuleSetInspector.IsBaselineWeakend(RuleAction.Error, RuleAction.Error)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Warning, RuleAction.Error)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Info, RuleAction.Error)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Hidden, RuleAction.Error)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.None, RuleAction.Error)
                .Should().BeFalse();

            // X -> Warning
            RuleSetInspector.IsBaselineWeakend(RuleAction.Error, RuleAction.Warning)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Warning, RuleAction.Warning)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Info, RuleAction.Warning)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Hidden, RuleAction.Warning)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.None, RuleAction.Warning)
                .Should().BeFalse();

            // X -> Info
            RuleSetInspector.IsBaselineWeakend(RuleAction.Error, RuleAction.Info)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Warning, RuleAction.Info)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Info, RuleAction.Info)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Hidden, RuleAction.Info)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.None, RuleAction.Info)
                .Should().BeFalse();

            // X -> Hidden
            RuleSetInspector.IsBaselineWeakend(RuleAction.Error, RuleAction.Hidden)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Warning, RuleAction.Hidden)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Info, RuleAction.Hidden)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Hidden, RuleAction.Hidden)
                .Should().BeFalse();
            RuleSetInspector.IsBaselineWeakend(RuleAction.None, RuleAction.Hidden)
                .Should().BeFalse();

            // X -> None
            RuleSetInspector.IsBaselineWeakend(RuleAction.Error, RuleAction.None)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Warning, RuleAction.None)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Info, RuleAction.None)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.Hidden, RuleAction.None)
                .Should().BeTrue();
            RuleSetInspector.IsBaselineWeakend(RuleAction.None, RuleAction.None)
                .Should().BeFalse();
        }
        #endregion

        #region Helpers
        enum IncludeType { AsIs, AsRelativeToProject };

        private static void VerifyFix(FixedRuleSetInfo fixedInfo, int expectedIncludesReset, int expectedRulesDeleted)
        {
            fixedInfo.IncludesReset.Should().HaveCount(expectedIncludesReset, "Unexpected number if includes were reset");
            fixedInfo.RulesDeleted.Should().HaveCount(expectedRulesDeleted, "Unexpected number of rules were deleted");
        }

        private void AssertConflictsExpected(string baselineFilePath, string targetFilePath, string detailedFailMessage = "")
        {
            this.testSubject.FindConflictingRules(baselineFilePath, targetFilePath).HasConflicts
                .Should().BeTrue("Conflicts expected: " + detailedFailMessage);
        }

        private void AssertNoConflictsExpected(string baselineFilePath, string targetFilePath, string detailedFailMessage = "")
        {
            this.testSubject.FindConflictingRules(baselineFilePath, targetFilePath).HasConflicts
                .Should().BeFalse("Conflicts expected: " + detailedFailMessage);
        }

        private void VerifyFixedRuleSetIsNotPersisted(RuleSet solutionRuleSet, RuleSet projectRuleSet, RuleSet fixedRuleSet)
        {
            fixedRuleSet.FilePath.Should().Be(projectRuleSet.FilePath);

            // Assert that not persisted
            AssertConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath, "File was not expected to be persisted, so conflicts should remain as they were");

            // Write the file and re-check for conflicts
            fixedRuleSet.WriteToFile(fixedRuleSet.FilePath);
            AssertNoConflictsExpected(solutionRuleSet.FilePath, projectRuleSet.FilePath, "Conflicts were fixed and persisted");
        }

        private RuleSet CreateCommonRuleSet(string ruleSetFileName = "SonarQube.ruleset", RuleAction defaultAction = RuleAction.Warning, int rules = DefaultNumberOfRules)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = Path.Combine(this.SonarQubeRuleSetFolder, ruleSetFileName);
            ruleSet.WriteToFile(ruleSet.FilePath);

            this.temporaryFiles.AddFile(ruleSet.FilePath, false);
            return ruleSet;
        }

        private RuleSet CreateUserSharedRuleSet(string ruleSetFileName, RuleAction defaultAction = RuleAction.Warning, int rules = DefaultNumberOfRules)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = Path.Combine(this.SolutionSharedRuleSetFolder, ruleSetFileName);
            ruleSet.WriteToFile(ruleSet.FilePath);

            this.temporaryFiles.AddFile(ruleSet.FilePath, false);

            return ruleSet;
        }

        private RuleSet CreateVsRuleSet(int rules, string ruleSetFileName, RuleAction defaultAction = RuleAction.Warning)
        {
            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = Path.Combine(this.VsRuleSetsDirectory, ruleSetFileName);
            ruleSet.WriteToFile(ruleSet.FilePath);

            this.temporaryFiles.AddFile(ruleSet.FilePath, false);

            return ruleSet;
        }

        private RuleSet CreateProjectRuleSetWithIncludes(int rules, string solutionRuleSetToInclude, IncludeType solutionIncludeType, RuleAction defaultAction = RuleAction.Warning, params string[] otherIncludes)
        {
            string projectRuleSetFilePath = Path.Combine(this.ProjectRuleSetFolder, Guid.NewGuid() + ".ruleset");
            string solutionInclude = solutionIncludeType == IncludeType.AsIs ? solutionRuleSetToInclude : PathHelper.CalculateRelativePath(projectRuleSetFilePath, solutionRuleSetToInclude);
            string[] includes = new[] { solutionInclude };
            if ((otherIncludes?.Length ?? 0) > 0)
            {
                includes = includes.Concat(otherIncludes).ToArray();
            }

            RuleSet ruleSet = TestRuleSetHelper.CreateTestRuleSet(rules, includes);
            ruleSet.Rules.ToList().ForEach(r => r.Action = defaultAction);
            ruleSet.FilePath = projectRuleSetFilePath;
            ruleSet.WriteToFile(ruleSet.FilePath);

            this.temporaryFiles.AddFile(ruleSet.FilePath, false);

            return ruleSet;
        }

        private RuleSet CreateProjectRuleSetWithInclude(int rules, string solutionRuleSetToInclude, IncludeType solutionIncludeType, RuleAction defaultAction)
        {
            return this.CreateProjectRuleSetWithIncludes(rules, solutionRuleSetToInclude, solutionIncludeType, defaultAction);
        }

        private static RuleSetInclude FindInclude(RuleSet targetRuleSet, string includeFilePath)
        {
            return targetRuleSet.RuleSetIncludes.Single(inc => StringComparer.OrdinalIgnoreCase.Equals(inc.FilePath, includeFilePath));
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

        private static void AssertWeakRulesByFullIds(RuleConflictInfo info, IEnumerable<RuleReference> expectedRules, RuleSet baseline)
        {
            var expectedFullRuleIds = new HashSet<string>(expectedRules.Select(r => r.FullId));
            List<string> found = new List<string>();
            foreach(var keyValue in info.WeakerActionRules)
            {
                string ruleFullId = keyValue.Key.FullId;
                found.Add(ruleFullId);

                expectedFullRuleIds.Should().Contain(ruleFullId, "Unexpected weakened rule");
                RuleReference baselineRule;
                baseline.Rules.TryGetRule(ruleFullId, out baselineRule)
                    .Should().BeTrue("Test setup error: baseline doesn't contain the rule {0}", ruleFullId);
                keyValue.Value.Should().Be(baselineRule.Action, "Unexpected Action. Expecting the baseline rule action to be returned part of RuleConflictInfo");
            }

            info.WeakerActionRules.Should().HaveSameCount(expectedFullRuleIds, "Not all the expected weakened rule were found. Missing: {0}", string.Join(", ", expectedFullRuleIds.Except(found)));
            info.HasConflicts
                .Should().BeTrue("Expected weakened rules");
        }

        private static void AssertMissingRulesByFullIds(RuleConflictInfo info, IEnumerable<RuleReference> expectedRules)
        {
            string[] expectedFullRuleIds = expectedRules.Select(r => r.FullId).ToArray();
            var actualFullIds = info.MissingRules.Select(r => r.FullId).ToArray();
            actualFullIds.Should().Equal(expectedFullRuleIds, "Actually missing: {0}", string.Join(", ", actualFullIds));
            info.HasConflicts
                .Should().BeTrue("Expected missing rules");
        }

        private static void AssertNoConflicts(RuleConflictInfo info)
        {
            AssertNoMissingRules(info);
            AssertNoWeakRules(info);
            info.HasConflicts.Should().BeFalse("Not expecting conflicts");
        }

        private static void AssertNoMissingRules(RuleConflictInfo info)
        {
            info.MissingRules.Should().HaveCount(0, "Actually missing: {0}", string.Join(", ", info.MissingRules.Select(r => r.FullId)));
        }

        private static void AssertNoWeakRules(RuleConflictInfo info)
        {
            info.WeakerActionRules.Should().HaveCount(0, "Actually weak: {0}", string.Join(", ", info.WeakerActionRules.Keys.Select(r => r.FullId)));
        }

        private static IEnumerable<RuleAction> GetSupportedRuleActions()
        {
            RuleAction[] unsupportedActions = new[] { RuleAction.Default };
            return Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions);
        }

        private static IEnumerable<RuleAction> GetSupportedImportRuleActions()
        {
            RuleAction[] unsupportedActions = new[] { RuleAction.None, RuleAction.Default };
            return Enum.GetValues(typeof(RuleAction)).OfType<RuleAction>().Except(unsupportedActions);
        }
        #endregion Helpers
    }

}
