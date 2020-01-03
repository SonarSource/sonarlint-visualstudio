﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SonarQube.Client.Models
{
    public class SonarQubeRule
    {
        public SonarQubeRule(string key, string repositoryKey, bool isActive, SonarQubeIssueSeverity severity, IDictionary<string, string> parameters)
        {
            Key = key;
            RepositoryKey = repositoryKey;
            IsActive = isActive;
            Severity = severity;

            Parameters = new ReadOnlyDictionary<string, string>(parameters);
        }

        public string Key { get; }

        public string RepositoryKey { get; }

        public bool IsActive { get; }

        public SonarQubeIssueSeverity Severity { get; }

        /// <summary>
        /// When the rule is active, contains the parameters that are set in the corresponding quality profile.
        /// This is empty dictionary if the rule is inactive, or does not have parameters.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters { get; }
    }
}
