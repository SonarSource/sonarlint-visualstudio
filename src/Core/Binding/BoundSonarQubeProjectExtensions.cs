﻿/*
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

using System;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding
{
    public static class BoundSonarQubeProjectExtensions
    {
        public static ConnectionInformation CreateConnectionInformation(this BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var connection = new ConnectionInformation(binding.ServerUri, binding.Credentials);

            connection.Organization = binding.Organization;
            return connection;
        }

        public static ConnectionInformation CreateConnectionInformation(this BoundServerProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var connection = new ConnectionInformation(binding.ServerConnection.ServerUri, binding.ServerConnection.Credentials);

            connection.Organization = binding.ServerConnection is ServerConnection.SonarCloud sc ? new SonarQubeOrganization(sc.OrganizationKey, null) : null;
            return connection;
        }
    }
}
