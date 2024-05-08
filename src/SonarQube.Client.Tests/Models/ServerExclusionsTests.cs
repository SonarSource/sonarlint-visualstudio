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

using System.Collections.Generic;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests.Models
{
    [TestClass]
    public class ServerExclusionsTests
    {
        [TestMethod]
        public void Equals_OtherObjIsNull_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(null).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_ExclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion2" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_GlobalExclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global2" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_InclusionsAreDifferent_False()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion2" });

            testSubject.Equals(other).Should().BeFalse();
        }

        [TestMethod]
        public void Equals_EverythingIsTheSame_True()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            var other = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global1" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(other).Should().BeTrue();
        }

        [TestMethod]
        public void Equals_SameReference_True()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "exclusion1" },
                globalExclusions: new[] { "global" },
                inclusions: new[] { "inclusion1" });

            testSubject.Equals(testSubject).Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_HasExclusions_AppendPathPrefix()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "path1", "**\\path2", "**/path3", "**path4", "*/*" },
                globalExclusions: Array.Empty<string>(),
                inclusions: null);

            testSubject.Exclusions.Should().BeEquivalentTo(
                "**/path1",
                "**/**\\path2",
                "**/path3",
                "**/**path4",
                "**/*/*");
            testSubject.GlobalExclusions.Should().BeEmpty();
            testSubject.Inclusions.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasGlobalExclusions_AppendPathPrefix()
        {
            var testSubject = new ServerExclusions(
                exclusions: null,
                globalExclusions: new[] { "path1", "**\\path2", "**/path3", "**path4", "*/*" },
                inclusions: Array.Empty<string>());

            testSubject.Exclusions.Should().BeEmpty();
            testSubject.GlobalExclusions.Should().BeEquivalentTo(
                "**/path1",
                "**/**\\path2",
                "**/path3",
                "**/**path4",
                "**/*/*");
            testSubject.Inclusions.Should().BeEmpty();
        }

        [TestMethod]
        public void Ctor_HasInclusions_AppendPathPrefix()
        {
            var testSubject = new ServerExclusions(
                exclusions: Array.Empty<string>(),
                globalExclusions: null,
                inclusions: new[] {"path1", "**\\path2", "**/path3", "**path4", "*/*"});

            testSubject.Exclusions.Should().BeEmpty();
            testSubject.GlobalExclusions.Should().BeEmpty();
            testSubject.Inclusions.Should().BeEquivalentTo(
                "**/path1",
                "**/**\\path2",
                "**/path3",
                "**/**path4",
                "**/*/*");
        }

        [TestMethod]
        public void ToDictionary_HasExclusions_ReturnsConcatenatedValues()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] { "**/path1", "**/*/path2" },
                globalExclusions: new[] { "**/path1" },
                inclusions: null);

            var result = testSubject.ToDictionary();

            result.Should().BeEquivalentTo(
                new Dictionary<string, string>
                {
                    {"sonar.exclusions", "**/path1,**/*/path2"},
                    {"sonar.global.exclusions", "**/path1"},
                    {"sonar.inclusions", ""}
                });
        }

        [TestMethod]
        public void ToDictionary_HasGlobalExclusions_ReturnsConcatenatedValues()
        {
            var testSubject = new ServerExclusions(
                exclusions: null,
                globalExclusions: new[] { "**/path1", "**/*/path2" },
                inclusions: new[] { "**/path1" });

            var result = testSubject.ToDictionary();

            result.Should().BeEquivalentTo(
                new Dictionary<string, string>
                {
                    {"sonar.exclusions", ""},
                    {"sonar.global.exclusions", "**/path1,**/*/path2"},
                    {"sonar.inclusions", "**/path1"}
                });
        }

        [TestMethod]
        public void ToDictionary_HasInclusions_ReturnsConcatenatedValues()
        {
            var testSubject = new ServerExclusions(
                exclusions: new[] {"**/path1"},
                globalExclusions: null,
                inclusions: new[] {"**/path1", "**/*/path2"});

            var result = testSubject.ToDictionary();

            result.Should().BeEquivalentTo(
                new Dictionary<string, string>
                {
                    {"sonar.exclusions", "**/path1"},
                    {"sonar.global.exclusions", ""},
                    {"sonar.inclusions", "**/path1,**/*/path2"}
                });
        }



        
    }
}
