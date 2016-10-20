//-----------------------------------------------------------------------
// <copyright file="MinimumSupportedServerPlugin.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using SonarLint.VisualStudio.Integration.Resources;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    internal class MinimumSupportedServerPlugin
    {
        public static readonly MinimumSupportedServerPlugin CSharp = new MinimumSupportedServerPlugin("csharp", Language.CSharp, "5.0");
        public static readonly MinimumSupportedServerPlugin VbNet = new MinimumSupportedServerPlugin("vbnet", Language.VBNET, "3.0");

        private MinimumSupportedServerPlugin(string key, Language language, string minimumVersion)
        {
            Key = key;
            Language = language;
            MinimumVersion = minimumVersion;
        }

        public string Key { get; }
        public string MinimumVersion { get; }
        public Language Language { get; }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.MinimumSupportedServerPlugin, Language.Name, MinimumVersion);
        }

        public bool ISupported(EnvDTE.Project project)
        {
            return Language.ForProject(project).Equals(Language);
        }
    }
}
