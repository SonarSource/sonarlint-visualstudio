﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Binding;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation;

[TestClass]
public class ConnectedModeSuggestionListenerTests
{
    private IBindingSuggestionHandler bindingSuggestionHandler;
    private IActiveConfigScopeTracker activeConfigScopeTracer;
    private ConnectedModeSuggestionListener testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        bindingSuggestionHandler = Substitute.For<IBindingSuggestionHandler>();
        activeConfigScopeTracer = Substitute.For<IActiveConfigScopeTracker>();
        testSubject = new ConnectedModeSuggestionListener(bindingSuggestionHandler, activeConfigScopeTracer);
    }

    [TestMethod]
    public void MefCtor_CheckExports() =>
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeSuggestionListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<IBindingSuggestionHandler>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>());

    [TestMethod]
    public void AssistCreatingConnectionAsync_Notifies()
    {
        const string scopeId = "scope-id";
        activeConfigScopeTracer.Current.Returns(new ConfigurationScope(scopeId));

        var response = testSubject.AssistCreatingConnectionAsync(new AssistCreatingConnectionParams()
        {
            connectionParams = new SonarQubeConnectionParams(new Uri("http://localhost:9000"), "a-token", "a-token-value")
        });

        bindingSuggestionHandler.Received().Notify();
        response.Result.Should().Be(new AssistCreatingConnectionResponse(scopeId));
    }

    [TestMethod]
    public void AssistBindingAsync_NotImplemented()
    {
        Action act = () => testSubject.AssistBindingAsync(new AssistBindingParams("A_CONNECTION_ID", "A_PROJECT_KEY", "A_CONFIG_SCOPE_ID", false));

        act.Should().Throw<NotImplementedException>();
    }

    [TestMethod]
    public void NoBindingSuggestionFound_Notifies()
    {
        testSubject.NoBindingSuggestionFound(new NoBindingSuggestionFoundParams("a-project-key"));

        bindingSuggestionHandler.Received().Notify();
    }
}
