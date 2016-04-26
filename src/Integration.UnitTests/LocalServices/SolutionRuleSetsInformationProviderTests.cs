//-----------------------------------------------------------------------
// <copyright file="SolutionRuleSetsInformationProviderTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.IO;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionRuleSetsInformationProviderTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsOutputWindowPane outputPane;
        private SolutionMock solutionMock;
        private ProjectMock projectMock;
        private const string SolutionRoot = @"c:\solution";

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.projectMock = this.solutionMock.AddOrGetProject(Path.Combine(SolutionRoot, @"Project\project.proj"));

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
        }

        #region Tests
        [TestMethod]
        public void SolutionRuleSetsInformationProvider_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionRuleSetsInformationProvider(null));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ArgChecks()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.GetProjectRuleSetsDeclarations(null).ToArray());
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithDefaultValue()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", ProjectBindingOperation.DefaultProjectRuleSet);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Verify
            Assert.AreEqual(1, info.Length, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithEmptyRuleSets()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", null);
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", string.Empty);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Verify
            Assert.AreEqual(2, info.Length, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            VerifyRuleSetInformation(info[1], prop2);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationPropertyWithSameNonDefaultValues()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock prop1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            PropertyMock prop2 = CreateProperty(this.projectMock, "config2", @"x:\Folder\Custom2.ruleset");
            PropertyMock prop3 = CreateProperty(this.projectMock, "config3", @"..\Custom3.ruleset");

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Verify
            Assert.AreEqual(3, info.Length, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], prop1);
            VerifyRuleSetInformation(info[1], prop2);
            VerifyRuleSetInformation(info[2], prop3);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_ConfigurationWithNoRuleSetProperty()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            CreateProperty(this.projectMock, "config1", "Custom1.ruleset", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Verify
            Assert.AreEqual(0, info.Length, "Unexpected number of results");
            this.outputPane.AssertOutputStrings(1);
        }


        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetProjectRuleSetsDeclarations_RuleSetsWithDirectories()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            PropertyMock ruleSet1 = CreateProperty(this.projectMock, "config1", "Custom1.ruleset");
            CreateProperty(this.projectMock, "config1", @"x:\YYY\zzz", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
            PropertyMock ruleSet2 = CreateProperty(this.projectMock, "config2", "Custom1.ruleset");
            CreateProperty(this.projectMock, "config2", @"x:\YYY\zzz;q:\;", Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);

            // Act
            RuleSetDeclaration[] info = testSubject.GetProjectRuleSetsDeclarations(this.projectMock).ToArray();

            // Verify
            Assert.AreEqual(2, info.Length, "Unexpected number of results");
            VerifyRuleSetInformation(info[0], ruleSet1);
            VerifyRuleSetInformation(info[1], ruleSet2);
            this.outputPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_ArgChecks()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);

            // Act Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath(null, LanguageGroup.CSharp));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath(null, LanguageGroup.VB));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnOpenSolution()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            // Case 1: VB + invalid path characters
            // Act
            string ruleSetPath = testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey" + Path.GetInvalidPathChars().First(), LanguageGroup.VB);

            // Verify
            Assert.AreEqual(@"z:\folder\solution\SonarQube\MyKey_VB.ruleset", ruleSetPath);

            // Case 2: C# + valid path characters
            // Act
            ruleSetPath = testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey", LanguageGroup.CSharp);

            // Verify
            Assert.AreEqual(@"z:\folder\solution\SonarQube\MyKeyCSharp.ruleset", ruleSetPath);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_CalculateSolutionSonarQubeRuleSetFilePath_OnClosedSolution()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            // Act + Verify
            Exceptions.Expect<InvalidOperationException>(() => testSubject.CalculateSolutionSonarQubeRuleSetFilePath("MyKey", LanguageGroup.CSharp));
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnOpenSolution()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, @"z:\folder\solution\solutionFile.sln");

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Verify
            Assert.AreEqual(@"z:\folder\solution\SonarQube", path);
        }

        [TestMethod]
        public void SolutionRuleSetsInformationProvider_GetSolutionSonarQubeRulesFolder_OnClosedSolution()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var projectHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            projectHelper.CurrentActiveSolution = new SolutionMock(null, "" /*When the solution is closed the file is empty*/);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectHelper);

            //
            // Act
            string path = testSubject.GetSolutionSonarQubeRulesFolder();

            // Verify
            Assert.IsNull(path);
        }


        [TestMethod]
        public void SolutionRuleSetsInformationProvider_TryGetProjectRuleSetFilePath()
        {
            // Setup
            var testSubject = new SolutionRuleSetsInformationProvider(this.serviceProvider);
            var fileSystem = new ConfigurableFileSystem();
            this.serviceProvider.RegisterService(typeof(IFileSystem), fileSystem);
            ProjectMock project = new ProjectMock(@"c:\Solution\Project\Project1.myProj");
            RuleSetDeclaration declaration;
            string ruleSetPath;

            // Case 1: Declaration has an full path which exists on disk
            declaration = CreateDeclaration(project, @"c:\RuleSet.ruleset");
            fileSystem.RegisterFile(declaration.RuleSetPath);

            // Act
            Assert.IsTrue(testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath));

            // Verify
            Assert.AreEqual(@"c:\RuleSet.ruleset", ruleSetPath);

            // Case 2: Declaration is relative to project and on disk
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, @"..\RuleSet.ruleset");
            fileSystem.RegisterFile(@"c:\Solution\RuleSet.ruleset");

            // Act
            Assert.IsTrue(testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath));

            // Verify
            Assert.AreEqual(@"c:\Solution\RuleSet.ruleset", ruleSetPath);

            // Case 3: File doesn't exist
            fileSystem.ClearFiles();
            declaration = CreateDeclaration(project, "MyFile.ruleset");

            // Act
            Assert.IsFalse(testSubject.TryGetProjectRuleSetFilePath(project, declaration, out ruleSetPath));

            // Verify
            Assert.IsNull(ruleSetPath);
        }
        #endregion

        #region Helpers
        private static RuleSetDeclaration CreateDeclaration(ProjectMock project, string ruleSetValue)
        {
            return new RuleSetDeclaration(project, new PropertyMock("never mind", null), ruleSetValue, "Configuration");
        }

        private static PropertyMock CreateProperty(ProjectMock project, string configurationName, object propertyValue, string propertyName = Constants.CodeAnalysisRuleSetPropertyKey)
        {
            ConfigurationMock config = GetOrCreateConfiguration(project, configurationName);

            var prop = config.Properties.RegisterKnownProperty(propertyName);
            prop.Value = propertyValue;
            return prop;
        }

        private static ConfigurationMock GetOrCreateConfiguration(ProjectMock project, string configurationName)
        {
            ConfigurationMock config = project.ConfigurationManager.Configurations.SingleOrDefault(c => c.ConfigurationName == configurationName);
            if (config == null)
            {
                config = new ConfigurationMock(configurationName);
                project.ConfigurationManager.Configurations.Add(config);
            }

            return config;
        }

        private void VerifyRuleSetInformation(RuleSetDeclaration info, PropertyMock property)
        {
            Assert.AreSame(property, info.DeclaringProperty);
            Assert.AreEqual(property.Value, info.RuleSetPath);

            Configuration configuration = (Configuration)property.Collection.Parent;
            if (configuration == null)
            {
                Assert.Inconclusive("Test setup error, expected to have configuration as parent");
            }

            Assert.AreEqual(configuration.ConfigurationName, info.ConfigurationContext);

            Property ruleSetDirectory = configuration.Properties.OfType<Property>().SingleOrDefault(p => p.Name == Constants.CodeAnalysisRuleSetDirectoriesPropertyKey);
            string ruleSetDirectoryValue = ruleSetDirectory?.Value as string;
            if (string.IsNullOrWhiteSpace(ruleSetDirectoryValue))
            {
                Assert.AreEqual(0, info.RuleSetDirectories.Count());
            }
            else
            {
                string[] expected = ruleSetDirectoryValue.Split(new[] { SolutionRuleSetsInformationProvider.RuleSetDirectoriesValueSpliter }, StringSplitOptions.RemoveEmptyEntries);
                CollectionAssert.AreEquivalent(expected, info.RuleSetDirectories.ToArray(), "Actual: {0}", string.Join(", ", info.RuleSetDirectories));
            }
        }
        #endregion
    }
}
