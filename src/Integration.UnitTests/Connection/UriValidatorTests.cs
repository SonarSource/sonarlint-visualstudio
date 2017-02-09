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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class UriValidatorTests
    {
        private string secureScheme;
        private string insecureScheme;
        private string unsupportedScheme;
        private ConfigurableUriValidator validator;

        [TestInitialize]
        public void TestInitialize()
        {
            this.secureScheme = "safe";
            this.insecureScheme = "unsafe";
            this.unsupportedScheme = "unknown";

            var supportedSchemes = new HashSet<string>(new[] { this.secureScheme, this.insecureScheme });
            var insecureSchemes = new HashSet<string>(new[] { this.insecureScheme });

            this.validator = new ConfigurableUriValidator(supportedSchemes, insecureSchemes);
        }

        [TestMethod]
        public void UriValidator_Ctor_ArgumentNullException()
        {
            var emptySet = new HashSet<string>();

            Exceptions.Expect<ArgumentNullException>(() => new UriValidator(null));
            Exceptions.Expect<ArgumentNullException>(() => new UriValidator(null, emptySet));
            Exceptions.Expect<ArgumentNullException>(() => new UriValidator(emptySet, null));
        }

        [TestMethod]
        public void UriValidator_IsSupportedScheme_LowercaseValidator_CaseInsensitive()
        {
            var lowercaseValidator = new UriValidator(
                supportedSchemes: new HashSet<string>(new[] { "case" }),
                insecureSchemes: new HashSet<string>(new[] { "case" })
            );

            VerifyIsSupportedSchemeCaseSensitivity(lowercaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsSupportedScheme_UppercaseValidator_CaseInsensitive()
        {
            var uppercaseValidator = new UriValidator(
                supportedSchemes: new HashSet<string>(new[] { "CASE" }),
                insecureSchemes: new HashSet<string>(new[] { "CASE" })
            );

            VerifyIsSupportedSchemeCaseSensitivity(uppercaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsSupportedScheme_MixedValidator_CaseInsensitive()
        {
            var mixedcaseValidator = new UriValidator(
                supportedSchemes: new HashSet<string>(new[] { "cAsE" }),
                insecureSchemes: new HashSet<string>(new[] { "cAsE" })
            );

            VerifyIsSupportedSchemeCaseSensitivity(mixedcaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsInsecureScheme_LowercaseValidator_CaseInsensitive()
        {
            var lowercaseValidator = new UriValidator(
                supportedSchemes: new HashSet<string>(new[] { "case" }),
                insecureSchemes: new HashSet<string>(new[] { "case" })
            );

            VerifyIsInsecureSchemeCaseSensitivity(lowercaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsInsecureScheme_UppercaseValidator_CaseInsensitive()
        {
            var uppercaseValidator = new UriValidator(
                    supportedSchemes: new HashSet<string>(new[] { "CASE" }),
                    insecureSchemes: new HashSet<string>(new[] { "CASE" })
            );

            VerifyIsInsecureSchemeCaseSensitivity(uppercaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsInsecureScheme_MixedcaseValidator_CaseInsensitive()
        {
            var mixedcaseValidator = new UriValidator(
                    supportedSchemes: new HashSet<string>(new[] { "cAsE" }),
                    insecureSchemes: new HashSet<string>(new[] { "cAsE" })
            );

            VerifyIsInsecureSchemeCaseSensitivity(mixedcaseValidator);
        }

        [TestMethod]
        public void UriValidator_IsSupportedScheme_SupportedSchemes()
        {
            // Test
            bool isSecureUriSupported = this.validator.IsSupportedScheme(this.SecureUri);
            bool isInsecureUriSupported = this.validator.IsSupportedScheme(this.InsecureUri);
            bool isInsupportedUriSupported = this.validator.IsSupportedScheme(this.UnsupportedUri);

            // Verify
            isSecureUriSupported.Should().BeTrue();
            isInsecureUriSupported.Should().BeTrue();
            isInsupportedUriSupported.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_IsInsecureScheme_InsecureSchemesAreInsecure()
        {
            // Test
            bool isSecureUriSecure = this.validator.IsInsecureScheme(this.SecureUri);
            bool isInsecureUriSecure = this.validator.IsInsecureScheme(this.InsecureUri);

            // Verify
            isSecureUriSecure.Should().BeFalse();
            isInsecureUriSecure.Should().BeTrue();
        }

        [TestMethod]
        public void UriValidator_IsSecureScheme_UnsupportedScheme_ReturnsFalse()
        {
            // Test
            bool isSecure = this.validator.IsInsecureScheme(this.UnsupportedUri);

            // Verify
            isSecure.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_Ctor_InsecureSchemesIsSubsetOfSupportedSchemes()
        {
            // Setup
            var supportedSchemes = new HashSet<string>(new[] { "a", "b" });
            var insecureSchemesSubset = new HashSet<string>(new[] { "b" });
            var insecureSchemesNotSubset = new HashSet<string>(new[] { "b", "c" });

            // Test 1: is a subset
            new UriValidator(supportedSchemes, insecureSchemesSubset);

            // Test 2: not a subset
            Exceptions.Expect<ArgumentException>(() =>
            {
                new UriValidator(supportedSchemes, insecureSchemesNotSubset);
            });
        }

        [TestMethod]
        public void UriValidator_IsValidUri_String_NullOrEmptyUri_IsInvalid()
        {
            // Setup
            string nullUriString = null;
            string emptyUriString = string.Empty;

            // Test
            bool nullResult = this.validator.IsValidUri(nullUriString);
            bool emptyResult = this.validator.IsValidUri(emptyUriString);

            // Verify
            nullResult.Should().BeFalse();
            emptyResult.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_IsValidUri_Uri_NullUri_ThrowsException()
        {
            // Setup
            Uri uri = null;

            // Test
            Exceptions.Expect<ArgumentNullException>(() => this.validator.IsValidUri(uri));
        }

        [TestMethod]
        public void UriValidator_IsValidUri_String_AbsoluteUrisOnly()
        {
            // Setup
            string relativeUriString = "/Home/Index";

            // Test
            bool result = this.validator.IsValidUri(relativeUriString);

            // Verify
            result.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_IsValidUri_IncompleteUri_IsInvalid()
        {
            string uriString = "http:/";

            bool result = this.validator.IsValidUri(uriString);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_IsValidUri_AmbiguousScheme_IsInvalid()
        {
            string uriString = "//localhost";

            bool result = this.validator.IsValidUri(uriString);

            result.Should().BeFalse();
        }

        [TestMethod]
        public void UriValidator_IsValidUri_UriWithPort_IsValid()
        {
            // Setup
            string uriString = CreateUriString(this.secureScheme, "localhost:9001");

            // Test
            bool result = this.validator.IsValidUri(uriString);

            // Verify
            result.Should().BeTrue();
        }

        [TestMethod]
        public void UriValidator_IsValidUri_LongUriWithFragments_IsValid()
        {
            // Setup
            string uriString = CreateUriString(this.secureScheme, "localhost:9001/this/is/a/longer/uri/that/should/be?valid=true");

            // Test
            bool result = this.validator.IsValidUri(uriString);

            // Verify
            result.Should().BeTrue();
        }

        #region Helpers

        private Uri InsecureUri => CreateUri(this.insecureScheme, "localhost");

        private Uri SecureUri => CreateUri(this.secureScheme, "localhost");

        private Uri UnsupportedUri => CreateUri(this.unsupportedScheme, "localhost");

        private static string CreateUriString(string scheme, string remainingUri) => $"{scheme}://{remainingUri}";

        private static Uri CreateUri(string scheme, string remainingUri) => new Uri(CreateUriString(scheme, remainingUri));

        private void VerifyIsInsecureSchemeCaseSensitivity(UriValidator validator)
        {
            Uri lowercaseUri = CreateUri("case", "localhost");
            Uri uppercaseUri = CreateUri("CASE", "localhost");
            Uri mixedcaseUri = CreateUri("cAsE", "localhost");

            // Test
            bool lowercaseInsecure = validator.IsInsecureScheme(lowercaseUri);
            bool uppercaseInsecure = validator.IsInsecureScheme(uppercaseUri);
            bool mixedcaseInsecure = validator.IsInsecureScheme(mixedcaseUri);

            // Verify
            lowercaseInsecure.Should().BeTrue("Lowercase scheme should be insecure");
            uppercaseInsecure.Should().BeTrue("Uppercase scheme should be insecure");
            mixedcaseInsecure.Should().BeTrue("Mixed-case scheme should be insecure");
        }

        private void VerifyIsSupportedSchemeCaseSensitivity(UriValidator validator)
        {
            Uri lowercaseUri = CreateUri("case", "localhost");
            Uri uppercaseUri = CreateUri("CASE", "localhost");
            Uri mixedcaseUri = CreateUri("cAsE", "localhost");

            // Test
            bool lowercaseSupported = validator.IsSupportedScheme(lowercaseUri);
            bool uppercaseSupported = validator.IsSupportedScheme(uppercaseUri);
            bool mixedcaseSupported = validator.IsSupportedScheme(mixedcaseUri);

            // Verify
            lowercaseSupported.Should().BeTrue("Lowercase scheme should be supported");
            uppercaseSupported.Should().BeTrue("Uppercase scheme should be supported");
            mixedcaseSupported.Should().BeTrue("Mixed-case scheme should be supported");
        }

        #endregion Helpers
    }
}