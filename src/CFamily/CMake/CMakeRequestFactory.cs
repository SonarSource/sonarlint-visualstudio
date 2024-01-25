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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.CFamily.CompilationDatabase;
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.CFamily.SubProcess;

namespace SonarLint.VisualStudio.CFamily.CMake
{
    [Export(typeof(IRequestFactory))]
    internal class CMakeRequestFactory : IRequestFactory
    {
        private readonly ICompilationConfigProvider compilationConfigProvider;
        private readonly ICFamilyRulesConfigProvider rulesConfigProvider;
        private readonly IEnvironmentVarsProvider envVarsProvider;

        [ImportingConstructor]
        public CMakeRequestFactory(ICompilationConfigProvider compilationConfigProvider,
            ICFamilyRulesConfigProvider rulesConfigProvider,
            IEnvironmentVarsProvider envVarsProvider)
        {
            this.compilationConfigProvider = compilationConfigProvider;
            this.rulesConfigProvider = rulesConfigProvider;
            this.envVarsProvider = envVarsProvider;
        }

        public async Task<IRequest> TryCreateAsync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions)
        {
            var dbEntry = compilationConfigProvider.GetConfig(analyzedFilePath);
            if (dbEntry == null)
            {
                return null;
            }

            // TODO - handle user specifying the language via a command / argument #2533
            var languageKey = CFamilyShared.FindLanguageFromExtension(dbEntry.File);
            if (languageKey == null)
            {
                return null;
            }

            var rulesConfig = rulesConfigProvider.GetRulesConfiguration(languageKey);
            var context = new RequestContext(
                languageKey, 
                rulesConfig, 
                analyzedFilePath, 
                SubProcessFilePaths.PchFilePath, 
                analyzerOptions, 
                CFamilyShared.IsHeaderFileExtension(analyzedFilePath));

            var envVars = await envVarsProvider.GetAsync();
            if (envVars == null)
            {
                return null;
            }

            return new CompilationDatabaseRequest(dbEntry, context, envVars);
        }
    }
}
