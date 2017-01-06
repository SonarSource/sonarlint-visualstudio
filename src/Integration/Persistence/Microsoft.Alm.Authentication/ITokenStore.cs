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

namespace Microsoft.Alm.Authentication
{
    public interface ITokenStore
    {
        /// <summary>
        /// Deletes a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token is being deleted.</param>
        void DeleteToken(Uri targetUri);
        /// <summary>
        /// Reads a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token to read.</param>
        /// <param name="token">A <see cref="Token"/> if successful; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if successful; otherwise <see langword="false"/>.</returns>
        bool ReadToken(Uri targetUri, out Token token);
        /// <summary>
        /// Writes a <see cref="Token"/> to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the token, used when reading back from storage.
        /// </param>
        /// <param name="token">The <see cref="Token"/> to be written.</param>
        void WriteToken(Uri targetUri, Token token);
    }
}

