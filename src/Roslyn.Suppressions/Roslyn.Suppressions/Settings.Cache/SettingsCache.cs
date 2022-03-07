/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Collections.Concurrent;
using System.Collections.Generic;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache
{
    internal class SettingsCache : ISettingsCache
    {
        private readonly ISuppressedIssuesFileStorage fileStorage;
        private readonly ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> settingsCollection;


        public SettingsCache(ILogger logger) : this(new SuppressedIssuesFileStorage(logger), new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>())
        {

        }

        internal SettingsCache(ISuppressedIssuesFileStorage fileStorage, ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> settingsCollection)
        {
            this.fileStorage = fileStorage;
            this.settingsCollection = settingsCollection;
        }


        public IEnumerable<SonarQubeIssue> GetSettings(string settingsKey)
        {
            if(!settingsCollection.ContainsKey(settingsKey))
            {
                var settings = fileStorage.Get(settingsKey);
                settingsCollection.AddOrUpdate(settingsKey, settings, (x,y) => settings);
            }
            return settingsCollection[settingsKey];
        }

        public void Invalidate(string settingsKey)
        {
            settingsCollection.TryRemove(settingsKey, out _);
        }
    }
}
