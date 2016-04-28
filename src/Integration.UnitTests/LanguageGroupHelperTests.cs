//-----------------------------------------------------------------------
// <copyright file="LanguageGroupHelperTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class LanguageGroupHelperTests
    {
        [TestMethod]
        public void LanguageGroupHelper_GetLanguage()
        {
            Assert.AreSame(Language.CSharp, LanguageGroupHelper.GetLanguage(LanguageGroup.CSharp));
            Assert.AreSame(Language.VBNET, LanguageGroupHelper.GetLanguage(LanguageGroup.VB));
            Assert.AreSame(Language.Unknown, LanguageGroupHelper.GetLanguage(LanguageGroup.Unknown));

            using (new AssertIgnoreScope())
            {
                Exceptions.Expect<InvalidOperationException>(()=>LanguageGroupHelper.GetLanguage((LanguageGroup)int.MinValue));
            }
        }

        [TestMethod]
        public void LanguageGroupHelper_GetLanguageGroup()
        {
            Assert.AreEqual(LanguageGroup.CSharp, LanguageGroupHelper.GetLanguageGroup(Language.CSharp));
            Assert.AreEqual(LanguageGroup.VB, LanguageGroupHelper.GetLanguageGroup(Language.VBNET));
            Assert.AreEqual(LanguageGroup.Unknown, LanguageGroupHelper.GetLanguageGroup(Language.Unknown));
            Assert.AreEqual(LanguageGroup.Unknown, LanguageGroupHelper.GetLanguageGroup(new Language("Java", "Java", new Guid().ToString())));
        }


        [TestMethod]
        public void LanguageGroupHelper_GetProjectGroup()
        {
            // C#
            var csProject = new ProjectMock("cs.proj");
            csProject.SetCSProjectKind();
            Assert.AreEqual(LanguageGroup.CSharp, LanguageGroupHelper.GetProjectGroup(csProject));

            // VB
            var vbProject = new ProjectMock("vb.proj");
            vbProject.SetVBProjectKind();
            Assert.AreEqual(LanguageGroup.VB, LanguageGroupHelper.GetProjectGroup(vbProject));

            // Other
            Assert.AreEqual(LanguageGroup.Unknown, LanguageGroupHelper.GetProjectGroup(new ProjectMock("other.proj")));
        }
    }
}
