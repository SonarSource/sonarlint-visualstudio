/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.RoslynAnalyzerServer.Http;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Http;

[TestClass]
public class HttpListenerFactoryTest
{
    private HttpListenerFactory testSubject = null!;

    [TestInitialize]
    public void TestInitialize() => testSubject = new HttpListenerFactory();

    [TestMethod]
    [DataRow(8080)]
    [DataRow(1234)]
    [DataRow(60000)]
    public void Create_ShouldReturnListenerWithCorrectPrefix(int port)
    {
        var listener = testSubject.Create(port);

        listener.Should().NotBeNull();
        listener.Prefixes.Count.Should().Be(1);
        listener.Prefixes.Should().Contain($"http://127.0.0.1:{port}/");
    }
}
