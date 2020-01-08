/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Integration.Connection
{
    internal class UriValidator
    {
        private static readonly ISet<string> DefaultSupportedSchemes = new HashSet<string>(new[] { "http", "https" }, StringComparer.OrdinalIgnoreCase);

        private static readonly ISet<string> DefaultInsecureSchemes = new HashSet<string>(new[] { "http" }, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Supported URI schemes.
        /// </summary>
        private readonly ISet<string> supportedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Schemes which are considered to be insecure.
        /// </summary>
        private readonly ISet<string> insecureSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public UriValidator()
        {
            this.supportedSchemes.UnionWith(DefaultSupportedSchemes);
            this.insecureSchemes.UnionWith(DefaultInsecureSchemes);
        }

        public UriValidator(ISet<string> supportedSchemes)
        {
            if (supportedSchemes == null)
            {
                throw new ArgumentNullException(nameof(supportedSchemes));
            }

            this.supportedSchemes.Clear();
            this.insecureSchemes.Clear();

            this.supportedSchemes.UnionWith(supportedSchemes);
        }

        public UriValidator(ISet<string> supportedSchemes, ISet<string> insecureSchemes)
            : this(supportedSchemes)
        {
            if (insecureSchemes == null)
            {
                throw new ArgumentNullException(nameof(supportedSchemes));
            }

            this.supportedSchemes.Clear();
            this.insecureSchemes.Clear();

            this.supportedSchemes.UnionWith(supportedSchemes);
            this.insecureSchemes.UnionWith(insecureSchemes);

            if (!this.insecureSchemes.IsSubsetOf(this.supportedSchemes))
            {
                throw new ArgumentException(Resources.Strings.ExceptionInsecureSchemesIsNotSubset, nameof(insecureSchemes));
            }
        }

        /// <summary>
        /// True if <paramref name="uri"/>'s scheme is one of <see cref="supportedSchemes"/>, false otherwise.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <param name="uri"><see cref="Uri"/> to check, must not be null.</param>
        public bool IsSupportedScheme(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return this.supportedSchemes?.Contains(uri.Scheme) ?? false;
        }

        /// <summary>
        /// True if <paramref name="uri"/>'s scheme is one of <see cref="insecureSchemes"/>, false otherwise.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <param name="uri"><see cref="Uri"/> to check, must not be null.</param>
        public bool IsInsecureScheme(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return this.insecureSchemes?.Contains(uri.Scheme) ?? false;
        }

        /// <summary>
        /// Whether or not <paramref name="uriString"/> is considered to be a valid URI.
        /// </summary>
        /// <param name="uriString"><see cref="string"/> URI to check</param>
        /// <remarks>
        /// Valid URIs cannot be null, must have a scheme listed in <see cref="supportedSchemes"/>,
        /// and be absolute.
        /// </remarks>
        public virtual bool IsValidUri(string uriString)
        {
            // non empty
            if (string.IsNullOrWhiteSpace(uriString))
            {
                return false;
            }

            Uri uri;

            // creatable
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out uri))
            {
                return false;
            }

            return this.IsValidUri(uri);
        }

        /// <summary>
        /// Whether or not <paramref name="uri"/> is considered to be a valid URI.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <param name="uri"><see cref="Uri"/> to check, must not be null.</param>
        /// <remarks>
        /// Valid URIs must have a scheme listed in <see cref="supportedSchemes"/>
        /// and be absolute.
        /// </remarks>
        public virtual bool IsValidUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            // absolute
            if (!uri.IsAbsoluteUri)
            {
                return false;
            }

            // supported
            if (!this.IsSupportedScheme(uri))
            {
                return false;
            }

            return true;
        }
    }
}
