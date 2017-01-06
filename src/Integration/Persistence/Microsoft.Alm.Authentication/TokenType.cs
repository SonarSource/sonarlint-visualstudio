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

namespace Microsoft.Alm.Authentication
{
    public enum TokenType
    {
        Unknown = 0,
        /// <summary>
        /// Azure Directory Access Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Access Token")]
        Access,
        /// <summary>
        /// Azure Directory Refresh Token
        /// </summary>
        [System.ComponentModel.Description("Azure Directory Refresh Token")]
        Refresh,
        /// <summary>
        /// Personal Access Token, can be compact or not.
        /// </summary>
        [System.ComponentModel.Description("Personal Access Token")]
        Personal,
        /// <summary>
        /// Federated Authentication (aka FedAuth) Token
        /// </summary>
        [System.ComponentModel.Description("Federated Authentication Token")]
        Federated,
        /// <summary>
        /// Used only for testing
        /// </summary>
        [System.ComponentModel.Description("Test-only Token")]
        Test,
    }
}

