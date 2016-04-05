//-----------------------------------------------------------------------
// <copyright file="ILocalService.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Marker for local service.
    /// Local service are services which are hosted by <see cref="IHost"/>. <seealso cref="System.IServiceProvider"/>.
    /// </summary>
    public interface ILocalService
    {
        ///  Local services need to derive from this interface and registered in <see cref = "VsSessionHost.SupportedLocalServices" />.
        ///  The mapping between the interface and the concrete class implemented needed to be registered in <see cref="VsSessionHost"/>
        ///  so that it could be serviced.
        ///  It's recommended to call service.<see cref="IServiceProviderExtensions.AssertLocalServiceIsNotNull{T}(T)"/> once the service
        ///  retrieved to indicated that it's mandatory and pick up cases when it's not registered.
        ///  The main reason for those service is testability and abstraction, the fact that we have those
        ///  implementations as services is a by-product rather than a goal that we strived for.
    }
}
