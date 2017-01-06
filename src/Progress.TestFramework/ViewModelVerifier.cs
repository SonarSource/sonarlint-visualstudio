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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Helper class that verifies the <see cref="INotifyPropertyChanged"/> derived types
    /// </summary>
    public class ViewModelVerifier
    {
        private readonly INotifyPropertyChanged propertyChangeProvider;
        private readonly List<string> propertyChanges = new List<string>();

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
            Assert.AreNotEqual(value1, value2, "Test error: cannot run test with two equal values");

            // Setup
            PropertyInfo property = typeof(M).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            Assert.IsNotNull(property, "Test error: Cannot find public property (get and set ) {0}", propertyName);
            Action<T> setProperty = (v) => property.SetValue(vm, v);
            Func<T> getProperty = () => (T)property.GetValue(vm);
            setProperty(value1);
            Assert.AreEqual(value1, getProperty(), "Test error: getProperty and setProperty are not working as expected");
            ViewModelVerifier verifier = new ViewModelVerifier(vm);

            // Change
            setProperty(value2);
            verifier.AssertSinglePropertyChange(propertyName);
            verifier.Reset();

            // No change
            setProperty(value2);
            verifier.AssertNoPropertChanges();

            // Change again
            setProperty(value1);
            verifier.AssertSinglePropertyChange(propertyName);
        }

        /// <summary>
        /// Resets the verifier (property changes)
        /// </summary>
        public void Reset()
        {
            this.propertyChanges.Clear();
        }

        /// <summary>
        /// Asserts if only one property change occurred
        /// </summary>
        /// <param name="propertyName">The name of the single property that was changed</param>
        public void AssertSinglePropertyChange(string propertyName)
        {
            Assert.AreEqual(propertyName, this.propertyChanges.FirstOrDefault(), "Unexpected property change");
            Assert.AreEqual(1, this.propertyChanges.Count, "Unexpected number of property changes");
        }

        /// <summary>
        /// Asserts if all the property changes occurred (in order specified)
        /// </summary>
        /// <param name="propertyNames">The name of the properties that were changed (in order of change)</param>
        public void AssertOrderedPropertyChanges(params string[] propertyNames)
        {
            Assert.AreEqual(propertyNames.Length, this.propertyChanges.Count, "Unexpected number of property changes");
            for (int i = 0; i < propertyNames.Length; i++)
            {
                Assert.AreEqual(propertyNames[i], this.propertyChanges[i], "Unexpected property change at index {0}", new object[] { i });
            }
        }

        /// <summary>
        /// Asserts if there are no property changes
        /// </summary>
        public void AssertNoPropertChanges()
        {
            Assert.AreEqual(0, this.propertyChanges.Count, "Not expecting any property changes");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.propertyChanges.Add(e.PropertyName);
        }
    }
}
