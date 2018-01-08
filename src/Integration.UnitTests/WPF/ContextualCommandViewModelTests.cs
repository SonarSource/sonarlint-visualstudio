/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Collections.Generic;
using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.UnitTests.WPF
{
    [TestClass]
    public class ContextualCommandViewModelTests
    {
        [TestMethod]
        public void ContextualCommandViewModel_Ctor_NullArgChecks()
        {
            var command = new RelayCommand(() => { });
            ContextualCommandViewModel suppressAnalysisWarning;
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                suppressAnalysisWarning = new ContextualCommandViewModel(null, command);
            });
        }

        [TestMethod]
        public void ContextualCommandViewModel_CommandInvocation()
        {
            // Arrange
            bool canExecute = false;
            bool executed = false;
            var realCommand = new RelayCommand<object>(
                (state) =>
                {
                    state.Should().Be(this);
                    executed = true;
                },
                (state) =>
                {
                    state.Should().Be(this);
                    return canExecute;
                });
            var testSubject = new ContextualCommandViewModel(this, realCommand);

            // Sanity
            testSubject.Command.Should().NotBeNull();
            testSubject.InternalRealCommand.Should().Be(realCommand);

            // Case 1: Can't execute
            canExecute = false;
            // Act
            testSubject.Command.CanExecute(null).Should().BeFalse("CanExecute wasn't called as expected");

            // Case 2: Can execute
            canExecute = true;

            // Act
            testSubject.Command.CanExecute(null).Should().BeTrue("CanExecute wasn't called as expected");

            // Case 3: Execute
            // Act
            testSubject.Command.Execute(null);
            executed.Should().BeTrue("Execute wasn't called as expected");
        }

        [TestMethod]
        public void ContextualCommandViewModel_DisplayText()
        {
            // Arrange
            var context = new object();
            var command = new RelayCommand(() => { });
            var testSubject = new ContextualCommandViewModel(context, command);

            using (var tracker = new PropertyChangedTracker(testSubject))
            {
                // Case 1: null
                // Act + Assert
                testSubject.DisplayText.Should().BeNull("Expected display text to return null when not set");

                // Case 2: static
                testSubject.DisplayText = "foobar9000";
                // Act + Assert
                testSubject.DisplayText.Should().Be("foobar9000", "Unexpected static display text");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.DisplayText), 1);

                // Case 3: dynamic
                var funcInvoked = false;
                Func<object, string> func = x =>
                {
                    funcInvoked = true;
                    return "1234";
                };
                testSubject.SetDynamicDisplayText(func);
                // Act + Assert
                testSubject.DisplayText.Should().Be("1234", "Unexpected dynamic display text");
                funcInvoked.Should().BeTrue("Dynamic display text function was not invoked");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.DisplayText), 2);
            }

            // Case 4: dynamic - null exception
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetDynamicDisplayText(null));
        }

        [TestMethod]
        public void ContextualCommandViewModel_Icon()
        {
            // Arrange
            var context = new object();
            var command = new RelayCommand(() => { });
            var testSubject = new ContextualCommandViewModel(context, command);

            using (var tracker = new PropertyChangedTracker(testSubject))
            {
                // Case 1: null
                // Act + Assert
                testSubject.Icon.Should().BeNull("Expected icon to return null when not set");

                // Case 2: static
                var staticIcon = new IconViewModel(null);
                testSubject.Icon = staticIcon;
                // Act + Assert
                testSubject.Icon.Should().Be(staticIcon, "Unexpected static icon");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.Icon), 1);

                // Case 3: dynamic
                var dynamicIcon = new IconViewModel(null);
                var funcInvoked = false;
                Func<object, IconViewModel> func = x =>
                {
                    funcInvoked = true;
                    return dynamicIcon;
                };
                testSubject.SetDynamicIcon(func);
                // Act + Assert
                testSubject.Icon.Should().Be(dynamicIcon, "Unexpected dynamic icon");
                funcInvoked.Should().BeTrue("Dynamic icon function  was not invoked");
                tracker.AssertPropertyChangedRaised(nameof(testSubject.Icon), 2);
            }

            // Case 4: dynamic - null exception
            Exceptions.Expect<ArgumentNullException>(() => testSubject.SetDynamicIcon(null));
        }

        private sealed class PropertyChangedTracker : IDisposable
        {
            private readonly INotifyPropertyChanged subject;
            private readonly IDictionary<string, int> trackingDictionary = new Dictionary<string, int>();

            public PropertyChangedTracker(INotifyPropertyChanged subject)
            {
                this.subject = subject;
                this.subject.PropertyChanged += this.OnPropertyChanged;
            }

            public void AssertPropertyChangedRaised(string propertyName, int count)
            {
                (count > 0 || this.trackingDictionary.ContainsKey(propertyName)).Should().BeTrue($"PropertyChanged was not raised for '{propertyName}'");
                this.trackingDictionary[propertyName].Should().Be(count, "Unexpected number of PropertyChanged events raised");
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

            private bool isDisposed;

            private void Dispose(bool disposing)
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

            #endregion IDisposable
        }
    }
}