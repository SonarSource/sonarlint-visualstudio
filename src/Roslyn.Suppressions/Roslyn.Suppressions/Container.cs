/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.IO;
using System.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions
{
    internal interface IContainer : IDisposable
    {
        ILogger Logger { get; }

        ISuppressionChecker SuppressionChecker { get; }
    }

    internal sealed class Container : IContainer
    {
        /// <summary>
        /// We do not provide ValueFactory in the initialization so that exceptions would not be cached.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.threading.lazythreadsafetymode?view=net-6.0
        /// </summary>
        private static readonly Lazy<Container> _instance = new Lazy<Container>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static IContainer Instance
        {
            get
            {
                try
                {
                    return _instance.Value;
                }
                catch
                {
                    return null;
                }
            }
        }

        private readonly ISuppressedIssuesFileWatcher fileWatcher;

        public ILogger Logger { get; }

        public ISuppressionChecker SuppressionChecker { get; }

        public Container()
        {
            Directory.CreateDirectory(RoslynSettingsFileInfo.Directory);
            Logger = new Logger();

            var settingsCache = new SettingsCache(Logger);
            fileWatcher = new SuppressedIssuesFileWatcher(settingsCache, Logger);

            SuppressionChecker = new SuppressionChecker(settingsCache);
        }

        public void Dispose()
        {
            fileWatcher?.Dispose();
        }
    }
}
