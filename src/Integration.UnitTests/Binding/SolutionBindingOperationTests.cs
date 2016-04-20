//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOperationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class SolutionBindingOperationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableProjectSystemFilter projectFilter;
        private ConfigurableVsOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private ConfigurableRuleSetSerializer ruleFS;
        private ConfigurableSolutionBindingSerializer solutionBinding;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfo;

        private const string SolutionRoot = @"c:\solution";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.outputPane = new ConfigurableVsOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane);
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.projectFilter = new ConfigurableProjectSystemFilter();
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
            this.projectSystemHelper.CurrentActiveSolution = this.solutionMock;
            this.sccFileSystem  = new ConfigurableSourceControlledFileSystem();
            this.ruleFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            this.solutionBinding = new ConfigurableSolutionBindingSerializer();
            this.ruleSetInfo = new ConfigurableSolutionRuleSetsInformationProvider();
            this.ruleSetInfo.SolutionRootFolder = SolutionRoot;

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleFS);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfo);
        }

        #region Tests
        [TestMethod]
        public void SolutionBindingOperation_ArgChecks()
        {
            var connectionInformation = new ConnectionInformation(new Uri("http://valid"));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(null, connectionInformation, "key"));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, null, "key"));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, null));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, connectionInformation, string.Empty));

            var testSubject = new SolutionBindingOperation(this.serviceProvider, connectionInformation, "key");
            Assert.IsNotNull(testSubject, "Avoid 'testSubject' not used analysis warning");
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets_ArgChecks()
        {
            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.RegisterKnownRuleSets(null));
        }

        [TestMethod]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key"); 
            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            // Sanity
            Assert.AreEqual(0, testSubject.RuleSetsInformationMap.Count, "Not expecting any registered rulesets");

            // Act
            testSubject.RegisterKnownRuleSets(ruleSetMap);

            // Verify
            CollectionAssert.AreEquivalent(ruleSetMap.Keys.ToArray(), testSubject.RuleSetsInformationMap.Keys.ToArray());
            Assert.AreSame(ruleSetMap[RuleSetGroup.CSharp], testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].RuleSet);
            Assert.AreSame(ruleSetMap[RuleSetGroup.VB], testSubject.RuleSetsInformationMap[RuleSetGroup.VB].RuleSet);
        }

        [TestMethod]
        public void SolutionBindingOperation_GetRuleSetFilePath()
        {
            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Test case 1: unknown ruleset map
            // Act + Verify
            using (new AssertIgnoreScope())
            {
                Assert.IsNull(testSubject.GetRuleSetFilePath(RuleSetGroup.CSharp));
            }

            // Test case 2: known ruleset map
            // Setup
            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(new ProjectMock[0], GetQualityProfiles());
            testSubject.Prepare(CancellationToken.None);

            // Act
            string filePath = testSubject.GetRuleSetFilePath(RuleSetGroup.CSharp);

            // Verify
            Assert.IsFalse(string.IsNullOrWhiteSpace(filePath));
            Assert.AreEqual(testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath, filePath, "NewRuleSetFilePath is expected to be updated during Prepare and returned now");
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization_ArgChecks()
        {
            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act + Verify
            Exceptions.Expect<ArgumentNullException>(() => testSubject.Initialize(null, GetQualityProfiles()));
            Exceptions.Expect<ArgumentNullException>(() => testSubject.Initialize(new Project[0], null));
        }

        [TestMethod]
        public void SolutionBindingOperation_Initialization()
        {
            // Setup
            var cs1Project = this.solutionMock.AddOrGetProject("CS1.csproj");
            cs1Project.SetCSProjectKind();
            var cs2Project = this.solutionMock.AddOrGetProject("CS2.csproj");
            cs2Project.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var projects = new[] { cs1Project, vbProject, cs2Project };

            // Sanity
            Assert.AreEqual(0, testSubject.Binders.Count, "Not expecting any project binders");

            // Act
            testSubject.Initialize(projects, GetQualityProfiles());

            // Verify
            Assert.AreEqual(@"c:\solution\xxx.sln", testSubject.SolutionFullPath);
            Assert.AreEqual(projects.Length, testSubject.Binders.Count, "Should be one per managed project");
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            var binder = new ConfigurableBindingOperation();
            testSubject.Binders.Add(binder);
            bool prepareCalledForBinder = false;
            binder.PrepareAction = (ct) => prepareCalledForBinder = true;
            string sonarQubeRulesDirectory = Path.Combine(SolutionRoot, Constants.SonarQubeManagedFolderName);

            // Sanity
            this.sccFileSystem.AssertDirectoryNotExists(sonarQubeRulesDirectory);
            Assert.AreEqual(@"c:\solution\SonarQube\keyCSharp.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath);
            Assert.AreEqual(@"c:\solution\SonarQube\keyVB.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath);
            
            // Act
            testSubject.Prepare(CancellationToken.None);

            // Verify
            this.sccFileSystem.AssertDirectoryNotExists(sonarQubeRulesDirectory);
            Assert.IsTrue(prepareCalledForBinder, "Expected to propagate the prepare call to binders");
            this.sccFileSystem.AssertFileNotExists(@"c:\solution\SonarQube\keyCSharp.ruleset");
            this.sccFileSystem.AssertFileNotExists(@"c:\solution\SonarQube\keyVB.ruleset");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Verify
            this.sccFileSystem.AssertFileExists(@"c:\solution\SonarQube\keyCSharp.ruleset");
            this.sccFileSystem.AssertFileExists(@"c:\solution\SonarQube\keyVB.ruleset");
            this.sccFileSystem.AssertDirectoryExists(sonarQubeRulesDirectory);
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_Cancellation_DuringBindersPrepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool prepareCalledForBinder = false;
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => src.Cancel() });
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => prepareCalledForBinder = true });

                // Act
                testSubject.Prepare(src.Token);
            }

            // Verify
            Assert.AreEqual(@"c:\solution\SonarQube\keyCSharp.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath);
            Assert.AreEqual(@"c:\solution\SonarQube\keyVB.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath);
            Assert.IsFalse(prepareCalledForBinder, "Expected to be cancelled as soon as possible i.e. after the first binder");
        }

        [TestMethod]
        public void SolutionBindingOperation_Prepare_Cancellation_BeforeBindersPrepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(projects, GetQualityProfiles());
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool prepareCalledForBinder = false;
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                testSubject.Binders.Add(new ConfigurableBindingOperation { PrepareAction = (t) => prepareCalledForBinder = true });
                src.Cancel();

                // Act
                testSubject.Prepare(src.Token);
            }

            // Verify
            Assert.IsNotNull(testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath, "Expected to be set before Prepare is called");
            Assert.IsNotNull(testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath, "Expected to be set before Prepare is called");
            Assert.IsFalse(prepareCalledForBinder, "Expected to be cancelled as soon as possible i.e. before the first binder");
        }

        [TestMethod]
        public void SolutionBindingOperation_CommitSolutionBinding()
        {
            // Setup
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.solutionBinding);
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var projects = new[] { csProject };

            var connectionInformation = new ConnectionInformation(new Uri("http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject("key", connectionInformation);

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            testSubject.RegisterKnownRuleSets(ruleSetMap);
            var profiles = GetQualityProfiles();
            profiles[RuleSetGroup.CSharp] = new QualityProfile { Key = "C# Profile", QualityProfileTimestamp = DateTime.Now };
            testSubject.Initialize(projects, profiles);
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);
            this.solutionBinding.WriteSolutionBindingAction = bindingInfo =>
            {
                Assert.AreEqual(connectionInformation.ServerUri, bindingInfo.ServerUri);
                Assert.AreEqual(1, bindingInfo.Profiles.Count);

                QualityProfile csProfile = profiles[RuleSetGroup.CSharp];
                Assert.AreEqual(csProfile.Key, bindingInfo.Profiles[RuleSetGroup.CSharp].ProfileKey);
                Assert.AreEqual(csProfile.QualityProfileTimestamp, bindingInfo.Profiles[RuleSetGroup.CSharp].ProfileTimestamp);

                return "Doesn't matter";
            };

            // Sanity
            this.solutionBinding.AssertWrittenFiles(0);

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Verify
            Assert.IsTrue(commitResult);
            Assert.IsTrue(commitCalledForBinder);
            Assert.IsTrue(this.solutionItemsProject.Files.ContainsKey(@"c:\solution\SonarQube\keyCSharp.ruleset"), "Ruleset was expected to be added to solution items");
            this.solutionBinding.AssertWrittenFiles(1);
        }

        [TestMethod]
        public void SolutionBindingOperation_RuleSetInformation_Ctor_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation.RuleSetInformation(RuleSetGroup.CSharp, null));
        }

        #endregion

        #region Helpers
        private SolutionBindingOperation CreateTestSubject(string projectKey, ConnectionInformation connection = null)
        {
            return new SolutionBindingOperation(this.serviceProvider,
                connection ?? new ConnectionInformation(new Uri("http://host")),
                projectKey);
        }

        private static Dictionary<RuleSetGroup, QualityProfile> GetQualityProfiles()
        {
            return new Dictionary<RuleSetGroup, QualityProfile>();
        }
        #endregion
    }
}
