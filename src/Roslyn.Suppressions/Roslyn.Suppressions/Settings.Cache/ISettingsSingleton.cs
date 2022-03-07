using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache
{
    public interface ISettingsSingleton
    {
        ConcurrentDictionary<string, IEnumerable<SonarQubeIssue>> Instance { get; }
    } 
}
