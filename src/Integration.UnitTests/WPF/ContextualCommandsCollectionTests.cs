/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ContextualCommandsCollectionTests
    {
        [TestMethod]
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

        [TestMethod]
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

            // Case 1: AddToFolder command
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