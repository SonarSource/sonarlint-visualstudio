/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Represents a programming language for which connected mode is supported. Implements <seealso cref="IEquatable{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <seealso cref="Language"/>s are equal if they have the same <see cref="Id"/> and <see cref="ProjectType"/>.
    /// </para>
    /// <para>
    /// This class is safe for use as a key in collection classes. E.g., <seealso cref="IDictionary{TKey, TValue}"/>.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("{Name} (ID: {Id})")]
    [TypeConverter(typeof(LanguageConverter))]
    public sealed class Language : IEquatable<Language>
    {
        private const string VersionNumberPattern = "(\\d+\\.\\d+\\.\\d+\\.\\d+)\\";
        private static readonly PluginInfo SqvsRoslynPlugin = new("sqvsroslyn", $"sonarqube-ide-visualstudio-roslyn-plugin-{VersionNumberPattern}.jar");
        private static readonly PluginInfo CSharpPlugin = new("csharpenterprise", $"sonar-csharp-enterprise-plugin-{VersionNumberPattern}.jar", isEnabledForAnalysis: false);
        private static readonly PluginInfo VbNetPlugin = new("vbnetenterprise", $"sonar-vbnet-enterprise-plugin-{VersionNumberPattern}.jar", isEnabledForAnalysis: false);
        private static readonly PluginInfo SecretsPlugin = new("text", $"sonar-text-plugin-{VersionNumberPattern}.jar");
        private static readonly PluginInfo CFamilyPlugin = new("cpp", $"sonar-cfamily-plugin-{VersionNumberPattern}.jar");
        private static readonly PluginInfo JavascriptPlugin = new("javascript", $"sonar-javascript-plugin-{VersionNumberPattern}.jar");
        private static readonly PluginInfo HtmlPlugin = new("web", $"sonar-html-plugin-{VersionNumberPattern}.jar");
        private static readonly PluginInfo TsqlPlugin = new("tsql", null);

        private static readonly RepoInfo CSharpRepo = new("csharpsquid", "csharp");
        private static readonly RepoInfo CSharpSecurityRepo = new("roslyn.sonaranalyzer.security.cs", "csharp");
        private static readonly RepoInfo VbNetRepo = new("vbnet");
        private static readonly RepoInfo CppRepo = new("cpp");
        private static readonly RepoInfo CRepo = new("c");
        private static readonly RepoInfo JsRepo = new("javascript");
        private static readonly RepoInfo JsSecurityRepo = new("jssecurity", "javascript");
        private static readonly RepoInfo TsRepo = new("typescript");
        private static readonly RepoInfo TsSecurityRepo = new("tssecurity", "typescript");
        private static readonly RepoInfo CssRepo = new("css");
        private static readonly RepoInfo HtmlRepo = new("Web", "html"); //See https://github.com/SonarSource/sonarlint-visualstudio/issues/4586.
        private static readonly RepoInfo SecretsRepo = new("secrets");
        private static readonly RepoInfo TextRepo = new("text");
        private static readonly RepoInfo TsqlRepo = new("tsql");

        public static readonly Language Unknown = new();
        public static readonly Language CSharp = new("CSharp", CoreStrings.CSharpLanguageName, "cs", SqvsRoslynPlugin, CSharpRepo, CSharpSecurityRepo,
            settingsFileName: "sonarlint_csharp.globalconfig", additionalPlugins: [CSharpPlugin]);
        public static readonly Language VBNET = new("VB", CoreStrings.VBNetLanguageName, "vbnet", SqvsRoslynPlugin, VbNetRepo, settingsFileName: "sonarlint_vb.globalconfig",
            additionalPlugins: [VbNetPlugin]);
        public static readonly Language Cpp = new("C++", CoreStrings.CppLanguageName, "cpp", CFamilyPlugin, CppRepo);
        public static readonly Language C = new("C", "C", "c", CFamilyPlugin, CRepo);
        public static readonly Language Js = new("Js", "JavaScript", "js", JavascriptPlugin, JsRepo, JsSecurityRepo);
        public static readonly Language Ts = new("Ts", "TypeScript", "ts", JavascriptPlugin, TsRepo, TsSecurityRepo);
        public static readonly Language Css = new("Css", "CSS", "css", JavascriptPlugin, CssRepo);
        public static readonly Language Html = new("Html", "HTML", "web", HtmlPlugin, HtmlRepo);
        public static readonly Language Secrets = new("Secrets", "Secrets", "secrets", SecretsPlugin, SecretsRepo);
        public static readonly Language Text = new("Text", "Text", "text", SecretsPlugin, TextRepo);
        public static readonly Language TSql = new("TSql", "T-SQL", "tsql", TsqlPlugin, TsqlRepo);

        /// <summary>
        /// A stable identifier for this language.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Object containing the server-side description of the language as used by SonarQube/Cloud
        /// </summary>
        /// <remarks>The server-side language key is the stable id used server-side to identify a language. Ideally this would have been used as the
        /// Id here originally. However, it wasn't and we can't easily change the Id values now since they are serialized in the
        /// solution-level binding file.</remarks>
        public string ServerLanguageKey { get; }

        public PluginInfo PluginInfo { get; }

        /// <summary>
        /// The language display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Suffix and extension added to the language-specific rules configuration file for the language
        /// </summary>
        /// <remarks>e.g. for ruleset-based languages this will be a language identifier + ".globalconfig"</remarks>
        public string SettingsFileNameAndExtension { get; }

        /// <summary>
        /// Additional plugins that should be installed for a language
        /// </summary>
        public PluginInfo[] AdditionalPlugins { get; }

        public RepoInfo RepoInfo { get; }

        /// <summary>
        /// Nullable, the repository info for the security rules (i.e. hotspots) for this language
        /// </summary>
        public RepoInfo SecurityRepoInfo { get; }

        /// <summary>
        /// Private constructor reserved for the <seealso cref="Unknown"/>.
        /// </summary>
        private Language()
        {
            Id = string.Empty;
            Name = CoreStrings.UnknownLanguageName;
            SettingsFileNameAndExtension = string.Empty;
        }

        public Language(
            string id,
            string name,
            string serverLanguageKey,
            PluginInfo pluginInfo,
            RepoInfo repoInfo,
            RepoInfo securityRepoInfo = null,
            string settingsFileName = null,
            PluginInfo[] additionalPlugins = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Id = id;
            Name = name;
            SettingsFileNameAndExtension = settingsFileName;
            AdditionalPlugins = additionalPlugins;
            ServerLanguageKey = serverLanguageKey ?? throw new ArgumentNullException(nameof(serverLanguageKey));
            PluginInfo = pluginInfo ?? throw new ArgumentNullException(nameof(pluginInfo));
            RepoInfo = repoInfo ?? throw new ArgumentNullException(nameof(repoInfo));
            SecurityRepoInfo = securityRepoInfo;
        }

        #region IEquatable<Language> and Equals

        public bool Equals(Language other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other != null
                   && other.Id == Id;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Language);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        public override string ToString() => Name;

        public bool HasRepoKey(string repoKey) => RepoInfo.Key == repoKey || SecurityRepoInfo?.Key == repoKey;
    }
}
