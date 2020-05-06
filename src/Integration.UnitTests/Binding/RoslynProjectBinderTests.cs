using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class RoslynProjectBinderTests
    {
        private Mock<IServiceProvider> serviceProviderMock;
        private Mock<ISolutionRuleSetsInformationProvider> solutionRuleSetsInformationProviderMock;
        private Mock<IFileSystem> fileSystemMock;
        private Mock<IProjectSystemHelper> projectSystemMock;

        private RoslynProjectBinder testSubject;
        private ConfigurableSourceControlledFileSystem configurableSourceControlledFileSystem;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();
            projectSystemMock = new Mock<IProjectSystemHelper>();
            solutionRuleSetsInformationProviderMock = new Mock<ISolutionRuleSetsInformationProvider>();

            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISolutionRuleSetsInformationProvider)))
                .Returns(solutionRuleSetsInformationProviderMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IRuleSetSerializer)))
                .Returns(Mock.Of<IRuleSetSerializer>());

            configurableSourceControlledFileSystem = new ConfigurableSourceControlledFileSystem(fileSystemMock.Object);

            serviceProviderMock
                .Setup(x => x.GetService(typeof(ISourceControlledFileSystem)))
                .Returns(configurableSourceControlledFileSystem);

            testSubject = new RoslynProjectBinder(serviceProviderMock.Object, fileSystemMock.Object);
        }

        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new RoslynProjectBinder(null, Mock.Of<IFileSystem>());

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }


        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new RoslynProjectBinder(serviceProviderMock.Object, null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void GetBindAction_QueuesRulesetFileToBeWritten()
        {
            var bindingConfig = new DotNetBindingConfigFile(new RuleSet("test"), "c:\\test.ruleset");
            var projectMock = new ProjectMock("c:\\test.csproj");
            var ruleSetValue = "test";

            fileSystemMock.Setup(x => x.File.Exists("c:\\test.ruleset")).Returns(false);

            solutionRuleSetsInformationProviderMock
                .Setup(x => x.GetProjectRuleSetsDeclarations(projectMock))
                .Returns(new List<RuleSetDeclaration>
                {
                    new RuleSetDeclaration(projectMock, new PropertyMock("never mind", null), ruleSetValue, "Configuration")
                });

            testSubject.GetBindAction(bindingConfig, projectMock, CancellationToken.None);

            configurableSourceControlledFileSystem.AssertQueuedOperationCount(1);
        }
    }
}
