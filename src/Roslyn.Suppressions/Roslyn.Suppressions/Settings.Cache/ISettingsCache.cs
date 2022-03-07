using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache
{
    public interface ISettingsCache
    {
        IEnumerable<SonarQubeIssue> GetSettings(string projectKey);
        void Invalidate(string projectKey);
    }


}
