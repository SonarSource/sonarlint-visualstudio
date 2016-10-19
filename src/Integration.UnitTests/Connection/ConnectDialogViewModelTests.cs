//-----------------------------------------------------------------------
// <copyright file="ConnectDialogViewModelTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectDialogViewModelTests
    {
        #region Tests

        [TestMethod]
        public void ConnectDialogViewModel_GetErrorForProperty_ServerUrlRaw_NoValidationErrorWhenPristine()
        {
            // Test case 1: new model has prestine URL
            // Setup
            var testSubject1 = new ConnectionInfoDialogViewModel();

            // Act
            string validationError1 = GetErrorForProperty(testSubject1, nameof(testSubject1.ServerUrlRaw));

            // Verify
            Assert.IsTrue(testSubject1.IsUrlPristine, "Server URL should be prestine on initial view model construction");
            Assert.IsNull(validationError1, "Validation error should be null on initial view model construction");


            // Test case 2a: setting raw server url makes it 'dirty' – valid URL
            // Setup
            var testSubject2a = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2a.ServerUrlRaw = "http://localhost";
            string validationError2a = GetErrorForProperty(testSubject2a, nameof(testSubject2a.ServerUrlRaw));

            // Verify
            Assert.IsFalse(testSubject2a.IsUrlPristine, "Server URL should no longer be prestine once set to a valid URL");
            Assert.IsNull(validationError2a, "Validation error should be null for valid URL");


            // Test case 2b: setting raw server url makes it 'dirty' – invalid URL
            // Setup
            var testSubject2b = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2b.ServerUrlRaw = "not-a-valid-url";
            string validationError2b = GetErrorForProperty(testSubject2b, nameof(testSubject2b.ServerUrlRaw));

            // Verify
            Assert.IsFalse(testSubject2b.IsUrlPristine, "Server URL should no longer be prestine once set to an invalid URL");
            Assert.IsNotNull(validationError2b, "Validation error should not be null for invalid URL");


            // Test case 3: clearing a non-prestine view model should still be non-prestine
            // Setup
            var testSubject3 = new ConnectionInfoDialogViewModel();
            testSubject3.ServerUrlRaw = "blah"; // Makes url field dirty
            Assert.IsFalse(testSubject3.IsUrlPristine, "URL should be made dirty before clearing the field"); // Sanity check

            // Act
            testSubject3.ServerUrlRaw = null; // Clear field

            // Verify
            Assert.IsFalse(testSubject3.IsUrlPristine, "Server URL should still be non-prestine even after clearing the field");
        }

        [TestMethod]
        public void ConnectDialogViewModel_ServerUrl_SetOnlyWhenServerUrlRawIsValid()
        {
            // Setup
            var model = new ConnectionInfoDialogViewModel();

            // Test:
            //   Invalid entry does not set ServerUrl
            model.ServerUrlRaw = "http:/localhost/";
            Assert.IsTrue(model.ServerUrl == null, "ServerUrl should not be set");

            //   Valid entry updates ServerUrl
            model.ServerUrlRaw = "http://localhost/";
            Assert.IsFalse(model.ServerUrl == null, "ServerUrl should set");
            Assert.AreEqual(new Uri(model.ServerUrlRaw), model.ServerUrl, "Uri property should match raw string property");
        }

        [TestMethod]
        public void ConnectDialogViewModel_ShowSecurityWarning_InsecureUriScheme_IsTrue()
        {
            var model = new ConnectionInfoDialogViewModel();

            model.ServerUrlRaw = "http://hostname";
            Assert.IsTrue(model.ShowSecurityWarning, "Security warning should be visible");

            model.ServerUrlRaw = "https://hostname";
            Assert.IsFalse(model.ShowSecurityWarning, "Security warning should not be visible");
        }

        #endregion

        #region Helpers

        private static string GetErrorForProperty(ConnectionInfoDialogViewModel viewModel, string propertyName)
        {
            var errors = ((INotifyDataErrorInfo)viewModel).GetErrors(propertyName) as IEnumerable<string>;
            return errors?.FirstOrDefault();
        }

        #endregion
    }
}
