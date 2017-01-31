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

using SonarLint.VisualStudio.Integration.Persistence;
 using Xunit;
using System;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    
    public class BoundSonarQubeProjectTests
    {
        [Fact]
        public void BoundSonarQubeProject_Serialization()
        {
            // Arrange
            var serverUri = new Uri("https://finding-nemo.org");
            var projectKey = "MyProject Key";
            var testSubject = new BoundSonarQubeProject(serverUri, projectKey, new BasicAuthCredentials("used", "pwd".ToSecureString()));

            // Act (serialize + de-serialize)
            string data = JsonHelper.Serialize(testSubject);
            BoundSonarQubeProject deserialized = JsonHelper.Deserialize<BoundSonarQubeProject>(data);

            // Assert
            deserialized.Should().NotBe(testSubject);
            deserialized.ProjectKey.Should().Be(testSubject.ProjectKey);
            deserialized.ServerUri.Should().Be(testSubject.ServerUri);
            deserialized.Credentials.Should().BeNull();
        }
    }
}
