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
using System.Diagnostics;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal static class ResourceHelper
    {
        private static ResourceDictionary sharedResourcesCache;

        public static ResourceDictionary SharedResources
        {
            get
            {
                if (sharedResourcesCache == null)
                {
                    Uri resourceUri = new Uri("/SonarLint.VisualStudio.Integration;component/TeamExplorer/CommonStyles.xaml", UriKind.RelativeOrAbsolute);
                    sharedResourcesCache = (ResourceDictionary)Application.LoadComponent(resourceUri);
                }
                return sharedResourcesCache;
            }
        }

        public static T Get<T>(string resourceName) where T : class
        {
            var resource = SharedResources[resourceName] as T;
            Debug.Assert(resource != null, $"Failed to load resource '{resourceName}' as {typeof(T).Name}");
            return resource;
        }
    }
}
