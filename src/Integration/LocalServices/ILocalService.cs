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

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Marker for local service.
    /// Local service are services which are hosted by <see cref="IHost"/>. <seealso cref="System.IServiceProvider"/>.
    /// </summary>
    public interface ILocalService
    {
        ///  Local services need to derive from this interface and registered in <see cref = "VsSessionHost.SupportedLocalServices" />.
        ///  The mapping between the interface and the concrete class implemented needed to be registered in <see cref="VsSessionHost"/>
        ///  so that it could be serviced.
        ///  It's recommended to call service.<see cref="IServiceProviderExtensions.AssertLocalServiceIsNotNull{T}(T)"/> once the service
        ///  retrieved to indicated that it's mandatory and pick up cases when it's not registered.
        ///  The main reason for those service is testability and abstraction, the fact that we have those
        ///  implementations as services is a by-product rather than a goal that we strived for.
    }
}
