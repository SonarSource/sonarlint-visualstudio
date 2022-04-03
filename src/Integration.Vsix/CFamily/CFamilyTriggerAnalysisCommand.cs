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

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{
    public sealed class CFamilyTriggerAnalysisCommand : VsCommandBase
    {
        private readonly IActiveDocumentLocator activeDocumentLocator;
        private readonly ISonarLanguageRecognizer sonarLanguageRecognizer;
        private readonly IAnalysisRequester analysisRequester;

        public CFamilyTriggerAnalysisCommand(IActiveDocumentLocator activeDocumentLocator, ISonarLanguageRecognizer sonarLanguageRecognizer, IAnalysisRequester analysisRequester)
        {
            this.activeDocumentLocator = activeDocumentLocator;
            this.sonarLanguageRecognizer = sonarLanguageRecognizer;
            this.analysisRequester = analysisRequester;
        }

        protected override void InvokeInternal()
        {
            analysisRequester.RequestAnalysis(new AnalyzerTriggerOption { AnalysisTrigger = AnalysisTrigger.Action }, activeDocumentLocator.FindActiveDocument().FilePath);
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            var hasActiveCFamilyDoc = HasActiveCFamilyDoc();
            command.Visible = hasActiveCFamilyDoc;
            command.Enabled = hasActiveCFamilyDoc;
            command.Supported = hasActiveCFamilyDoc;
        }

        private bool HasActiveCFamilyDoc()
        {
            var activeDoc = activeDocumentLocator.FindActiveDocument();
            if (activeDoc == null)
            {
                return false;
            }

            var languages = sonarLanguageRecognizer.Detect(activeDoc.FilePath, activeDoc.TextBuffer.ContentType);
            return languages.Contains(AnalysisLanguage.CFamily);
        }
    }
}
