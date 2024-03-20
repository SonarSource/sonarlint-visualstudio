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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core.Process;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Core.Process;

[TestClass]
public class SLCoreErrorLoggerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreErrorLoggerFactory, ISLCoreErrorLoggerFactory>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreErrorLoggerFactory>();
    }
    
    [TestMethod]
    public void ReadsStreamInBackground()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => Task.Run(info.Arg<Func<Task<int>>>()));
        var testLogger = new TestLogger();
        var errorLoggerFactory = new SLCoreErrorLoggerFactory(testLogger, threadHandling);
        
        using var _ = errorLoggerFactory.Create(new StreamReader(new MemoryStream()));

        threadHandling.ReceivedWithAnyArgs().RunOnBackgroundThread(default(Func<Task<int>>));
    }

    [TestMethod]
    public void ReadsAllLines()
    {
        var threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => Task.Run(info.Arg<Func<Task<int>>>()));
        var testLogger = new TestLogger();
        var errorLoggerFactory = new SLCoreErrorLoggerFactory(testLogger, threadHandling);
        var memoryStream = new MemoryStream();
        var streamWriter = new StreamWriter(memoryStream);
        streamWriter.WriteLine("line1");
        streamWriter.WriteLine("line2");
        streamWriter.WriteLine("line3");
        streamWriter.Flush();
        memoryStream.Seek(0, SeekOrigin.Begin);
        using var slCoreErrorLogger = errorLoggerFactory.Create(new StreamReader(memoryStream));

        WaitForConditionOrTimeout(() => testLogger.OutputStrings.Count == 3);

        testLogger.OutputStrings.Should().BeEquivalentTo(new List<string>
        {
            "[SLCORE-ERR] line1\r\n",
            "[SLCORE-ERR] line2\r\n",
            "[SLCORE-ERR] line3\r\n"
        });
    }

    private static void WaitForConditionOrTimeout(Func<bool> condition, double timeoutInMilliseconds = 500)
    {
        var hangTimer = Stopwatch.StartNew();
        while (hangTimer.Elapsed.TotalMilliseconds < timeoutInMilliseconds)
        {
            if (condition())
            {
                return;
            }
        }
    }
}
