﻿/*
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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.SLCore.Configuration;
using SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models;

namespace SonarLint.VisualStudio.Integration.SLCore
{
    [Export(typeof(ISLCoreConstantsProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SLCoreConstantsProvider : ISLCoreConstantsProvider
    {
        private readonly Lazy<string> ideName;

        [ImportingConstructor]
        public SLCoreConstantsProvider(IVsUIServiceOperation vsUiServiceOperation)
        {
            // lazy is used to keep the mef-constructor free threaded and to avoid calling this multiple times
            ideName = new Lazy<string>(() =>
                {
                    return vsUiServiceOperation.Execute<SVsShell, IVsShell, string>(shell =>
                    {
                        shell.GetProperty((int)__VSSPROPID5.VSSPROPID_AppBrandName, out var name);
                        return name as string ?? "Microsoft Visual Studio";
                    });
                }
            );
        }

        public ClientConstantsDto ClientConstants => new ClientConstantsDto(ideName.Value, $"SonarLint Visual Studio/{VersionHelper.SonarLintVersion}", Process.GetCurrentProcess().Id);

        public FeatureFlagsDto FeatureFlags => new FeatureFlagsDto(true, true, true, true, false, false, true, false);

        //We do not support telemetry now
        public TelemetryClientConstantAttributesDto TelemetryConstants => new TelemetryClientConstantAttributesDto("SLVS_SHOULD_NOT_SEND_TELEMETRY", default, default, default, default);
    }
}
