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

using Newtonsoft.Json;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

/// <summary>
/// The information for the connection related properties are now stored in a separate file, but they are needed here for backward compatibility with previous binding formats
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
internal class BindingJsonModel
{
    public string ServerConnectionId { get; set; }
    public Uri ServerUri { get; set; } // left here for backward compatibility reasons
    public SonarQubeOrganization Organization { get; set; } // left here for backward compatibility reasons
    public string ProjectKey { get; set; }
    public string ProjectName { get; set; } // left here for backward compatibility reasons
    public Dictionary<Language, ApplicableQualityProfile> Profiles { get; set; }
}
