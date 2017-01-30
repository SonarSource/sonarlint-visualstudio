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

using FluentAssertions;
using SonarLint.VisualStudio.Integration.Connection.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{

    public class ConnectDialogViewModelTests
    {
        #region Tests

        [Fact]
        public void ConnectDialogViewModel_GetErrorForProperty_ServerUrlRaw_NoValidationErrorWhenPristine()
        {
            // Test case 1: new model has prestine URL
            // Arrange
            var testSubject1 = new ConnectionInfoDialogViewModel();

            // Act
            string validationError1 = GetErrorForProperty(testSubject1, nameof(testSubject1.ServerUrlRaw));

            // Assert
            testSubject1.IsUrlPristine.Should().BeTrue("Server URL should be prestine on initial view model construction");
            validationError1.Should().BeNull("Validation error should be null on initial view model construction");


            // Test case 2a: setting raw server url makes it 'dirty' valid URL
            // Arrange
            var testSubject2a = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2a.ServerUrlRaw = "http://localhost";
            string validationError2a = GetErrorForProperty(testSubject2a, nameof(testSubject2a.ServerUrlRaw));

            // Assert
            testSubject2a.IsUrlPristine
                .Should().BeFalse("Server URL should no longer be prestine once set to a valid URL");
            validationError2a
                .Should().BeNull("Validation error should be null for valid URL");


            // Test case 2b: setting raw server url makes it 'dirty' invalid URL
            // Arrange
            var testSubject2b = new ConnectionInfoDialogViewModel();

            // Act
            testSubject2b.ServerUrlRaw = "not-a-valid-url";
            string validationError2b = GetErrorForProperty(testSubject2b, nameof(testSubject2b.ServerUrlRaw));

            // Assert
            testSubject2b.IsUrlPristine
                .Should().BeFalse("Server URL should no longer be prestine once set to an invalid URL");
            validationError2b
                .Should().NotBeNull("Validation error should not be null for invalid URL");


            // Test case 3: clearing a non-prestine view model should still be non-prestine
            // Arrange
            var testSubject3 = new ConnectionInfoDialogViewModel();
            testSubject3.ServerUrlRaw = "blah"; // Makes url field dirty
            testSubject3.IsUrlPristine
                .Should().BeFalse("URL should be made dirty before clearing the field"); // Sanity check

            // Act
            testSubject3.ServerUrlRaw = null; // Clear field

            // Assert
            testSubject3.IsUrlPristine
                .Should().BeFalse("Server URL should still be non-prestine even after clearing the field");
        }

        [Fact]
        public void ConnectDialogViewModel_ServerUrl_SetOnlyWhenServerUrlRawIsValid()
        {
            // Arrange
            var model = new ConnectionInfoDialogViewModel();

            // Test:
            //   Invalid entry does not set ServerUrl
            model.ServerUrlRaw = "http:/localhost/";
            model.ServerUrl.Should().BeNull("ServerUrl should not be set");

            //   Valid entry updates ServerUrl
            model.ServerUrlRaw = "http://localhost/";
            model.ServerUrl.Should().NotBeNull("ServerUrl should set");
            model.ServerUrl.Should().Be(new Uri(model.ServerUrlRaw), "Uri property should match raw string property");
        }

        [Fact]
        public void ConnectDialogViewModel_ShowSecurityWarning_InsecureUriScheme_IsTrue()
        {
            var model = new ConnectionInfoDialogViewModel();

            model.ServerUrlRaw = "http://hostname";
            model.ShowSecurityWarning.Should().BeTrue("Security warning should be visible");

            model.ServerUrlRaw = "https://hostname";
            model.ShowSecurityWarning.Should().BeFalse("Security warning should not be visible");
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
