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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            // Assert
            noSlashResult.ToString().Should().Be("http://localhost/NoSlash/", "Unexpected normalization of URI without trailing slash");
        }

        [TestMethod]
        public void UriExtensions_EnsureTrailingSlash_HasTrailingSlash_ReturnsSameInstance()
        {
            // Act
            var withSlashResult = UriExtensions.EnsureTrailingSlash(new Uri("http://localhost/WithSlash/"));

            // Assert
            withSlashResult.ToString().Should().Be("http://localhost/WithSlash/", "Unexpected normalization of URI already with trailing slash");
        }
    }
}