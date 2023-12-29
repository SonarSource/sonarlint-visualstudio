/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

namespace SonarLint.VisualStudio.SLCore.Service.Lifecycle.Models
{
    public class TelemetryClientConstantAttributesDto
    {
        public string productKey { get; }
        public string productName { get; }
        public string productVersion { get; }
        public string ideVersion { get; }
        public Dictionary<string, object> additionalAttributes { get; }

        public TelemetryClientConstantAttributesDto(string productKey,
            string productName,
            string productVersion,
            string ideVersion,
            Dictionary<string, object> additionalAttributes)
        {
            this.productKey = productKey;
            this.productName = productName;
            this.productVersion = productVersion;
            this.ideVersion = ideVersion;
            this.additionalAttributes = additionalAttributes;
        }
    }
}
