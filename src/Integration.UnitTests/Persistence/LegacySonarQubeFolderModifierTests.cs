using System;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LegacySonarQubeFolderModifierTests
    {
        [TestMethod]
        public void Ctor_NullServiceProvider_ArgumentNullException()
        {
            Action act = () => new LegacySonarQubeFolderModifier(null, Mock.Of<IFileSystem>());

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new LegacySonarQubeFolderModifier(Mock.Of<IServiceProvider>(), null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Add_AddsFileToSolutionFolder()
        {
            var projectMock = Mock.Of<EnvDTE.Project>();
            var projectSystemHelperMock = new Mock<IProjectSystemHelper>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(typeof(IProjectSystemHelper)))
                .Returns(projectSystemHelperMock.Object);

            projectSystemHelperMock
                .Setup(x => x.GetSolutionFolderProject(Constants.LegacySonarQubeManagedFolderName, true))
                .Returns(projectMock);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.File.Exists("c:\\test")).Returns(true);

            var testSubject = new LegacySonarQubeFolderModifier(serviceProviderMock.Object, fileSystemMock.Object);
            testSubject.Add("c:\\test");

            projectSystemHelperMock.Verify(x=> x.AddFileToProject(projectMock, "c:\\test"), Times.Once);
        }
    }
}
