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

using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.TestInfrastructure;


namespace SonarLint.VisualStudio.Core.UnitTests.SystemAbstractions
{
    [TestClass]
    public class DefaultCurrentTimeProviderTests
    {
        [TestMethod]
        public void MefCtor_IsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<DefaultCurrentTimeProvider>();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<DefaultCurrentTimeProvider, ICurrentTimeProvider>();
        }

        [TestMethod]
        public void Now_ReturnsCurrentTime()
        {
            ICurrentTimeProvider currentTimeProvider = new DefaultCurrentTimeProvider();
            currentTimeProvider.Now.Should().BeOnOrBefore(DateTimeOffset.Now);
        }

        [TestMethod]
        public void LocalTimeZone_ReturnsLocalTimeZone()
        {
            ICurrentTimeProvider currentTimeProvider = new DefaultCurrentTimeProvider();
            currentTimeProvider.LocalTimeZone.Should().Be(TimeZoneInfo.Local);
        }
    }
}
