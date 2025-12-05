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

using System.ComponentModel.Composition.Primitives;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.NewCode;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.NewCode;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.NewCode;

[TestClass]
public class FocusOnNewCodeServiceTests
{
    private const string DefaultConfigScopeId = "my config scope";
    private ISonarLintSettings sonarLintSettings;
    private NoOpThreadHandler threadHandling;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private NoOpThreadHandler threadHandler;
    private ISLCoreServiceProvider serviceProvider;
    private INewCodeSLCoreService newCodeSlCoreService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private TestLogger logger;
    private FocusOnNewCodeService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        CreateMocks();
        SetUpDefaultMocks();
        CreateAndInitializeTestSubject();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        Export[] exports =
        [
            MefTestHelpers.CreateExport<ISonarLintSettings>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>()
        ];

        MefTestHelpers.CheckTypeCanBeImported<FocusOnNewCodeService, IFocusOnNewCodeService>(exports);
        MefTestHelpers.CheckTypeCanBeImported<FocusOnNewCodeService, IFocusOnNewCodeServiceUpdater>(exports);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FocusOnNewCodeService>();

    [DataTestMethod]
    [DataRow(true, false, "not supported")]
    [DataRow(true, true, "from this version")]
    [DataRow(false, false, "not supported")]
    [DataRow(false, true, "from this version")]
    public void Ctor_InitializesCorrectly(bool isEnabled, bool isSupported, string description)
    {
        CreateMocks();
        SetUpDefaultMocks();
        SetUpNewCodePreference(isEnabled);
        SetUpNewCodeDefinition(DefaultConfigScopeId, isSupported, description);

        CreateAndInitializeTestSubject();

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, isSupported, description),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        _ = sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled;
        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<FocusOnNewCodeService>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync(); // from ctor
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            _ = sonarLintSettings.IsFocusOnNewCodeEnabled; // this doesn't actually assert anything due to how NSub works, but is left here to make the test easier to understand
            serviceProvider.TryGetTransientService(out Arg.Any<INewCodeSLCoreService>());
            newCodeSlCoreService.GetNewCodeDefinitionAsync(Arg.Any<GetNewCodeDefinitionParams>());
            testSubject.InitializationProcessor.InitializeAsync(); // from CreateTestSubject
        });
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_ConfigurationScopeNotSet_InitializesCorrectly(bool isEnabled)
    {
        CreateMocks();
        SetUpDefaultMocks();
        SetUpNewCodePreference(isEnabled);
        SetUpConfigurationScope(null);

        CreateAndInitializeTestSubject();

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        _ = sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled;
        newCodeSlCoreService.DidNotReceiveWithAnyArgs().GetNewCodeDefinitionAsync(default);
        logger.AssertPartialOutputStringExists(string.Format(Resources.FocusOnNewCodeDefinitionUnavailableLogTemplate, SLCoreStrings.ConfigScopeNotInitialized));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_ServiceProviderNotInitialized_InitializesCorrectly(bool isEnabled)
    {
        CreateMocks();
        SetUpDefaultMocks();
        SetUpNewCodePreference(isEnabled);
        SetUpServiceProviderWithNewCodeService(false);

        CreateAndInitializeTestSubject();

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        _ = sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled;
        newCodeSlCoreService.DidNotReceiveWithAnyArgs().GetNewCodeDefinitionAsync(default);
        logger.AssertPartialOutputStringExists(string.Format(Resources.FocusOnNewCodeDefinitionUnavailableLogTemplate, SLCoreStrings.ServiceProviderNotInitialized));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_NewCodeDefinitionServiceThrows_InitializesCorrectly(bool isEnabled)
    {
        CreateMocks();
        SetUpDefaultMocks();
        SetUpNewCodePreference(isEnabled);
        const string testException = "test exception";
        newCodeSlCoreService.GetNewCodeDefinitionAsync(default).ThrowsAsyncForAnyArgs(new Exception(testException));

        CreateAndInitializeTestSubject();

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        _ = sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled;
        logger.AssertPartialOutputStringExists("Focus on New Code definition not available:");
        logger.AssertPartialOutputStringExists(testException);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetPreference_UpdatesSettingAndRaisesEvent(bool isEnabled)
    {
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        testSubject.SetPreference(isEnabled);

        sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled = isEnabled;
        testSubject.Current.IsEnabled.Should().Be(isEnabled);
        handler.Received(1).Invoke(testSubject, Arg.Is<NewCodeStatusChangedEventArgs>(e => e.NewStatus.IsEnabled == isEnabled));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetPreference_NotifiesSlCore(bool isSlCoreInitialized)
    {
        serviceProvider.ClearReceivedCalls();
        SetUpServiceProviderWithNewCodeService(isSlCoreInitialized);

        testSubject.SetPreference(true);

        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            serviceProvider.TryGetTransientService(out Arg.Any<INewCodeSLCoreService>());
            if (isSlCoreInitialized)
            {
                newCodeSlCoreService.DidToggleFocus();
            }
        });
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConfigurationScopeChanged_RefreshesStatusAndNotifies(bool configScopeUpdateType)
    {
        SetUpNewCodeDefinition(DefaultConfigScopeId, true, "new status");
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        var args = new ConfigurationScopeChangedEventArgs(configScopeUpdateType);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        handler.Received(1).Invoke(testSubject, Arg.Any<NewCodeStatusChangedEventArgs>());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConfigurationScopeChanged_ConfigurationScopeNotSet_InitializesCorrectly(bool isEnabled)
    {
        newCodeSlCoreService.ClearReceivedCalls();
        SetUpNewCodePreference(isEnabled);
        SetUpConfigurationScope(null);
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        var args = new ConfigurationScopeChangedEventArgs(definitionChanged: true);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        newCodeSlCoreService.DidNotReceiveWithAnyArgs().GetNewCodeDefinitionAsync(default);
        logger.AssertPartialOutputStringExists(string.Format(Resources.FocusOnNewCodeDefinitionUnavailableLogTemplate, SLCoreStrings.ConfigScopeNotInitialized));
        handler.Received(1).Invoke(testSubject, Arg.Any<NewCodeStatusChangedEventArgs>());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConfigurationScopeChanged_ServiceProviderNotInitialized_InitializesCorrectly(bool isEnabled)
    {
        newCodeSlCoreService.ClearReceivedCalls();
        SetUpNewCodePreference(isEnabled);
        SetUpServiceProviderWithNewCodeService(false);
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        var args = new ConfigurationScopeChangedEventArgs(definitionChanged: true);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        newCodeSlCoreService.DidNotReceiveWithAnyArgs().GetNewCodeDefinitionAsync(default);
        logger.AssertPartialOutputStringExists(string.Format(Resources.FocusOnNewCodeDefinitionUnavailableLogTemplate, SLCoreStrings.ServiceProviderNotInitialized));
        handler.Received(1).Invoke(testSubject, Arg.Any<NewCodeStatusChangedEventArgs>());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ConfigurationScopeChanged_NewCodeDefinitionServiceThrows_InitializesCorrectly(bool isEnabled)
    {
        newCodeSlCoreService.ClearReceivedCalls();
        SetUpNewCodePreference(isEnabled);
        const string testException = "test exception from event";
        newCodeSlCoreService.GetNewCodeDefinitionAsync(default).ThrowsAsyncForAnyArgs(new Exception(testException));
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        var args = new ConfigurationScopeChangedEventArgs(definitionChanged: true);
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith(activeConfigScopeTracker, args);

        testSubject.Current.Should().BeEquivalentTo(new FocusOnNewCodeStatus(isEnabled, true, Resources.FocusOnNewCodeNotAvailableDescription),
            options => options.ComparingByMembers<FocusOnNewCodeStatus>());
        logger.AssertPartialOutputStringExists("Focus on New Code definition not available:");
        logger.AssertPartialOutputStringExists(testException);
        handler.Received(1).Invoke(testSubject, Arg.Any<NewCodeStatusChangedEventArgs>());
    }

    private void SetUpServiceProviderWithNewCodeService(bool isInitialized) =>
        serviceProvider.TryGetTransientService(out Arg.Any<INewCodeSLCoreService>()).Returns(info =>
        {
            info[0] = newCodeSlCoreService;
            return isInitialized;
        });


    private void CreateMocks()
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<FocusOnNewCodeService>(threadHandling, Substitute.ForPartsOf<TestLogger>());
        threadHandler = Substitute.ForPartsOf<NoOpThreadHandler>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        newCodeSlCoreService = Substitute.For<INewCodeSLCoreService>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        logger = Substitute.ForPartsOf<TestLogger>();
    }

    private void SetUpDefaultMocks()
    {
        SetUpConfigurationScope(DefaultConfigScopeId);
        SetUpServiceProviderWithNewCodeService(true);
        SetUpNewCodeDefinition(DefaultConfigScopeId);
        SetUpNewCodePreference();
    }

    private void SetUpConfigurationScope(string id)
    {
        if (id == null)
        {
            activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);
        }
        else
        {
            activeConfigScopeTracker.Current.Returns(new ConfigurationScope(id));
        }
    }

    private void SetUpNewCodePreference(bool isEnabled = false) => sonarLintSettings.IsFocusOnNewCodeEnabled.Returns(isEnabled);

    private void SetUpNewCodeDefinition(string configScopeId, bool isSupported = true, string description = "any") =>
        newCodeSlCoreService.GetNewCodeDefinitionAsync(Arg.Is<GetNewCodeDefinitionParams>(x => x.configScopeId == configScopeId))
            .Returns(new GetNewCodeDefinitionResponse(description, isSupported));

    private void CreateAndInitializeTestSubject()
    {
        testSubject = new FocusOnNewCodeService(sonarLintSettings, initializationProcessorFactory, serviceProvider, activeConfigScopeTracker, threadHandler, logger);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }
}
