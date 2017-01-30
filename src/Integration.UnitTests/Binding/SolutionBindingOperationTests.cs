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

using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{

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

        public SolutionBindingOperationTests()
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
            this.sccFileSystem = new ConfigurableSourceControlledFileSystem();
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
        [Fact]
        public void Ctor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var connectionInformation = new ConnectionInformation(new Uri("http://valid"));

            // Act
            Action act = () => new SolutionBindingOperation(null, connectionInformation, "key");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [Fact]
        public void Ctor_WithNullConnection_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new SolutionBindingOperation(this.serviceProvider, null, "key");

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("connection");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Ctor_WithNullorEmptyOrWhiteSpaceSonarQubeProjectKey_ThrowsArgumentNullException(string value)
        {
            // Arrange
            var connectionInformation = new ConnectionInformation(new Uri("http://valid"));

            // Act
            Action act = () => new SolutionBindingOperation(this.serviceProvider, connectionInformation, value);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("sonarQubeProjectKey");
        }

        [Fact]
        public void RegisterKnownRuleSets_WithNullRuleSets_ThrowsArgumentNullException()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act
            Action act = () => testSubject.RegisterKnownRuleSets(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("ruleSets");
        }

        [Fact]
        public void SolutionBindingOperation_RegisterKnownRuleSets()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            ruleSetMap[Language.VBNET] = new RuleSet("vb");

            // Sanity
            testSubject.RuleSetsInformationMap.Should().BeEmpty("Not expecting any registered rulesets");

            // Act
            testSubject.RegisterKnownRuleSets(ruleSetMap);

            // Assert
            testSubject.RuleSetsInformationMap.Keys
                .Should().Equal(ruleSetMap.Keys);
            testSubject.RuleSetsInformationMap[Language.CSharp].RuleSet
                .Should().BeSameAs(ruleSetMap[Language.CSharp]);
            testSubject.RuleSetsInformationMap[Language.VBNET].RuleSet
                .Should().BeSameAs(ruleSetMap[Language.VBNET]);
        }

        [Fact]
        public void SolutionBindingOperation_GetRuleSetInformation()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Test case 1: unknown ruleset map
            // Act + Assert
            using (new AssertIgnoreScope())
            {
                testSubject.GetRuleSetInformation(Language.CSharp).Should().BeNull();
            }

            // Test case 2: known ruleset map
            // Arrange
            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            ruleSetMap[Language.VBNET] = new RuleSet("vb");

            testSubject.RegisterKnownRuleSets(ruleSetMap);
            testSubject.Initialize(new ProjectMock[0], GetQualityProfiles());
            testSubject.Prepare(CancellationToken.None);

            // Act
            string filePath = testSubject.GetRuleSetInformation(Language.CSharp).NewRuleSetFilePath;

            // Assert
            filePath.Should().NotBeNullOrWhiteSpace();
            filePath.Should().Be(testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath, "NewRuleSetFilePath is expected to be updated during Prepare and returned now");
        }

        [Fact]
        public void Initialize_WithNullProjects_ThrowsArgumentNullException()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act
            Action act = () => testSubject.Initialize(null, GetQualityProfiles());

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("projects");
        }

        [Fact]
        public void Initialize_WithNullProfilesMap_ThrowsArgumentNullException()
        {
            // Arrange
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            // Act
            Action act = () => testSubject.Initialize(new Project[0], null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("profilesMap");
        }

        [Fact]
        public void SolutionBindingOperation_Initialization()
        {
            // Arrange
            var cs1Project = this.solutionMock.AddOrGetProject("CS1.csproj");
            cs1Project.SetCSProjectKind();
            var cs2Project = this.solutionMock.AddOrGetProject("CS2.csproj");
            cs2Project.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var projects = new[] { cs1Project, vbProject, cs2Project };

            // Sanity
            testSubject.Binders.Should().BeEmpty("Not expecting any project binders");

            // Act
            testSubject.Initialize(projects, GetQualityProfiles());

            // Assert
            testSubject.SolutionFullPath.Should().Be(@"c:\solution\xxx.sln");
            projects.Should().HaveSameCount(testSubject.Binders, "Should be one per managed project");
        }

        [Fact]
        public void SolutionBindingOperation_Prepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            ruleSetMap[Language.VBNET] = new RuleSet("vb");

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
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath
                .Should().Be(@"c:\solution\SonarQube\keyCSharp.ruleset");
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath
                .Should().Be(@"c:\solution\SonarQube\keyVB.ruleset");

            // Act
            testSubject.Prepare(CancellationToken.None);

            // Assert
            this.sccFileSystem.AssertDirectoryNotExists(sonarQubeRulesDirectory);
            prepareCalledForBinder.Should().BeTrue("Expected to propagate the prepare call to binders");
            this.sccFileSystem.AssertFileNotExists(@"c:\solution\SonarQube\keyCSharp.ruleset");
            this.sccFileSystem.AssertFileNotExists(@"c:\solution\SonarQube\keyVB.ruleset");

            // Act (write pending)
            this.sccFileSystem.WritePendingNoErrorsExpected();

            // Assert
            this.sccFileSystem.AssertFileExists(@"c:\solution\SonarQube\keyCSharp.ruleset");
            this.sccFileSystem.AssertFileExists(@"c:\solution\SonarQube\keyVB.ruleset");
            this.sccFileSystem.AssertDirectoryExists(sonarQubeRulesDirectory);
        }

        [Fact]
        public void SolutionBindingOperation_Prepare_Cancellation_DuringBindersPrepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");
            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            ruleSetMap[Language.VBNET] = new RuleSet("vb");

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

            // Assert
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath
                .Should().Be(@"c:\solution\SonarQube\keyCSharp.ruleset");
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath
                .Should().Be(@"c:\solution\SonarQube\keyVB.ruleset");
            prepareCalledForBinder
                .Should().BeFalse("Expected to be canceled as soon as possible i.e. after the first binder");
        }

        [Fact]
        public void SolutionBindingOperation_Prepare_Cancellation_BeforeBindersPrepare()
        {
            // Arrange
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var vbProject = this.solutionMock.AddOrGetProject("VB.vbproj");
            vbProject.SetVBProjectKind();
            var projects = new[] { csProject, vbProject };

            SolutionBindingOperation testSubject = this.CreateTestSubject("key");

            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            ruleSetMap[Language.VBNET] = new RuleSet("vb");

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

            // Assert
            testSubject.RuleSetsInformationMap[Language.CSharp].NewRuleSetFilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            testSubject.RuleSetsInformationMap[Language.VBNET].NewRuleSetFilePath.Should().NotBeNull("Expected to be set before Prepare is called");
            prepareCalledForBinder.Should().BeFalse("Expected to be canceled as soon as possible i.e. before the first binder");
        }

        [Fact]
        public void SolutionBindingOperation_CommitSolutionBinding()
        {
            // Arrange
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.solutionBinding);
            var csProject = this.solutionMock.AddOrGetProject("CS.csproj");
            csProject.SetCSProjectKind();
            var projects = new[] { csProject };

            var connectionInformation = new ConnectionInformation(new Uri("http://xyz"));
            SolutionBindingOperation testSubject = this.CreateTestSubject("key", connectionInformation);

            var ruleSetMap = new Dictionary<Language, RuleSet>();
            ruleSetMap[Language.CSharp] = new RuleSet("cs");
            testSubject.RegisterKnownRuleSets(ruleSetMap);
            var profiles = GetQualityProfiles();
            profiles[Language.CSharp] = new QualityProfile { Key = "C# Profile", QualityProfileTimestamp = DateTime.Now };
            testSubject.Initialize(projects, profiles);
            testSubject.Binders.Clear(); // Ignore the real binders, not part of this test scope
            bool commitCalledForBinder = false;
            testSubject.Binders.Add(new ConfigurableBindingOperation { CommitAction = () => commitCalledForBinder = true });
            testSubject.Prepare(CancellationToken.None);
            this.solutionBinding.WriteSolutionBindingAction = bindingInfo =>
            {
                connectionInformation.ServerUri.Should().Be(bindingInfo.ServerUri);
                bindingInfo.Profiles.Should().HaveCount(1);

                QualityProfile csProfile = profiles[Language.CSharp];
                csProfile.Key.Should().Be(bindingInfo.Profiles[Language.CSharp].ProfileKey);
                csProfile.QualityProfileTimestamp.Should().Be(bindingInfo.Profiles[Language.CSharp].ProfileTimestamp);

                return "Doesn't matter";
            };

            // Sanity
            this.solutionBinding.AssertWrittenFiles(0);

            // Act
            var commitResult = testSubject.CommitSolutionBinding();

            // Assert
            commitResult.Should().BeTrue();
            commitCalledForBinder.Should().BeTrue();
            this.solutionItemsProject.Files.ContainsKey(@"c:\solution\SonarQube\keyCSharp.ruleset")
                .Should().BeTrue("Ruleset was expected to be added to solution items");
            this.solutionBinding.AssertWrittenFiles(1);
        }

        [Fact]
        public void Ctor_WithNullRuleSet_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new RuleSetInformation(Language.CSharp, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("ruleSet");
        }

        #endregion

        #region Helpers
        private SolutionBindingOperation CreateTestSubject(string projectKey, ConnectionInformation connection = null)
        {
            return new SolutionBindingOperation(this.serviceProvider,
                connection ?? new ConnectionInformation(new Uri("http://host")),
                projectKey);
        }

        private static Dictionary<Language, QualityProfile> GetQualityProfiles()
        {
            return new Dictionary<Language, QualityProfile>();
        }
        #endregion
    }
}
