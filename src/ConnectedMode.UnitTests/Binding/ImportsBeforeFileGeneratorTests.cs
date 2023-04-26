/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using System.IO.Abstractions;
using System.Xml;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class ImportsBeforeFileGeneratorTests
    {
        [TestMethod]
        public void FileDoesNotExist_CreatesFileWithWritesCorrectContent()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");
            string fileContent = GetTargetFileContent();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(false);

            CreateTestSubject(fileSystem: fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, fileContent), Times.Once);
        }

        [TestMethod]
        public void FileExists_DifferentText_CreatesFile()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(true);
            fileSystem.Setup(x => x.File.ReadAllText(pathToFile)).Returns("wrong text");

            CreateTestSubject(fileSystem: fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, It.IsAny<String>()), Times.Once);
        }

        [TestMethod]
        public void FileExists_SameText_DoesNotCreateFile()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(true);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(true);

            string fileContent = GetTargetFileContent();
            fileSystem.Setup(x => x.File.ReadAllText(pathToFile)).Returns(fileContent);

            CreateTestSubject(fileSystem: fileSystem.Object);

            fileSystem.Verify(x => x.File.WriteAllText(pathToFile, It.IsAny<String>()), Times.Never);
        }

        [TestMethod]
        public void DirectoryDoesNotExist_CreatesDirectory()
        {
            string pathToDirectory = GetPathToImportBefore();
            string pathToFile = Path.Combine(pathToDirectory, "SonarLint.targets");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(pathToDirectory)).Returns(false);
            fileSystem.Setup(x => x.File.Exists(pathToFile)).Returns(false);

            CreateTestSubject(fileSystem: fileSystem.Object);

            fileSystem.Verify(x => x.Directory.CreateDirectory(pathToDirectory), Times.Once);
        }

        [TestMethod]
        public void ThrowsNonCriticalException_Catches()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(It.IsAny<String>())).Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            CreateTestSubject(logger: logger, fileSystem: fileSystem.Object);

            logger.AssertPartialOutputStringExists("[ConnectedMode] Failed to write file to disk: this is a test");
        }

        [TestMethod]
        public void ThrowsCriticalException_ThrowsException()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.Exists(It.IsAny<String>())).Throws(new StackOverflowException());

            var act = () => CreateTestSubject(fileSystem: fileSystem.Object);

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void ConvertResourceToXml_DoesNotThrow()
        {
            var fileContent = GetTargetFileContent();

            var xmlDoc = new XmlDocument();
            var act = () => xmlDoc.LoadXml(fileContent);

            act.Should().NotThrow<XmlException>();
        }

        private string GetPathToImportBefore()
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            var pathToImportsBefore = Path.Combine(localAppData, "Microsoft", "MSBuild", "Current", "Microsoft.Common.targets", "ImportBefore");

            return pathToImportsBefore;
        }

        private static string GetTargetFileContent()
        {
            var resourcePath = "SonarLint.VisualStudio.ConnectedMode.Embedded.SonarLintTargets.xml";
            using var stream = new StreamReader(typeof(ImportBeforeFileGenerator).Assembly.GetManifestResourceStream(resourcePath));

            return stream.ReadToEnd();
        }

        private void CreateTestSubject(ILogger logger = null, IFileSystem fileSystem = null)
        {
            logger ??= Mock.Of<ILogger>();
            fileSystem ??= Mock.Of<IFileSystem>();

            _ = new ImportBeforeFileGenerator(logger, fileSystem);
        }
    }
}
