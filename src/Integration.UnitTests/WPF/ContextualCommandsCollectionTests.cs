//-----------------------------------------------------------------------
// <copyright file="ContextualCommandsCollectionTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ContextualCommandsCollectionTests
    {
        [TestMethod]
        public void ContextualCommandsCollection_HasCommands()
        {
            // Setup
            var testSubject = new ContextualCommandsCollection();

            // Case 1: no commands
            // Act + Verify
            Assert.IsFalse(testSubject.HasCommands);

            // Case 2: has commands
            testSubject.Add(new ContextualCommandViewModel(this, new RelayCommand(()=> { })));
            // Act + Verify
            Assert.IsTrue(testSubject.HasCommands);
        }

        [TestMethod]
        public void ContextualCommandsCollection_HasCommands_ChangedOnCollectionChange()
        {
            // Setup
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

            // Verify
            Assert.AreEqual(2, hasCommandsChangedCounter, "Adding a command should update HasCommands");

            // Case 2: Remove command
            // Act
            testSubject.Remove(cmd1);
            // Verify
            Assert.AreEqual(3, hasCommandsChangedCounter, "Adding a command should update HasCommands");

            // Case 3: Update command
            // Act
            testSubject[0] = cmd1;
            // Verify
            Assert.AreEqual(4, hasCommandsChangedCounter, "Adding a command should update HasCommands");
        }

    }
}
