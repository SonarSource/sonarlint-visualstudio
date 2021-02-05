/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Infrastructure.VS
{
    public interface IVsVersionProvider
    {
        IVsVersion Version { get; }
    }

    [Export(typeof(IVsVersionProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class VsVersionProvider : IVsVersionProvider
    {
        public IVsVersion Version { get; }

        [ImportingConstructor]
        public VsVersionProvider([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            var vsShell = serviceProvider.GetService(typeof(SVsShell)) as IVsShell;

            vsShell.GetProperty((int)__VSSPROPID5.VSSPROPID_AppBrandName, out var name);
            vsShell.GetProperty((int)__VSSPROPID5.VSSPROPID_AppShortBrandName, out var shortName);
            vsShell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var version);

            Version = new VsVersion((string)name, (string)shortName, (string)version);
        }
    }

    public interface IVsVersion
    {
        /// <summary>
        /// See <see cref="__VSSPROPID5.VSSPROPID_AppBrandName"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// See <see cref="__VSSPROPID5.VSSPROPID_AppShortBrandName"/>
        /// </summary>
        string ShortName { get; }

        /// <summary>
        /// See <see cref="__VSSPROPID5.VSSPROPID_ReleaseVersion"/>
        /// </summary>
        string BuildVersion { get; }
    }

    internal class VsVersion : IVsVersion
    {
        public string Name { get; }
        public string ShortName { get; }
        public string BuildVersion { get; }

        public VsVersion(string name, string shortName, string buildVersion)
        {
            Name = name;
            ShortName = shortName;
            BuildVersion = buildVersion;
        }
    }
}
