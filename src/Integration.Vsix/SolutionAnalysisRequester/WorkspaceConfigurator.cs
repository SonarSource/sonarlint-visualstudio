/*
 * SonarLint for VisualStudio
 * Copyright (C) 2015-2016 SonarSource SA
 * mailto:contact@sonarsource.com
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
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    public class WorkspaceConfigurator : IWorkspaceConfigurator
    {
        private readonly Workspace workspace;

        public WorkspaceConfigurator(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            this.workspace = workspace;
        }

        public void ToggleBooleanOptionKey(OptionKey optionKey)
        {
            bool current = (bool)workspace.Options.GetOption(optionKey);
            OptionSet newOptions = workspace.Options.WithChangedOption(optionKey, !current);
            workspace.Options = newOptions;
        }

        public IOption FindOptionByName(string feature, string name)
        {
            object localOptionService = FindOptionService(workspace);
            Debug.Assert(localOptionService != null);

            const string methodName = "GetRegisteredOptions";
            MethodInfo getOptionsMethod = localOptionService.GetType()?.GetMethod(methodName);
            Debug.Assert(getOptionsMethod != null);

            IEnumerable<IOption> options = getOptionsMethod?.Invoke(localOptionService, null) as IEnumerable<IOption>;

            options = options?.Where(o => o.Feature.Equals(feature, StringComparison.Ordinal) && o.Name.Equals(name, StringComparison.Ordinal));
            return options?.FirstOrDefault();
        }

        private static object FindOptionService(Workspace workspace)
        {
            const string optionServiceTypeName = "Microsoft.CodeAnalysis.Options.IOptionService";
            const string getServiceMethod = "GetService";

            Type optionServiceType = typeof(IOption).Assembly.GetType(optionServiceTypeName, false);
            Debug.Assert(optionServiceType != null);

            return workspace.Services.GetType()
                    ?.GetMethod(getServiceMethod)
                    ?.MakeGenericMethod(optionServiceType)
                    ?.Invoke(workspace.Services, null);
        }
    }
}
