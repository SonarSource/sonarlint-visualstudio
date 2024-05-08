/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.EslintBridgeClient
{
    [TestClass]
    public class EslintBridgeHttpHelperTests
    {
        [TestMethod]
        public async Task MakeCallAsync_MakesCall()
        {
            var eslintBridgeProcess = new Mock<IEslintBridgeProcess>();
            eslintBridgeProcess.Setup(p => p.Start()).ReturnsAsync(100);

            var httpWrapper = new Mock<IEslintBridgeHttpWrapper>();

            var request = new object();
            var endpoint = "api/someEndpoint";

            await EslintBridgeHttpHelper.MakeCallAsync(eslintBridgeProcess.Object, httpWrapper.Object, endpoint, request, CancellationToken.None);

            eslintBridgeProcess.Verify(p => p.Start(), Times.Once());
            eslintBridgeProcess.VerifyNoOtherCalls();

            var uri = new Uri("http://localhost:100/api/someEndpoint");

            httpWrapper.Verify(w => w.PostAsync(uri, request, CancellationToken.None));
            httpWrapper.VerifyNoOtherCalls();
        }
    }
}
