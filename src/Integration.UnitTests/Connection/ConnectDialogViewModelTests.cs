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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection.UI;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectDialogViewModelTests
    {
        #region Tests

        [TestMethod]
        public void ConnectDialogViewModel_GetErrorForProperty_ServerUrlRaw_NoValidationErrorWhenPristine()
        {
            // Test case 1: new model has pristine URL
            // Arrange
            var testSubject1 = new ConnectionInfoDialogViewModel();

            // Act
            string validationError1 = GetErrorForProperty(testSubject1, nameof(testSubject1.ServerUrlRaw));

            // Assert
            testSubject1.IsUrlPristine.Should().BeTrue("Server URL should be pristine on initial view model construction");
            validationError1.Should().BeNull("Validation error should be null on initial view model construction");

            // Test case 2a: setting raw server url makes it 'dirty' valid URL
            // Arrange
            var testSubject2a = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2a.ServerUrlRaw = "http://localhost";
            string validationError2a = GetErrorForProperty(testSubject2a, nameof(testSubject2a.ServerUrlRaw));

            // Assert
            testSubject2a.IsUrlPristine.Should().BeFalse("Server URL should no longer be pristine once set to a valid URL");
            validationError2a.Should().BeNull("Validation error should be null for valid URL");

            // Test case 2b: setting raw server url makes it 'dirty' invalid URL
            // Arrange
            var testSubject2b = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2b.ServerUrlRaw = "not-a-valid-url";
            string validationError2b = GetErrorForProperty(testSubject2b, nameof(testSubject2b.ServerUrlRaw));

            // Assert
            testSubject2b.IsUrlPristine.Should().BeFalse("Server URL should no longer be pristine once set to an invalid URL");
            validationError2b.Should().NotBeNull("Validation error should not be null for invalid URL");

            // Test case 3: clearing a non-pristine view model should still be non-pristine
            // Arrange
            var testSubject3 = new ConnectionInfoDialogViewModel();
            testSubject3.ServerUrlRaw = "blah"; // Makes url field dirty
            testSubject3.IsUrlPristine.Should().BeFalse("URL should be made dirty before clearing the field"); // Sanity check

            // Act
            testSubject3.ServerUrlRaw = null; // Clear field

            // Assert
            testSubject3.IsUrlPristine.Should().BeFalse("Server URL should still be non-pristine even after clearing the field");
        }

        [TestMethod]
        public void ConnectDialogViewModel_ServerUrl_SetOnlyWhenServerUrlRawIsValid()
        {
            // Arrange
            var model = new ConnectionInfoDialogViewModel();

            // Test:
            //   Invalid entry does not set ServerUrl
            model.ServerUrlRaw = "http:/localhost/";
            model.ServerUrl.Should().BeNull();

            //   Valid entry updates ServerUrl
            model.ServerUrlRaw = "http://localhost/";
            model.ServerUrl.Should().NotBeNull();
            model.ServerUrl.Should().Be(new Uri(model.ServerUrlRaw), "Uri property should match raw string property");
        }

        [TestMethod]
        public void ConnectDialogViewModel_ShowSecurityWarning_InsecureUriScheme_IsTrue()
        {
            var model = new ConnectionInfoDialogViewModel();

            model.ServerUrlRaw = "http://hostname";
            model.ShowSecurityWarning.Should().BeTrue("Security warning should be visible");

            model.ServerUrlRaw = "https://hostname";
            model.ShowSecurityWarning.Should().BeFalse("Security warning should not be visible");
        }

        #endregion Tests

        #region Helpers

        private static string GetErrorForProperty(ConnectionInfoDialogViewModel viewModel, string propertyName)
        {
            var errors = ((INotifyDataErrorInfo)viewModel).GetErrors(propertyName) as IEnumerable<string>;
            return errors?.FirstOrDefault();
        }

        #endregion Helpers
    }
}