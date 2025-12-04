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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.NewCode;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.NewCode;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.NewCode;

[TestClass]
public class FocusOnNewCodeServiceTests
{
    private ISonarLintSettings sonarLintSettings;
    private NoOpThreadHandler threadHandling;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private NoOpThreadHandler threadHandler;
    private ISLCoreServiceProvider serviceProvider;
    private INewCodeSLCoreService newCodeSlCoreService;
    private FocusOnNewCodeService testSubject;

    [TestInitialize]
    public void TestInitialize() => TestInitialize(false);

    private void TestInitialize(bool isEnabled)
    {
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        sonarLintSettings.IsFocusOnNewCodeEnabled.Returns(isEnabled);
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<FocusOnNewCodeService>(threadHandling, Substitute.ForPartsOf<TestLogger>());
        threadHandler = Substitute.ForPartsOf<NoOpThreadHandler>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        newCodeSlCoreService = Substitute.For<INewCodeSLCoreService>();
        SetUpSlCoreService(true);
        testSubject = new FocusOnNewCodeService(sonarLintSettings, initializationProcessorFactory, serviceProvider, threadHandler);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        Export[] exports =
        [
            MefTestHelpers.CreateExport<ISonarLintSettings>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
        ];

        MefTestHelpers.CheckTypeCanBeImported<FocusOnNewCodeService, IFocusOnNewCodeService>(exports);
        MefTestHelpers.CheckTypeCanBeImported<FocusOnNewCodeService, IFocusOnNewCodeServiceUpdater>(exports);
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<FocusOnNewCodeService>();

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Ctor_InitializesCorrectly(bool isEnabled)
    {
        TestInitialize(isEnabled);

        testSubject.Current.IsEnabled.Should().Be(isEnabled);
        _ = sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled;
        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<FocusOnNewCodeService>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync(); // from ctor
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            _ = sonarLintSettings.IsFocusOnNewCodeEnabled; // this doesn't actually assert anything due to how NSub works, but is left here to make the test easier to understand
            testSubject.InitializationProcessor.InitializeAsync(); // from CreateTestSubject
        });
        serviceProvider.DidNotReceiveWithAnyArgs().TryGetTransientService(out Arg.Any<INewCodeSLCoreService>());
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Set_UpdatesSettingAndRaisesEvent(bool isEnabled)
    {
        var handler = Substitute.For<EventHandler<NewCodeStatusChangedEventArgs>>();
        testSubject.Changed += handler;

        testSubject.Set(isEnabled);

        sonarLintSettings.Received(1).IsFocusOnNewCodeEnabled = isEnabled;
        testSubject.Current.IsEnabled.Should().Be(isEnabled);
        handler.Received(1).Invoke(testSubject, Arg.Is<NewCodeStatusChangedEventArgs>(e => e.NewStatus.IsEnabled == isEnabled));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Set_NotifiesSlCore(bool isSlCoreInitialized)
    {
        SetUpSlCoreService(isSlCoreInitialized);

        testSubject.Set(true);

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

    private void SetUpSlCoreService(bool isInitialized) =>
        serviceProvider.TryGetTransientService(out Arg.Any<INewCodeSLCoreService>()).Returns(info =>
        {
            info[0] = newCodeSlCoreService;
            return isInitialized;
        });
}
