﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class ThreadHandlingTests
    {
        [TestMethod]
        public async Task CheckAccess_IsUIThread_ReturnsTrue()
        {
            bool actual = false;

            await RunOnUIThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = testSubject.CheckAccess();
            });

            actual.Should().BeTrue();
        }

        [TestMethod]
        public async Task CheckAccess_NotUIThread_ReturnsFlase()
        {
            bool actual = false;

            await RunOnBackgroundThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = testSubject.CheckAccess();
            });

            actual.Should().BeFalse();
        }

        [TestMethod]
        public async Task ThrowIfOnUIThread_IsUIThread_Throws()
        {
            bool actual = false;

            await RunOnUIThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = Throws(testSubject.ThrowIfOnUIThread);
            });

            actual.Should().BeTrue();
        }

        [TestMethod]
        public async Task ThrowIfOnUIThread_NotUIThread_DoesNotThrow()
        {
            bool actual = false;

            await RunOnBackgroundThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = Throws(testSubject.ThrowIfOnUIThread);
            });

            actual.Should().BeFalse();
        }

        [TestMethod]
        public async Task ThrowIfNotOnUIThread_IsUIThread_ShouldNotThrow()
        {
            bool actual = false;

            await RunOnUIThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = Throws(testSubject.ThrowIfNotOnUIThread);
            });

            actual.Should().BeFalse();
        }

        [TestMethod]
        public async Task ThrowIfNotOnUIThread_NotUIThread_Throws()
        {
            bool actual = false;

            await RunOnBackgroundThread(() =>
            {
                var testSubject = new ThreadHandling();
                actual = Throws(testSubject.ThrowIfNotOnUIThread);
            });

            actual.Should().BeTrue();
        }

        private static async Task RunOnUIThread(Action op)
        {
            await Task.Run(() =>
            {
                ThreadHelper.SetCurrentThreadAsUIThread();
                op();
            });
        }

        private static async Task RunOnBackgroundThread(Action op)
        {
            // Other tests could have called ThreadHelper.SetCurrentThreadAsUIThread(), so
            // we have no idea which thread is currently considered to be the UI thread.

            // To make this test reliable, we set this current thread to the UIThread,
            // then run the check on another thread.
            ThreadHelper.SetCurrentThreadAsUIThread();
            await Task.Run(() =>
            {
                op();
            });
        }

        private static bool Throws(Action op)
        {
            try
            {
                op();
            }
            catch(Exception)
            {
                return true;
            }
            return false;
        }
    }
}
