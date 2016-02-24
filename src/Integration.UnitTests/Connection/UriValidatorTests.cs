//-----------------------------------------------------------------------
// <copyright file="UriValidatorTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
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
            Assert.IsTrue(isSecureUriSupported);
            Assert.IsTrue(isInsecureUriSupported);
            Assert.IsFalse(isInsupportedUriSupported);
        }

        [TestMethod]
        public void UriValidator_IsInsecureScheme_InsecureSchemesAreInsecure()
        {
            // Test
            bool isSecureUriSecure = this.validator.IsInsecureScheme(this.SecureUri);
            bool isInsecureUriSecure = this.validator.IsInsecureScheme(this.InsecureUri);

            // Verify
            Assert.IsFalse(isSecureUriSecure);
            Assert.IsTrue(isInsecureUriSecure);
        }

        [TestMethod]
        public void UriValidator_IsSecureScheme_UnsupportedScheme_ReturnsFalse()
        {
            // Test
            bool isSecure = this.validator.IsInsecureScheme(this.UnsupportedUri);

            // Verify
            Assert.IsFalse(isSecure);
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
            Assert.IsFalse(nullResult);
            Assert.IsFalse(emptyResult);
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
            Assert.IsFalse(result);
        }


        [TestMethod]
        public void UriValidator_IsValidUri_IncompleteUri_IsInvalid()
        {
            string uriString = "http:/";

            bool result = this.validator.IsValidUri(uriString);

            Assert.IsFalse(result);
        }


        [TestMethod]
        public void UriValidator_IsValidUri_AmbiguousScheme_IsInvalid()
        {
            string uriString = "//localhost";

            bool result = this.validator.IsValidUri(uriString);

            Assert.IsFalse(result);
        }


        [TestMethod]
        public void UriValidator_IsValidUri_UriWithPort_IsValid()
        {
            // Setup
            string uriString = CreateUriString(this.secureScheme, "localhost:9001");

            // Test
            bool result = this.validator.IsValidUri(uriString);

            // Verify
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UriValidator_IsValidUri_LongUriWithFragments_IsValid()
        {
            // Setup
            string uriString = CreateUriString(this.secureScheme, "localhost:9001/this/is/a/longer/uri/that/should/be?valid=true");

            // Test
            bool result = this.validator.IsValidUri(uriString);

            // Verify
            Assert.IsTrue(result);
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
            Assert.IsTrue(lowercaseInsecure, "Lowercase scheme should be insecure");
            Assert.IsTrue(uppercaseInsecure, "Uppercase scheme should be insecure");
            Assert.IsTrue(mixedcaseInsecure, "Mixed-case scheme should be insecure");
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
            Assert.IsTrue(lowercaseSupported, "Lowercase scheme should be supported");
            Assert.IsTrue(uppercaseSupported, "Uppercase scheme should be supported");
            Assert.IsTrue(mixedcaseSupported, "Mixed-case scheme should be supported");
        }

        #endregion
    }

}
