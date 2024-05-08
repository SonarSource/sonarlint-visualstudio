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

namespace SonarLint.VisualStudio.CFamily.SubProcess.UnitTests
{
    [TestClass]
    public class NoOpMessageHandlerTests
    {
        [TestMethod]
        public void Singleton()
        {
            var instance1 = NoOpMessageHandler.Instance;
            var instance2 = NoOpMessageHandler.Instance;

            instance1.Should().BeSameAs(instance2);
        }

        [TestMethod]
        public void AnalysisSucceeded_IsTrue()
        {
            NoOpMessageHandler.Instance.AnalysisSucceeded.Should().BeTrue();
        }

        [TestMethod]
        public void HandleOutput_DoesNothing()
        {
            var message = new Message("key", "file", 1, 2, 3, 4, "text", false, null, Array.Empty<Fix>());

            var testSubject = new NoOpMessageHandler();

            testSubject.IssueCount.Should().Be(0); // check initial state

            testSubject.HandleMessage(message);
            testSubject.HandleMessage(null);

            testSubject.IssueCount.Should().Be(0);
        }
    }
}
