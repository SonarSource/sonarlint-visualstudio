/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Integration.Connection;

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
