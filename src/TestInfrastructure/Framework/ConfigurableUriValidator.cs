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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Integration.Connection;

namespace SonarLint.VisualStudio.TestInfrastructure
{
    internal class ConfigurableUriValidator : UriValidator
    {
        #region Configurable properties

        public bool? IsValidUriOverride { get; set; }

        #endregion Configurable properties

        public ConfigurableUriValidator()
        {
        }

        public ConfigurableUriValidator(bool? isValidUriOverride)
        {
            this.IsValidUriOverride = isValidUriOverride;
        }

        public ConfigurableUriValidator(ISet<string> supportedSchemes)
            : base(supportedSchemes)
        {
        }

        public ConfigurableUriValidator(ISet<string> supportedSchemes, ISet<string> insecureSchemes)
            : base(supportedSchemes, insecureSchemes)
        {
        }

        public override bool IsValidUri(string uriString)
        {
            if (this.IsValidUriOverride.HasValue)
            {
                return this.IsValidUriOverride.Value;
            }
            return base.IsValidUri(uriString);
        }

        public override bool IsValidUri(Uri uri)
        {
            if (this.IsValidUriOverride.HasValue)
            {
                return this.IsValidUriOverride.Value;
            }
            return base.IsValidUri(uri);
        }
    }
}