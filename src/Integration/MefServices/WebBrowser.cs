/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IWebBrowser)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class WebBrowser : IWebBrowser
    {
        public void NavigateTo(string url)
        {
            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri) && IsSafeScheme(uri.Scheme))
            {
                var startInfo = new ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            else
            {
                Debug.Fail("Provided URL was not in a correct format");
            }
        }

        private static bool IsSafeScheme(string scheme)
        {
            return scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase);
        }
    }
}
