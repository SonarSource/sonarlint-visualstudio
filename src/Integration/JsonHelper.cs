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

using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace SonarLint.VisualStudio.Integration
{
    internal static class JsonHelper
    {
        public static T Deserialize<T>(string json)
        {
            using (var reader = new StringReader(json))
            {
                using (var textReader = new JsonTextReader(reader))
                {
                    return JsonSerializer.CreateDefault().Deserialize<T>(textReader);
                }
            }
        }

        public static string Serialize(object item)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                using (var textWriter = new JsonTextWriter(writer))
                {
                    var serializer = JsonSerializer.CreateDefault();
                    serializer.Formatting = Formatting.Indented;
                    serializer.Serialize(textWriter, item);
                }
            }

            return sb.ToString();
        }
    }
}
