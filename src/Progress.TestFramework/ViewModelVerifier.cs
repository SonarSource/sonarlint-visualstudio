/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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