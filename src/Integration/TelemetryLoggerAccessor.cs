//-----------------------------------------------------------------------
// <copyright file="TelemetryLoggerAccessor.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using System;

namespace SonarLint.VisualStudio.Integration
{
    internal static class TelemetryLoggerAccessor
    {
        public static ITelemetryLogger GetLogger(IServiceProvider serviceProvider)
        {
            IComponentModel componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            if (componentModel != null)
            {
                return componentModel.GetService<ITelemetryLogger>();
            }

            return null;
        }
    }
}
