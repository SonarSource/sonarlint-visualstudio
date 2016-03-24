//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOperationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
        private ConfigurableVsGeneralOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private ConfigurableSourceControlledFileSystem sccFileSystem;
        private ConfigurableRuleSetSerializer ruleFS;
        private ConfigurableSolutionBinding solutionBinding;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetInfo;

        private const string SolutionRoot = @"c:\solution";

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            this.dte = new DTEMock();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.solutionMock = new SolutionMock(dte, Path.Combine(SolutionRoot, "xxx.sln"));
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputPane = new ConfigurableVsGeneralOutputWindowPane());
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.solutionItemsProject = this.solutionMock.AddOrGetProject("Solution items");
            this.projectSystemHelper.SolutionItemsProject = this.solutionItemsProject;
            this.projectSystemHelper.CurrentActiveSolution = this.solutionMock;
            this.sccFileSystem  = new ConfigurableSourceControlledFileSystem();
            this.ruleFS = new ConfigurableRuleSetSerializer(this.sccFileSystem);
            this.solutionBinding = new ConfigurableSolutionBinding();
            this.ruleSetInfo = new ConfigurableSolutionRuleSetsInformationProvider();
            this.ruleSetInfo.SolutionRootFolder = SolutionRoot;

            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), this.sccFileSystem);
            this.serviceProvider.RegisterService(typeof(IRuleSetSerializer), this.ruleFS);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetInfo);
        }

        #region Tests
        [TestMethod]
        public void SolutionBindingOpearation_ArgChecks()
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
        public void SolutionBindingOpearation_RegisterKnownRuleSets()
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
        public void SolutionBindingOpearation_GetRuleSetFilePath()
        {
            // Setup
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
            testSubject.Prepare(CancellationToken.None);

            // Act
            string filePath = testSubject.GetRuleSetFilePath(RuleSetGroup.CSharp);

            // Verify
            Assert.IsFalse(string.IsNullOrWhiteSpace(filePath));
            Assert.AreEqual(testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath, filePath, "NewRuleSetFilePath is expected to be updated during Prepare and returned now");
        }

        [TestMethod]
        public void SolutionBindingOpearation_Initialization()
        {
            // Setup
            var cs1Project = this.solutionMock.AddOrGetProject("CS1.csproj");
            cs1Project.SetCSProjectKind();
            var cs2Project = this.solutionMock.AddOrGetProject("CS2.csproj");
            cs2Project.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            this.projectSystemHelper.ManagedProjects = new[] { cs1Project, vbProject, cs2Project };

            // Sanity
            Assert.AreEqual(0, testSubject.Binders.Count, "Not expecting any project binders");

            // Act
            testSubject.Initialize();

            // Verify
            Assert.AreEqual(@"c:\solution\xxx.sln", testSubject.SolutionFullPath);
            Assert.AreEqual(this.projectSystemHelper.ManagedProjects.Count(), testSubject.Binders.Count, "Should be one per managed project");
        }

        [TestMethod]
        public void SolutionBindingOpearation_Prepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
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
        public void SolutionBindingOpearation_Prepare_Cancellation_DuringBindersPrepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
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
        public void SolutionBindingOpearation_Prepare_Cancellation_BeforeBindersPrepare()
        {
            // Setup
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
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
        public void SolutionBindingOpearation_CommitSolutionBinding()
        {
            // Setup
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBinding), this.solutionBinding);
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject };

            var connectionInformation = new Integration.Service.ConnectionInformation(new Uri("Http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject("key", connectionInformation);

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);
            this.solutionBinding.WriteSolutionBindingAction = b =>
            {
                Assert.AreEqual(connectionInformation.ServerUri, b.ServerUri);
                Assert.AreEqual(connectionInformation.ServerUri, b.ServerUri);

                return "Doesn't matter";
            };

            // Act

            this.solutionBinding.AssertWriteSolutionBindingRequests(0);
            Assert.IsTrue(testSubject.CommitSolutionBinding());

            // Verify
            Assert.IsTrue(commitCalledForBinder);
            Assert.IsTrue(this.solutionItemsProject.Files.ContainsKey(@"c:\solution\SonarQube\keyCSharp.ruleset"), "Ruleset was expected to be added to solution items");
            this.solutionBinding.AssertWriteSolutionBindingRequests(1);
            this.solutionBinding.AssertAllPendingWritten();
        }
        #endregion

        #region Helpers
        private SolutionBindingOperation CreateTestSubject(string projectKey, ConnectionInformation connection = null)
        {
            return new SolutionBindingOperation(this.serviceProvider,
                connection ?? new ConnectionInformation(new Uri("http://host")),
                projectKey);
        }
        #endregion
    }
}
