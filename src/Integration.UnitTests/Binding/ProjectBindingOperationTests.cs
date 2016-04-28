//-----------------------------------------------------------------------
// <copyright file="ProjectBindingOperationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.IO;
using System.Linq;
using System.Threading;

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
            Assert.IsNotNull(testSubject, "Suppress warning that not used");
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
            Assert.AreEqual(@"c:\solution\Project\project.proj", testSubject.ProjectFullPath);
            Assert.AreEqual(RuleSetGroup.VB, testSubject.ProjectGroup);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                Assert.AreEqual(ProjectBindingOperation.DefaultProjectRuleSet, testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath);
                Assert.AreEqual("project", testSubject.PropertyInformationMap[prop].TargetRuleSetFileName);
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
            Assert.AreEqual(@"c:\solution\Project\project.proj", testSubject.ProjectFullPath);
            Assert.AreEqual(RuleSetGroup.VB, testSubject.ProjectGroup);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                Assert.IsTrue(string.IsNullOrEmpty(testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath));
                Assert.AreEqual("project", testSubject.PropertyInformationMap[prop].TargetRuleSetFileName);
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
            Assert.AreEqual(@"c:\solution\Project\project.proj", testSubject.ProjectFullPath);
            Assert.AreEqual(RuleSetGroup.VB, testSubject.ProjectGroup);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            foreach (var prop in new[] { prop1, prop2 })
            {
                Assert.AreEqual("Custom1.ruleset", testSubject.PropertyInformationMap[prop].CurrentRuleSetFilePath);
                Assert.AreEqual("project", testSubject.PropertyInformationMap[prop].TargetRuleSetFileName);
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
            Assert.AreEqual(@"c:\solution\Project\project.proj", testSubject.ProjectFullPath);
            Assert.AreEqual(RuleSetGroup.CSharp, testSubject.ProjectGroup);
            CollectionAssert.AreEquivalent(new[] { prop1, prop2 }, testSubject.PropertyInformationMap.Keys.ToArray(), "Unexpected properties");

            Assert.AreEqual(ProjectBindingOperation.DefaultProjectRuleSet, testSubject.PropertyInformationMap[prop1].CurrentRuleSetFilePath);
            Assert.AreEqual("project", testSubject.PropertyInformationMap[prop1].TargetRuleSetFileName, "Default ruleset - expected project based name to be generated");
            Assert.AreEqual("NonDefualtRuleSet.ruleset", testSubject.PropertyInformationMap[prop2].CurrentRuleSetFilePath);
            Assert.AreEqual("project.config2", testSubject.PropertyInformationMap[prop2].TargetRuleSetFileName, "Non default ruleset - expected configuration based rule set name to be generated");
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_VariousRuleSetsInProjects()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(RuleSetGroup.VB, @"c:\Solution\sln.ruleset");
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
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath, "Expected all the properties with default ruleset to have the same new ruleset");
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath, "Expected all the properties with default ruleset to have the same new ruleset");

            string expectedRuleSetForConfig1 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config1.ruleset");
            Assert.AreEqual(expectedRuleSetForConfig1, testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");
            this.sccFileSystem.AssertFileNotExists(expectedRuleSetForConfig1);

            string expectedRuleSetForConfig2 = Path.ChangeExtension(expectedRuleSetFileForPropertiesWithDefaultRulSets, "config2.ruleset");
            Assert.AreEqual(expectedRuleSetForConfig2, testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");
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
            this.ruleStore.RegisterRuleSetPath(RuleSetGroup.VB, @"c:\Solution\sln.ruleset");
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
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[customRuleSetProperty1].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[customRuleSetProperty2].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify that written
            this.sccFileSystem.AssertFileExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_SameDefaultRuleSetsInProject()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(RuleSetGroup.VB, @"c:\Solution\sln.ruleset");
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
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[defaultRuleSetProperty1].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");
            Assert.AreEqual(expectedRuleSetFileForPropertiesWithDefaultRulSets, testSubject.PropertyInformationMap[defaultRuleSetProperty2].NewRuleSetFilePath, "Expected different rule set path for properties with custom rulesets");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify that written
            this.sccFileSystem.AssertFileExists(expectedRuleSetFileForPropertiesWithDefaultRulSets);
        }

        [TestMethod]
        public void ProjectBindingOperation_Prepare_Cancellation()
        {
            // Setup
            this.ruleStore.RegisterRuleSetPath(RuleSetGroup.CSharp, @"c:\Solution\sln.ruleset");
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
            Assert.IsNull(testSubject.PropertyInformationMap[prop].NewRuleSetFilePath, "Not expecting the new rule set path to be set when cancelled");
            Assert.AreEqual(ProjectBindingOperation.DefaultProjectRuleSet, prop.Value.ToString(), "Should not update the property value");
            Assert.IsFalse(this.projectMock.Files.ContainsKey(expectedFile), "Should not be added to the project");
        }

        [TestMethod]
        public void ProjectBindingOperation_Commit()
        {
            // Setup
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            ProjectBindingOperation testSubject = this.CreateTestSubject();
            this.projectMock.SetCSProjectKind();
            this.ruleStore.RegisterRuleSetPath(RuleSetGroup.CSharp, @"c:\Solution\sln.ruleset");
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
            Assert.AreEqual(Path.GetFileName(expectedFile), prop.Value.ToString(), "Should update the property value");
            Assert.IsTrue(this.projectMock.Files.ContainsKey(expectedFile), "Should be added to the project");
        }
        #endregion

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
        #endregion
    }
}
