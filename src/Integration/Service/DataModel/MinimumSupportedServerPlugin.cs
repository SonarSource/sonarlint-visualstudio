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

using SonarLint.VisualStudio.Integration.Resources;
using System.Collections.Generic;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Service.DataModel
{
    internal class MinimumSupportedServerPlugin
    {
        public static readonly MinimumSupportedServerPlugin CSharp = new MinimumSupportedServerPlugin("csharp", Language.CSharp, "5.0");
        public static readonly MinimumSupportedServerPlugin VbNet = new MinimumSupportedServerPlugin("vbnet", Language.VBNET, "3.0");
        public static readonly IEnumerable<MinimumSupportedServerPlugin> All = new[] { CSharp, VbNet };

        private MinimumSupportedServerPlugin(string key, Language language, string minimumVersion)
        {
            Key = key;
            Language = language;
            MinimumVersion = minimumVersion;
        }

        public string Key { get; }
        public string MinimumVersion { get; }
        public Language Language { get; }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, Strings.MinimumSupportedServerPlugin, Language.Name, MinimumVersion);
        }

        public bool ISupported(EnvDTE.Project project)
        {
            return Language.ForProject(project).Equals(Language);
        }
    }
}
