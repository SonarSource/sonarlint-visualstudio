using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache
{
    internal class SettingsSingleton : ISettingsSingleton
    {
        private static readonly Lazy<ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>> lazyInstance = new Lazy<ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>>(() => new ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>>());
        public ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> Instance => lazyInstance.Value;
    }
}
