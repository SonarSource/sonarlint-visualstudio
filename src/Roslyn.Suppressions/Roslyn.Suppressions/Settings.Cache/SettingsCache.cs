using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Roslyn.Suppression.SettingsFile;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache
{
    internal class SettingsCache : ISettingsCache
    {
        private readonly ISuppressedIssuesFileStorage fileStorage;
        private readonly ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> settingsCollection;


        public SettingsCache(ILogger logger) : this(new SuppressedIssuesFileStorage(logger), new SettingsSingleton())
        {

        }

        internal SettingsCache(ISuppressedIssuesFileStorage fileStorage, ISettingsSingleton singleton)
        {
            this.fileStorage = fileStorage;
            this.settingsCollection = singleton.Instance;
        }


        public IEnumerable<SonarQubeIssue> GetSettings(string projectKey)
        {
            if(!settingsCollection.ContainsKey(projectKey))
            {
                var settings = fileStorage.Get(projectKey);
                settingsCollection.AddOrUpdate(projectKey, settings, (x,y) => settings);
            }
            return settingsCollection[projectKey];
        }

        public void Invalidate(string projectKey)
        {
            settingsCollection.TryRemove(projectKey,out _);
        }
    }
}
