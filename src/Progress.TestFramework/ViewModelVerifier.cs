//-----------------------------------------------------------------------
// <copyright file="ViewModelVerifier.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
