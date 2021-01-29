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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    /// <summary>
    /// Testable wrapper for <see cref="VsShellUtilities.OpenBrowser(string)"/>
    /// </summary>
    public interface IBrowserService
    {
        void Navigate(string url);
    }

    [Export(typeof(IBrowserService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BrowserService : IBrowserService
    {
        public void Navigate(string url)
        {
            VsShellUtilities.OpenBrowser(url);
        }
    }
}
