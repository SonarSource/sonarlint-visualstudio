//-----------------------------------------------------------------------
// <copyright file="ConfigurableUriValidator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableUriValidator : UriValidator
    {
        #region Configurable properties

        public bool? IsValidUriOverride { get; set; }

        #endregion

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
            :base(supportedSchemes, insecureSchemes)
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
