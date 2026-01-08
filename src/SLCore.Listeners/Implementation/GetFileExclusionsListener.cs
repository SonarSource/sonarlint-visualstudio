/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Files;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class GetFileExclusionsListener(ILogger logger, IUserSettingsProvider userSettingsProvider, IActiveConfigScopeTracker activeConfigScopeTracker) : IGetFileExclusionsListener
{
    private readonly ILogger logger = logger.ForContext(SLCoreStrings.SLCoreName, SLCoreStrings.FileExclusionsLogContext);

    public Task<GetFileExclusionsResponse> GetFileExclusionsAsync(GetFileExclusionsParams parameters) => Task.FromResult(GetFileExclusionsFromSettings(parameters));

    private GetFileExclusionsResponse GetFileExclusionsFromSettings(GetFileExclusionsParams parameters)
    {
        var exception = new StreamJsonRpc.LocalRpcException("TEST: force internal error")
        {
            ErrorCode = (int)StreamJsonRpc.Protocol.JsonRpcErrorCode.InternalError,
            ErrorData = new StreamJsonRpc.Protocol.CommonErrorData
            {
                TypeName = "org.sonarsource.slcore.TestException",
                Message = "TEST: fake remote exception details",
                StackTrace = "at org.sonarsource.slcore.TestClass.testMethod(TestClass.java:42)\n" +
                             "at org.sonarsource.slcore.AnotherClass.call(AnotherClass.java:7)",
                HResult = unchecked((int)0x80131500),
            },
        };

        var remoteStackTraceField = typeof(Exception).GetField("_remoteStackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        remoteStackTraceField?.SetValue(exception,
            "at org.sonarsource.slcore.TestClass.testMethod(TestClass.java:42)" + Environment.NewLine +
            "at org.sonarsource.slcore.AnotherClass.call(AnotherClass.java:7)" + Environment.NewLine);

        throw exception;

        if (activeConfigScopeTracker.Current?.Id is var activeConfigScope && activeConfigScope != parameters.configurationScopeId)
        {
            logger.WriteLine(SLCoreStrings.ConfigurationScopeMismatch, parameters.configurationScopeId, activeConfigScope);
            return new GetFileExclusionsResponse([]);
        }

        var fileExclusions = userSettingsProvider.UserSettings.AnalysisSettings.NormalizedFileExclusions.ToArray();
        return new GetFileExclusionsResponse([.. fileExclusions]);
    }
}
