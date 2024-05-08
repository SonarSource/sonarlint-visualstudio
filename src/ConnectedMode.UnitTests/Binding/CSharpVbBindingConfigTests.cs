/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.IO;
using System.IO.Abstractions.TestingHelpers;
using SonarLint.VisualStudio.Core.CSharpVB;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class CSharpVbBindingConfigTests
    {
        [TestMethod]
        public void Ctor_InvalidArgs()
        {
            var globalConfig = new FilePathAndContent<string>("dummy", "dummy");
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>("dummy", new SonarLintConfiguration());

            Action act = () => new CSharpVBBindingConfig(null, additionalFile);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("globalConfig");

             act = () => new CSharpVBBindingConfig(globalConfig, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("additionalFile");

            act = () => new CSharpVBBindingConfig(globalConfig, additionalFile, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Save_DirectoriesCreatedAndFilesSaved()
        {
            // Arrange
            var fileSystem = new MockFileSystem();

            var globalConfigFullPath = Path.Combine("c:\\parentDir", "savedRuleSet.txt");
            var additionalFileFullPath = Path.Combine("c:\\anotherParentDir\\ChildDir\\", "additionalFile.txt");

            var globalConfig = new FilePathAndContent<string>(globalConfigFullPath, "dummy");
            var additionalFile = new FilePathAndContent<SonarLintConfiguration>(additionalFileFullPath, new SonarLintConfiguration());

            var testSubject = new CSharpVBBindingConfig(globalConfig, additionalFile, fileSystem);

            // Act
            testSubject.Save();

            // Assert
            fileSystem.AllDirectories.Should().BeEquivalentTo(new[]
            {
                "C:\\",             // note: the MockFileSystem capitalises the drive
                "c:\\parentDir",
                "c:\\anotherParentDir",
                "c:\\anotherParentDir\\ChildDir",
            });

            fileSystem.AllFiles.Should().BeEquivalentTo(new[]
            {
                globalConfigFullPath,
                additionalFileFullPath
            });

            var savedAdditionalFile = fileSystem.File.ReadAllText(additionalFileFullPath);
            savedAdditionalFile.Should().Be(@"<?xml version=""1.0"" encoding=""utf-8""?>
<AnalysisInput xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Settings />
  <Rules />
</AnalysisInput>");
        }
    }
}
