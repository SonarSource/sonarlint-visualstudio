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

using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Integration.Service
{
    /// <summary>
    /// XML-serializable data class for Roslyn export profile information
    /// </summary>
    [XmlRoot]
    public class RoslynExportProfile
    {
        [XmlAttribute]
        public string Version { get; set; }

        public Deployment Deployment { get; set; }

        public Configuration Configuration { get; set; }

        #region Serialization

        public static RoslynExportProfile Load(TextReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(RoslynExportProfile));
            RoslynExportProfile profile = serializer.Deserialize(reader) as RoslynExportProfile;
            return profile;
        }

        #endregion
    }

}
