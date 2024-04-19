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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.OpenInIde;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.OpenInIDE;

[TestClass]
public class OpenInIdeConfigScopeValidatorTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<OpenInIdeConfigScopeValidator, IOpenInIdeConfigScopeValidator>(
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<OpenInIdeConfigScopeValidator>();
    }

    [TestMethod]
    public void TryGetConfigurationScopeRoot_EnsuresActiveConfigScopeTrackerIsAccessedOnBackgroundThread()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out _, out var threadHandling);

        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(null, out _);
        
        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            _ = configScopeTracker.Current;
        });
    }
    
    [TestMethod]
    public void TryGetConfigurationScopeRoot_CurrentConfigurationNull_ReturnsFalse()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out var logger, out _);
        configScopeTracker.Current.Returns((ConfigurationScope)null);
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot("some scope", out _).Should().BeFalse();
        
        logger.AssertPartialOutputStringExists("[Open in IDE] Configuration scope mismatch:");
    }
    
    [TestMethod]
    public void TryGetConfigurationScopeRoot_IdsMismatch_ReturnsFalse()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out var logger, out _);
        configScopeTracker.Current.Returns(new ConfigurationScope("scope", "connection", "project", "root"));
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot("some other scope", out _).Should().BeFalse();
        
        logger.AssertPartialOutputStringExists("[Open in IDE] Configuration scope mismatch:");
    }
    
    [TestMethod]
    public void TryGetConfigurationScopeRoot_ScopeNotBound_ReturnsFalse()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out var logger, out _);
        const string issueConfigurationScope = "scope";
        configScopeTracker.Current.Returns(new ConfigurationScope(issueConfigurationScope));
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out _).Should().BeFalse();
        
        logger.AssertPartialOutputStringExists(OpenInIdeResources.ApiHandler_ConfigurationScopeNotBound);
    }
    
    [TestMethod]
    public void TryGetConfigurationScopeRoot_RootNotSet_ReturnsFalse()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out var logger, out _);
        const string issueConfigurationScope = "scope";
        configScopeTracker.Current.Returns(new ConfigurationScope(issueConfigurationScope, "connection", "project"));
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out _).Should().BeFalse();
        
        logger.AssertPartialOutputStringExists(OpenInIdeResources.ApiHandler_ConfigurationScopeRootNotSet);
    }
    
    [TestMethod]
    public void TryGetConfigurationScopeRoot_ValidScope_ReturnsTrue()
    {
        var openInIdeConfigScopeValidator = CreateTestSubject(out var configScopeTracker, out var logger, out _);
        const string issueConfigurationScope = "scope";
        const string rootPath = "root";
        configScopeTracker.Current.Returns(new ConfigurationScope(issueConfigurationScope, "connection", "project", rootPath));
        
        openInIdeConfigScopeValidator.TryGetConfigurationScopeRoot(issueConfigurationScope, out var actualRoot).Should().BeTrue();

        actualRoot.Should().BeSameAs(rootPath);
        logger.AssertNoOutputMessages();
    }

    private OpenInIdeConfigScopeValidator CreateTestSubject(out IActiveConfigScopeTracker configScopeTracker, out TestLogger logger, out IThreadHandling threadHandling)
    {
        configScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger = new();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        return new(configScopeTracker, logger, threadHandling);
    }
}
