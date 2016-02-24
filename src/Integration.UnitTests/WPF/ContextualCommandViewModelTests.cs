//-----------------------------------------------------------------------
// <copyright file="ContextualCommandViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.WPF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ContextualCommandViewModelTests
    {
        [TestMethod]
        public void ContextualCommandViewModel_Ctor_NullArgChecks()
        {
            var context = new object();
            var command = new RelayCommand(() => { });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                new ContextualCommandViewModel(null, command);
            });

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                new ContextualCommandViewModel(context, null);
            });
        }

        [TestMethod]
        public void ContextualCommandViewModel_CommandInvocation()
        {
            // Setup
            bool canExecute = false;
            bool executed = false;
            var realCommand = new RelayCommand<object>(
                (state) => { Assert.AreEqual(this, state); executed = true; },
                (state) => { Assert.AreEqual(this, state); return canExecute; });
            var testSubject = new ContextualCommandViewModel(this, realCommand);

            // Sanity
            Assert.IsNotNull(testSubject.Command);
            Assert.AreSame(realCommand, testSubject.InternalRealCommand);

            // Case 1: Can't execute
            canExecute = false;
            // Act
            Assert.IsFalse(testSubject.Command.CanExecute(null), "CanExecute wasn't called as expected");

            // Case 2: Can execute
            canExecute = true;
            // Act
            Assert.IsTrue(testSubject.Command.CanExecute(null), "CanExecute wasn't called as expected");

            // Case 3: Execute
            // Act
            testSubject.Command.Execute(null);
            Assert.IsTrue(executed, "Execute wasn't called as expected");
        }

        [TestMethod]
        public void ContextualCommandViewModel_DisplayText()
        {
            // Setup
            var context = new object();
            var command = new RelayCommand(() => { });
            var testSubject = new ContextualCommandViewModel(context, command);

            using (var tracker = new PropertyChangedTracker(testSubject))
            {
                // Case 1: null
                // Act + Verify
                Assert.IsNull(testSubject.DisplayText, "Expected display text to return null when not set");

                // Case 2: static
                testSubject.DisplayText = "foobar9000";
                // Act + Verify
                Assert.AreEqual("foobar9000", testSubject.DisplayText, "Unexpected static display text");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.DisplayText), 1);

                // Case 3: dynamic
                var funcInvoked = false;
                Func<object, string> func = x => { funcInvoked = true; return "1234"; };
                testSubject.SetDynamicDisplayText(func);
                // Act + Verify
                Assert.AreEqual("1234", testSubject.DisplayText, "Unexpected dynamic display text");
                Assert.IsTrue(funcInvoked, "Dynamic display text function was not invoked");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.DisplayText), 2);
            }

            // Case 4: dynamic - null exception
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetDynamicDisplayText(null));
        }

        [TestMethod]
        public void ContextualCommandViewModel_Icon()
        {
            // Setup
            var context = new object();
            var command = new RelayCommand(() => { });
            var testSubject = new ContextualCommandViewModel(context, command);

            using (var tracker = new PropertyChangedTracker(testSubject))
            {
                // Case 1: null
                // Act + Verify
                Assert.IsNull(testSubject.Icon, "Expected icon to return null when not set");

                // Case 2: static
                var staticIcon = new IconViewModel(null);
                testSubject.Icon = staticIcon;
                // Act + Verify
                Assert.AreSame(staticIcon, testSubject.Icon, "Unexpected static icon");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.Icon), 1);

                // Case 3: dynamic
                var dynamicIcon = new IconViewModel(null);
                var funcInvoked = false;
                Func<object, IconViewModel> func = x => { funcInvoked = true; return dynamicIcon; };
                testSubject.SetDynamicIcon(func);
                // Act + Verify
                Assert.AreSame(dynamicIcon, testSubject.Icon, "Unexpected dynamic icon");
                Assert.IsTrue(funcInvoked, "Dynamic icon function  was not invoked");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.Icon), 2);
            }

            // Case 4: dynamic - null exception
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetDynamicIcon(null));
        }

        private class PropertyChangedTracker : IDisposable
        {
            private INotifyPropertyChanged subject;
            private IDictionary<string, int> trackingDictionary = new Dictionary<string, int>();

            public PropertyChangedTracker(INotifyPropertyChanged subject)
            {
                this.subject = subject;
                this.subject.PropertyChanged += this.OnPropertyChanged;
            }

            public void AssertPropertyChangedRaised(string propertyName, int count)
            {
                Assert.IsTrue(count > 0 || this.trackingDictionary.ContainsKey(propertyName), $"PropertyChanged was not raised for '{propertyName}'");
                Assert.AreEqual(count, this.trackingDictionary[propertyName], "Unexpected number of PropertyChanged events raised");
            }

            private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (!this.trackingDictionary.ContainsKey(e.PropertyName))
                {
                    this.trackingDictionary[e.PropertyName] = 0;
                }
                this.trackingDictionary[e.PropertyName]++;
            }

            #region IDisposable

            private bool isDisposed = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!isDisposed)
                {
                    if (disposing)
                    {
                        this.subject.PropertyChanged -= this.OnPropertyChanged;
                    }

                    isDisposed = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            #endregion
        }
    }
}
