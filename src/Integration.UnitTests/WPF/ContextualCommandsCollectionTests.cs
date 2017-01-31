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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;
using System.ComponentModel;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    public class ContextualCommandsCollectionTests
    {
        [Fact]
        public void ContextualCommandsCollection_HasCommands()
        {
            // Arrange
            var testSubject = new ContextualCommandsCollection();

            // Case 1: no commands
            // Act + Assert
            testSubject.HasCommands.Should().BeFalse();

            // Case 2: has commands
            testSubject.Add(new ContextualCommandViewModel(this, new RelayCommand(()=> { })));
            // Act + Assert
            testSubject.HasCommands.Should().BeTrue();
        }

        [Fact]
        public void ContextualCommandsCollection_HasCommands_ChangedOnCollectionChange()
        {
            // Arrange
            var testSubject = new ContextualCommandsCollection();
            int hasCommandsChangedCounter = 0;
            ((INotifyPropertyChanged)testSubject).PropertyChanged += (o, e) =>
              {
                  if (e.PropertyName == "HasCommands")
                  {
                      hasCommandsChangedCounter++;
                  }
              };

            // Case 1: Add command
            var cmd1 = new ContextualCommandViewModel(this, new RelayCommand(() => { }));
            var cmd2 = new ContextualCommandViewModel(this, new RelayCommand(() => { }));
            // Act
            testSubject.Add(cmd1);
            testSubject.Add(cmd2);

            // Assert
            hasCommandsChangedCounter.Should().Be(2, "Adding a command should update HasCommands");

            // Case 2: Remove command
            // Act
            testSubject.Remove(cmd1);
            // Assert
            hasCommandsChangedCounter.Should().Be(3, "Adding a command should update HasCommands");

            // Case 3: Update command
            // Act
            testSubject[0] = cmd1;
            // Assert
            hasCommandsChangedCounter.Should().Be(4, "Adding a command should update HasCommands");
        }

    }
}
