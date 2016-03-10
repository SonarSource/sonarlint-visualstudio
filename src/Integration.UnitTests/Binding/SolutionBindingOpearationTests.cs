//-----------------------------------------------------------------------
// <copyright file="SolutionBindingOpearationTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class SolutionBindingOpearationTests
    {
        private DTEMock dte;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private ConfigurableVsGeneralOutputWindowPane outputPane;
        private ProjectMock solutionItemsProject;
        private SolutionMock solutionMock;
        private const string SolutionRoot = @"c:\solution";

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
        }

        [TestMethod]
        public void SolutionBindingOpearation_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(null, this.projectSystemHelper, "key"));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, null, "key"));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, null));
            Exceptions.Expect<ArgumentNullException>(() => new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, string.Empty));

            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, "key");
            Assert.IsNotNull(testSubject, "Avoid 'testSubject' not used analysis warning");
        }

        [TestMethod]
        public void SolutionBindingOpearation_RegisterKnownRuleSets()
        {
            // Setup
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, "key");
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
            const string ProjectKey = "key";
            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(ProjectKey, fs);
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, "key", writer);

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
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, "key");
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
            const string ProjectKey = "key";
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(ProjectKey, fs);
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, ProjectKey, writer);

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

            // Sanity
            Assert.IsNull(testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath);
            Assert.IsNull(testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath);

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Verify
            Assert.AreEqual(@"c:\solution\SonarQube\keyCSharp.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath);
            Assert.AreEqual(@"c:\solution\SonarQube\keyVB.ruleset", testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath);
            fs.AssertFileExists(@"c:\solution\SonarQube\keyCSharp.ruleset");
            fs.AssertFileExists(@"c:\solution\SonarQube\keyVB.ruleset");
            Assert.IsTrue(prepareCalledForBinder, "Expected to propagate the prepare call to binders");
        }

        [TestMethod]
        public void SolutionBindingOpearation_Prepare_Cancellation_DuringBindersPrepare()
        {
            // Setup
            const string ProjectKey = "key";
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(ProjectKey, fs);
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, ProjectKey, writer);

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
        public void SSolutionBindingOpearation_Prepare_Cancellation_BeforeBindersPrepare()
        {
            // Setup
            const string ProjectKey = "key";
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject, vbProject };

            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(ProjectKey, fs);
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, ProjectKey, writer);

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
            Assert.IsNull(testSubject.RuleSetsInformationMap[RuleSetGroup.CSharp].NewRuleSetFilePath);
            Assert.IsNull(testSubject.RuleSetsInformationMap[RuleSetGroup.VB].NewRuleSetFilePath);
            Assert.IsFalse(prepareCalledForBinder, "Expected to be cancelled as soon as possible i.e. before the first binder");
        }

        [TestMethod]
        public void SolutionBindingOpearation_Commit()
        {
            // Setup
            const string ProjectKey = "key";
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            this.projectSystemHelper.ManagedProjects = new[] { csProject };

            var fs = new ConfigurableRuleSetGenerationFileSystem();
            var writer = new SolutionRuleSetWriter(ProjectKey, fs);
            var testSubject = new SolutionBindingOperation(this.serviceProvider, this.projectSystemHelper, ProjectKey, writer);

            var ruleSetMap = new Dictionary<RuleSetGroup, RuleSet>();
            ruleSetMap[RuleSetGroup.CSharp] = new RuleSet("cs");
            ruleSetMap[RuleSetGroup.VB] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize();
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);

            // Act
            using (new AssertIgnoreScope()) // Ignore asserts that the file is not on disk
            {
                testSubject.Commit();
            }

            // Verify
            Assert.IsTrue(commitCalledForBinder);
            Assert.IsTrue(this.solutionItemsProject.Files.ContainsKey(@"c:\solution\SonarQube\keyCSharp.ruleset"), "Ruleset was expected to be added to solution items");
        }
    }
}
