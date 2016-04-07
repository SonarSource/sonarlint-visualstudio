//-----------------------------------------------------------------------
// <copyright file="UriExtensionsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class UriExtensionsTests
    {
        [TestMethod]
        public void UriExtensions_NullArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => UriExtensions.EnsureTrailingSlash(null));
        }

        [TestMethod]
        public void UriExtensions_EnsureTrailingSlash_NoTrailingSlash_AppendsSlash()
        {
            // Act
            var noSlashResult = UriExtensions.EnsureTrailingSlash(new Uri("http://localhost/NoSlash"));

            // Verify
            Assert.AreEqual("http://localhost/NoSlash/", noSlashResult.ToString(), "Unexpected normalisation of URI without trailing slash");
        }

        [TestMethod]
        public void UriExtensions_EnsureTrailingSlash_HasTrailingSlash_ReturnsSameInstance()
        {
            // Act
            var withSlashResult = UriExtensions.EnsureTrailingSlash(new Uri("http://localhost/WithSlash/"));

            // Verify
            Assert.AreEqual("http://localhost/WithSlash/", withSlashResult.ToString(), "Unexpected normalisation of URI already with trailing slash");
        }
    }
}
