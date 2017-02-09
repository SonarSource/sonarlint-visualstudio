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

using System;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public partial class ProjectBindingOperationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private const string SolutionRoot = @"c:\solution";
        private ConfigurableSolutionRuleStore ruleStore;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private ConfigurableRuleSetSerializer ruleSetFS;

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));
            this.outputPane = new ConfigurableVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.ruleStore = new ConfigurableSolutionRuleStore();
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem();
            this.ruleSetFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);

            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleSetFS);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), new SolutionRuleSetsInformationProvider(this.serviceProvider));
        }

        #region Tests

        [TestMethod]
        public void ProjectBindingOperation_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ProjectBindingOperation(null, this.projectMock, this.ruleStore));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectBindingOperation(this.serviceProvider, null, this.ruleStore));
            Exceptions.Expect<ArgumentNullException>(() => new ProjectBindingOperation(this.serviceProvider, this.projectMock, null));

            ProjectBindingOperation testSubject = this.CreateTestSubject();
            testSubject.Should().NotBeNull("Suppress warning that not used");
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithDefaultValues()
        {
            // Setup
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", ProjectBindingOperation.DefaultProjectRuleSet);

            // Act
            testSubject.Initialize();

            // Verify
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath.Should().Be(ProjectBindingOperation.DefaultProjectRuleSet);
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithEmptyRuleSets()
        {
            // Setup
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", null);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", string.Empty);

            // Act
            testSubject.Initialize();

            // Verify
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                string.IsNullOrEmpty(testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath).Should().BeTrue();
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertyWithSameNonDefaultValues()
        {
            // Setup
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", "Custom1.ruleset");

            // Act
            testSubject.Initialize();

            // Verify
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.VBNET);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath.Should().Be("Custom1.ruleset");
                testSubject.PropertyInformationMap[prop].TargetRuleSetFileName.Should().Be("project");
            }
        }

        [TestMethod]
        public void ProjectBindingOperation_Initialize_ConfigurationPropertiesWithVariousValues()
        {
            // Setup
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", "NonDefualtRuleSet.ruleset");

            // Act
            testSubject.Initialize();

            // Verify
            testSubject.ProjectFullPath.Should().Be(@"c:\solution\Project\project.proj");
            testSubject.ProjectLanguage.Should().Be(Language.CSharp);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            testSubject.PropertyInformationMap[prop1].CurrentRuleSetFilePath.Should().Be(ProjectBindingOperation.DefaultProjectRuleSet);
            testSubject.PropertyInformationMap[prop1].TargetRuleSetFileName.Should().Be("project", "Default ruleset - expected project based name to be generated");
            testSubject.PropertyInformationMap[prop2].CurrentRuleSetFilePath.Should().Be("NonDefualtRuleSet.ruleset");
            testSubject.PropertyInformationMap[prop2].TargetRuleSetFileName.Should().Be("project.config2", "Non default ruleset - expected configuration based rule set name to be generated");
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_VariousRuleSetsInProjects()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(Language.VBNET, @"c:\Solution\sln.ruleset");
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock customRuleSetProperty1 = CreateProperty(this.projectMock, "config1", "Custom.ruleset");
            PropertyMock customRuleSetProperty2 = CreateProperty(this.projectMock, "config2", "Custom.ruleset");
            PropertyMock defaultRuleSetProperty1 = CreateProperty(this.projectMock, "config3", ProjectBindingOperation.DefaultProjectRuleSet);
            PropertyMock defaultRuleSetProperty2 = CreateProperty(this.projectMock, "config4", ProjectBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Verify
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
            testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected all the properties with default ruleset to have the same new ruleset");
            testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected all the properties with default ruleset to have the same new ruleset");

            string expectedRuleSetForConfig1 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config1.ruleset");
            testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetForConfig1, "Expected different rule set path for properties with custom rulesets");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetForConfig1);

            string expectedRuleSetForConfig2 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config2.ruleset");
            testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetForConfig2, "Expected different rule set path for properties with custom rulesets");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetForConfig2);

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify that written
            this.sccFileSystem.AssertFileExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
            this.sccFileSystem.AssertFileExists(expectedRuleSetForConfig1);
            this.sccFileSystem.AssertFileExists(expectedRuleSetForConfig2);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_SameNonDefaultRuleSetsInProject()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(Language.VBNET, @"c:\Solution\sln.ruleset");
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock customRuleSetProperty1 = CreateProperty(this.projectMock, "config1", "Custom.ruleset");
            PropertyMock customRuleSetProperty2 = CreateProperty(this.projectMock, "config2", "Custom.ruleset");
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Verify
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
            testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");
            testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify that written
            this.sccFileSystem.AssertFileExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_SameDefaultRuleSetsInProject()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(Language.VBNET, @"c:\Solution\sln.ruleset");
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetVBProjectKind();
            PropertyMock defaultRuleSetProperty1 = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);
            PropertyMock defaultRuleSetProperty2 = CreateProperty(this.projectMock, "config2", ProjectBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Verify
            string expectedRuleSetFileForPropertiesWithDefaultRulSets = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
            testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");
            testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath.Should().Be(expectedRuleSetFileForPropertiesWithDefaultRulSets, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify that written
            this.sccFileSystem.AssertFileExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_Cancellation()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(Language.CSharp, @"c:\Solution\sln.ruleset");
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            PropertyMock prop = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                CancellationToken token = src.Token;
                src.Cancel();

                // Act
                testSubject.Prepare(token);
            }

            // Verify
            string expectedFile = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            testSubject.PropertyInformationMap[prop].NewRuleSetFilePath.Should().BeNull("Not expecting the new rule set path to be set when canceled");
            prop.Value.ToString().Should().Be(ProjectBindingOperation.DefaultProjectRuleSet, "Should not update the property value");
            this.projectMock.Files.ContainsKey(expectedFile).Should().BeFalse("Should not be added to the project");
        }

        [TestMethod]
        public void ProjectBindingOperation_Commit()
        {
            // Setup
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            this.ruleStore.RegisterRuleSetPath(Language.CSharp, @"c:\Solution\sln.ruleset");
            PropertyMock prop = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            using (new AssertIgnoreScope()) // Ignore that the file is not on disk
            {
                testSubject.Commit();
            }

            // Verify
            string expectedFile = Path.Combine(Path.GetDirectoryName(this.projectMock.FilePath), Path.GetFileNameWithoutExtension(this.projectMock.FilePath) + ".ruleset");
            prop.Value.ToString().Should().Be(Path.GetFileName(expectedFile), "Should update the property value");
            this.projectMock.Files.ContainsKey(expectedFile).Should().BeTrue("Should be added to the project");
        }

        #endregion Tests

        #region Helpers

        private static PropertyMock CreateProperty(ProjectMock project, string configurationName, object propertyValue)
        {
            ConfigurationMock config = project.ConfigurationManager.Configurations.SingleOrDefault(c => c.ConfigurationName == configurationName);
            if (config == null)
            {
                config = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(config);
            }

            var prop = config.Properties.RegisterKnownProperty(Constants.CodeAnalysisRuleSetPropertyKey);
            prop.Value = propertyValue;
            return prop;
        }

        private ProjectBindingOperation CreateTestSubject()
        {
            return new ProjectBindingOperation(this.serviceProvider, this.projectMock, this.ruleStore);
        }

        #endregion Helpers
    }
}