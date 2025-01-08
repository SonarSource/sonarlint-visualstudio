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

using SonarLint.VisualStudio.IssueVisualization.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion;
using SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models;
using FileEditDto = SonarLint.VisualStudio.SLCore.Listener.FixSuggestion.Models.FileEditDto;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ShowFixSuggestionListenerTests
{
    private ShowFixSuggestionListener testSubject;
    private IFixSuggestionHandler fixSuggestionHandler;

    [TestInitialize]
    public void Initialize()
    {
        fixSuggestionHandler = Substitute.For<IFixSuggestionHandler>();
        testSubject = new ShowFixSuggestionListener(fixSuggestionHandler);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<ShowFixSuggestionListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IFixSuggestionHandler>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<ShowFixSuggestionListener>();
    }

    [TestMethod]
    public void ShowFixSuggestion_CallsHandler()
    {
        var listOfChanges = new List<ChangesDto>
        {
            new(new LineRangeDto(10, 10), "public void test()", "private void test()")
        };
        var fileEditDto = new FileEditDto(@"C:\Users\test\TestProject\AFile.cs", listOfChanges);
        var fixSuggestionDto = new FixSuggestionDto("SUGGESTION_ID", "AN EXPLANATION", fileEditDto);
        var parameters = new ShowFixSuggestionParams("CONFIG_SCOPE_ID", "S1234", fixSuggestionDto);

        testSubject.ShowFixSuggestion(parameters);

        fixSuggestionHandler.Received(1).ApplyFixSuggestion(parameters);
    }
}
