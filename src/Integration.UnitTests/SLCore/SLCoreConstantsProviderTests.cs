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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.SLCore
{
    [TestClass]
    public class SLCoreConstantsProviderTests
    {
        SLCoreConstantsProvider testSubject;
        private IVsShell vsShell;

        [TestInitialize]
        public void Setup()
        {
            vsShell = Substitute.For<IVsShell>();
            var vsServiceOperation = Substitute.For<IVsUIServiceOperation>();
            vsServiceOperation.Execute<SVsShell, IVsShell, string>(Arg.Any<Func<IVsShell, string>>()).Returns(info =>
            {
                var func = info.Arg<Func<IVsShell, string>>();
                return func(vsShell);
            });
            testSubject = new SLCoreConstantsProvider(vsServiceOperation);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SLCoreConstantsProvider, ISLCoreConstantsProvider>(
                MefTestHelpers.CreateExport<IVsUIServiceOperation>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SLCoreConstantsProvider>();
        }

        [TestMethod]
        public void ClientConstants_ShouldBeExpected()
        {
            const string ideName = "MyIde";
            SetupIdeName(ideName);

            var expectedClientConstants = new ClientConstantsDto(ideName, $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}",
                Process.GetCurrentProcess().Id);
            var actual = testSubject.ClientConstants;

            actual.Should().BeEquivalentTo(expectedClientConstants);
        }

        [TestMethod]
        public void ClientConstants_VsOperationCalledOnlyOnce()
        {
            SetupIdeName("somename");

            _ = testSubject.ClientConstants;
            _ = testSubject.ClientConstants;
            _ = testSubject.ClientConstants;
            _ = testSubject.ClientConstants;

            vsShell.ReceivedWithAnyArgs(1).GetProperty(default, out _);
        }

        [TestMethod]
        public void FeatureFlags_ShouldBeExpected()
        {
            var expectedFeatureFlags = new FeatureFlagsDto(true, true, true, true, false, false, true, false);
            var actual = testSubject.FeatureFlags;

            actual.Should().BeEquivalentTo(expectedFeatureFlags);
        }

        [TestMethod]
        public void TelemetryConstants_ShouldBeExpected()
        {
            var expectedTelemetryConstants =
                new TelemetryClientConstantAttributesDto("SLVS_SHOULD_NOT_SEND_TELEMETRY", default, default, default, default);
            var actual = testSubject.TelemetryConstants;

            actual.Should().BeEquivalentTo(expectedTelemetryConstants);
        }

        [TestMethod]
        public void StandaloneLanguages_ShouldBeExpected()
        {
            var expected = new[]
            {
                Language.JS,
                Language.TS,
                Language.CSS,
                Language.C,
                Language.CPP,
                Language.CS,
                Language.VBNET,
                Language.SECRETS
            };
            var actual = testSubject.LanguagesInStandaloneMode;

            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void AnalyzableLanguages_ShouldBeExpected()
        {
            var expected = new[] { Language.SECRETS };
            var actual = testSubject.SLCoreAnalyzableLanguages;

            actual.Should().BeEquivalentTo(expected);
        }
        
        [TestMethod]
        public void Verify_AllConfiguredLanguagesAreKnown()
        {
            var slCoreConstantsProvider = new SLCoreConstantsProvider(Substitute.For<IVsUIServiceOperation>());

            var languages = slCoreConstantsProvider.LanguagesInStandaloneMode
                .Concat(slCoreConstantsProvider.SLCoreAnalyzableLanguages)
                .Select(x => x.ConvertToCoreLanguage());

            languages.Should().NotContain(Core.Language.Unknown);
        }
        
        [TestMethod]
        public void Verify_AllConfiguredLanguagesHaveKnownPluginKeys()
        {
            var slCoreConstantsProvider = new SLCoreConstantsProvider(Substitute.For<IVsUIServiceOperation>());

            var languages = slCoreConstantsProvider.LanguagesInStandaloneMode
                .Concat(slCoreConstantsProvider.SLCoreAnalyzableLanguages)
                .Select(x => x.GetPluginKey());

            languages.Should().NotContainNulls();
        }


        private void SetupIdeName(object name)
        {
            vsShell.GetProperty((int)__VSSPROPID5.VSSPROPID_AppBrandName, out Arg.Any<object>()).Returns(info =>
            {
                info[1] = name;
                return 0;
            });
        }
    }
}
