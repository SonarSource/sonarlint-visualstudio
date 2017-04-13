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
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Alm.Authentication
{
    public abstract class Secret
    {
        public static string UriToName(Uri targetUri, string @namespace)
        {
            const string TokenNameBaseFormat = "{0}:{1}://{2}";
            const string TokenNamePortFormat = TokenNameBaseFormat + ":{3}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("Secret::UriToName");

            // trim any trailing slashes and/or whitespace for compatibility with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();

            var targetName = targetUri.IsDefaultPort
                ? string.Format(CultureInfo.InvariantCulture, TokenNameBaseFormat, @namespace, targetUri.Scheme, trimmedHostUrl)
                : string.Format(CultureInfo.InvariantCulture, TokenNamePortFormat, @namespace, targetUri.Scheme, trimmedHostUrl, targetUri.Port);

            Trace.WriteLine("   target name = " + targetName);

            return targetName;
        }

        public delegate string UriNameConversion(Uri targetUri, string @namespace);
    }
}

