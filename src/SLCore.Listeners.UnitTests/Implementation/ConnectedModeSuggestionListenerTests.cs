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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Binding;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ConnectedModeSuggestionListenerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeSuggestionListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IBindingSuggestionHandler>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>());
    }

    [TestMethod]
    public void AssistCreatingConnectionAsync_Notifies()
    {
        var bindingSuggestionHandler = Substitute.For<IBindingSuggestionHandler>();
        var activeConfigScopeTracer = Substitute.For<IActiveConfigScopeTracker>();
        const string scopeId = "scope-id";
        activeConfigScopeTracer.Current.Returns(new ConfigurationScope(scopeId));

        var testSubject = new ConnectedModeSuggestionListener(bindingSuggestionHandler, activeConfigScopeTracer);
        var response = testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams("a-server-url", "a-token", "a-token-value"));

        bindingSuggestionHandler.Received().Notify();
        response.Result.Should().Be(new AssistCreatingConnectionResponse(scopeId));
    }

    [TestMethod]
    public void NoBindingSuggestionFound_Notifies()
    {
        var bindingSuggestionHandler = Substitute.For<IBindingSuggestionHandler>();
        var activeConfigScopeTracer = Substitute.For<IActiveConfigScopeTracker>();

        var testSubject = new ConnectedModeSuggestionListener(bindingSuggestionHandler, activeConfigScopeTracer);
        testSubject.NoBindingSuggestionFound(new NoBindingSuggestionFoundParams("a-project-key"));

        bindingSuggestionHandler.Received().Notify();
    }
}
