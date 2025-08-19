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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.EmbeddedAnalyzers;

[Export(typeof(IEmbeddedDotnetAnalyzersLocator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class EmbeddedDotnetAnalyzersLocator(IVsixRootLocator vsixRootLocator, ILanguageProvider languageProvider, IFileSystemService fileSystem) : IEmbeddedDotnetAnalyzersLocator
{
    private const string PathInsideVsix = "EmbeddedDotnetAnalyzerDLLs";
    private const string DllsSearchPattern = "SonarAnalyzer.*.dll"; // starting from 10.0, the analyzer assemblies are merged and all of the dll names start with SonarAnalyzer
    private const string EnterpriseInfix = ".Enterprise."; // enterprise analyzer assemblies are included in the same folder and need to be filtered out

    private readonly IFileSystem fileSystem = fileSystem;

    public List<string> GetBasicAnalyzerFullPaths() => GetBasicAnalyzerDlls().ToList();

    public Dictionary<Language, List<string>> GetBasicAnalyzerFullPathsByLanguage() => GroupByLanguage(GetBasicAnalyzerDlls());

    private IEnumerable<string> GetBasicAnalyzerDlls() => GetAllAnalyzerDlls().Where(x => !x.Contains(EnterpriseInfix));

    public List<string> GetEnterpriseAnalyzerFullPaths() => GetAllAnalyzerDlls().ToList();

    public Dictionary<Language, List<string>> GetEnterpriseAnalyzerFullPathsByLanguage() => GroupByLanguage(GetAllAnalyzerDlls());

    private string[] GetAllAnalyzerDlls() => fileSystem.Directory.GetFiles(GetPathToParentFolder(), DllsSearchPattern);

    private string GetPathToParentFolder() => Path.Combine(vsixRootLocator.GetVsixRoot(), PathInsideVsix);


    private Dictionary<Language, List<string>> GroupByLanguage(IEnumerable<string> analyzerDlls)
    {
        var result = languageProvider.RoslynLanguages.ToDictionary(x => x, _ => new List<string>());

        foreach (var analyzerDll in analyzerDlls)
        {
            var dllLanguage = result.Keys.FirstOrDefault(x => analyzerDll.Contains(x.RoslynDllIdentifier));
            if (dllLanguage != null)
            {
                result[dllLanguage].Add(analyzerDll);
            }
        }

        return result;
    }
}
