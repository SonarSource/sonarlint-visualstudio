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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using FluentAssertions;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Helper class that verifies the <see cref="INotifyPropertyChanged"/> derived types
    /// </summary>
    public class ViewModelVerifier
    {
        private readonly INotifyPropertyChanged propertyChangeProvider;
        internal readonly List<string> propertyChanges = new List<string>();

        public ViewModelVerifier(INotifyPropertyChanged model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            this.propertyChangeProvider = model;
            this.propertyChangeProvider.PropertyChanged += this.OnPropertyChanged;
        }

        public static void RunVerificationTest<M, T>(M vm, string propertyName, T value1, T value2)
          where M : System.ComponentModel.INotifyPropertyChanged, new()
        {
            value2.Should().NotBe(value1, "Test error: cannot run test with two equal values");

            // Arrange
            PropertyInfo property = typeof(M).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            property.Should().NotBeNull("Test error: Cannot find public property (get and set ) {0}", propertyName);
            Action<T> setProperty = (v) => property.SetValue(vm, v);
            Func<T> getProperty = () => (T)property.GetValue(vm);
            setProperty(value1);
            getProperty().Should().Be(value1, "Test error: getProperty and setProperty are not working as expected");
            ViewModelVerifier verifier = new ViewModelVerifier(vm);

            // Change
            setProperty(value2);
            verifier.propertyChanges.Should().ContainSingle(propertyName);
            verifier.Reset();

            // No change
            setProperty(value2);
            verifier.propertyChanges.Should().BeEmpty();

            // Change again
            setProperty(value1);
            verifier.propertyChanges.Should().ContainSingle(propertyName);
        }

        /// <summary>
        /// Resets the verifier (property changes)
        /// </summary>
        public void Reset()
        {
            this.propertyChanges.Clear();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.propertyChanges.Add(e.PropertyName);
        }
    }
}