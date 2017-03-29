/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
