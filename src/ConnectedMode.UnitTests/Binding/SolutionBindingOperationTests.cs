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

using System.IO.Abstractions.TestingHelpers;
using System.Threading;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.UnitTests
{
    [TestClass]
    public class SolutionBindingOperationTests
    {
        #region Tests

        [TestMethod]
        public void SaveConfiguration_SolutionLevelFilesAreSaved()
        {
            // Arrange
            var config1 = CreateBindingConfig("c:\\csharp.txt");
            var config2 = CreateBindingConfig("c:\\vb.txt");

            var testSubject = CreateTestSubject();

            var bindingConfigs = new IBindingConfig[]
            {
                config1.Object,
                config2.Object
            };

            // Act
            testSubject.SaveRuleConfiguration(bindingConfigs, CancellationToken.None);

            // Assert
            CheckConfigWasSaved(config1);
            CheckConfigWasSaved(config2);
        }

        [TestMethod]
        public void SaveConfiguration_DirectoryiesAreCreated()
        {
            // Arrange
            var config1 = CreateBindingConfig("c:\\x.txt");
            var config2 = CreateBindingConfig("c:\\XXX\\any.txt", "D:\\YYY\\any.txt");
            var config3 = CreateBindingConfig("c:\\aaa\\bbb\\any.txt");

            var fileSystem = new MockFileSystem();

            var testSubject = CreateTestSubject(fileSystem);

            var bindingConfigs = new IBindingConfig[]
            {
                config1.Object,
                config2.Object,
                config3.Object
            };

            // Act
            testSubject.SaveRuleConfiguration(bindingConfigs, CancellationToken.None);

            // Assert
            fileSystem.AllDirectories.Should().BeEquivalentTo(new string[]
                {
                    "C:\\",             // note: the MockFileSystem capitalises the drive
                    "c:\\aaa",          // note: the MockFileSystem lists the parent directory separately
                    "c:\\aaa\\bbb",
                    "c:\\XXX",

                    "D:\\",
                    "D:\\YYY"
                });
        }

        #endregion Tests

        #region Helpers

        private SolutionBindingOperation CreateTestSubject(MockFileSystem fileSystem = null)
        {
            fileSystem ??= new MockFileSystem();
            return new SolutionBindingOperation(fileSystem);
        }

        private Mock<IBindingConfig> CreateBindingConfig(params string[] slnLevelFilePaths)
        {
            var config = new Mock<IBindingConfig>();
            config.SetupGet(x => x.SolutionLevelFilePaths).Returns(slnLevelFilePaths);

            return config;
        }

        private static void CheckConfigWasSaved(Mock<IBindingConfig> config)
            => config.Verify(x => x.Save(), Times.Once);

        #endregion Helpers
    }
}
