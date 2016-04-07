//-----------------------------------------------------------------------
// <copyright file="UriExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration
{
    internal static class UriExtensions
    {
        public static Uri EnsureTrailingSlash(this Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var uriString = uri.ToString();

            if (!uriString.EndsWith("/"))
            {
                return new Uri(uriString + "/");
            }

            return uri;
        }
    }
}
