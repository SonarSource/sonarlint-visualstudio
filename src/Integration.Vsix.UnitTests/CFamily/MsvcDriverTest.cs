/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.Vsix.CFamily.UnitTests
{
    [TestClass]
    public class MsvcDriverTest
    {
        private const string X86_MACROS = "#define _M_IX86 600\n#define _M_IX86_FP 2\n";
        private const string X64_MACROS = "#define _WIN64 1\n#define _M_X64 100\n#define _M_AMD64 100\n";
        readonly CFamilyHelper.Capture compiler = new CFamilyHelper.Capture()
        {
            Executable = "",
            StdOut = "",
            CompilerVersion = "19.10.25017",
            X64 = false
        };

        [TestMethod]
        public void File_Type()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "file.cpp" },
                }
            });
            req.File.Should().Be("basePath/file.cpp");
            req.Flags.Should().Be(Request.MS | Request.CPlusPlus | Request.CPlusPlus11 | Request.CPlusPlus14 | Request.SonarLint);

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "file.c" },
                }
            });
            req.File.Should().Be("basePath/file.c");
            req.Flags.Should().Be(Request.MS | Request.C99 | Request.C11 | Request.SonarLint);

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "/TP", "file.c" },
                }
            });
            req.File.Should().Be("basePath/file.c");
            req.Flags.Should().Be(Request.MS | Request.CPlusPlus | Request.CPlusPlus11 | Request.CPlusPlus14 | Request.SonarLint);

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "/TC", "file.cpp" },
                }
            });
            req.File.Should().Be("basePath/file.cpp");
            req.Flags.Should().Be(Request.MS | Request.C99 | Request.C11 | Request.SonarLint);
        }

        [TestMethod]
        public void File_RelativeAndAbsolutePath()
        {
            // 1. Relative Path
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "root",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "subdir1\\subdir2\\file.cpp" }, //  \ should be converted to /
                }
            });
            req.File.Should().Be("root/subdir1/subdir2/file.cpp");


            // 2. Absolute path
            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "root",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "d://subdir1\\subdir2\\file.cpp" }, //  should not be changed
                }
            });
            req.File.Should().Be("d://subdir1\\subdir2\\file.cpp");
        }

        [TestMethod]
        public void Should_Skip_Cx_And_Cli()
        {
            Request reqCx = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "/ZW", "file.cpp" },
                }
            });
            reqCx.File.Should().Be("");

            Request reqCli = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() { "cl.exe", "/clr", "file.cpp" },
                }
            });
            reqCli.File.Should().Be("");
        }

        [TestMethod]
        public void Include_Directories()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>() { "INCLUDE=system" },
                    Cmd = new List<string>() { "cl.exe", "/I", "c:/user" },
                }
            });
            req.IncludeDirs.Should().BeEquivalentTo("c:/user", "basePath/system");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>() { "INCLUDE=system" },
                    Cmd = new List<string>() { "cl.exe", "/I", "d:\\user", "/X" },
                }
            });
            req.IncludeDirs.Should().BeEquivalentTo("d:\\user");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "cwd",
                    Env = new List<string>() { "INCLUDE=system" },
                    Cmd = new List<string>() { "cl.exe", "/I", "user" },
                }
            });
            req.IncludeDirs.Should().BeEquivalentTo("cwd/user", "cwd/system");

        }

        // Regression test for SLVS-1014: https://github.com/SonarSource/sonarlint-visualstudio/issues/1014
        [TestMethod]
        public void Include_Directories_MultipleIncludes()
        {
            var req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "cwd",
                    Env = new List<string>()
                    {
                        "INCLUDE=system;;\r\tfoo\r\t;;bar\r\n",     // leading and training whitespace should be stripped
                        "INCLUDE=include2a;;;;include2b",           // empty entries should be ignored
                        "XXXINCLUDE=shouldbeignored",               // other environment variable
                        "INCLUDE=include3a;\r\t\n  ; ;include3b",   // whitespace entries should be ignored
                        "INCLUDE=subDir1\\subdir2\\relativePath1.txt", // \ should be converted to /
                        "INCLUDE=c:\\absPath1"                      // Absolute path should not be changed
                    },
                    Cmd = new List<string>(), // no /I parameters
                }
            });

            req.IncludeDirs.Should().BeEquivalentTo(
                "cwd/system", "cwd/foo", "cwd/bar",
                "cwd/include2a", "cwd/include2b",
                "cwd/include3a", "cwd/include3b",
                "cwd/subDir1/subdir2/relativePath1.txt",
                "c:\\absPath1");
        }

        [TestMethod]
        public void Include_Directories_SlashIArgs()
        {
            var req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "cwd",
                    Env = new List<string>()
                    {
                        "NotAnInclude=should be ignored"
                    },
                    Cmd = new List<string>() { "cl.exe",
                        "/I", "\r\tuser  \n", // leading and training whitespace should be stripped
                        "/I", "foo",
                        "i", "should be ignored - case-sensitive",
                        "/I", "bar",
                        "/I", "subdir2\\relativePath1.txt", // \ should be converted to /
                        "/I", "c:\\absPath1"                // Absolute path should not be changed

                    }
                }
            });
            req.IncludeDirs.Should().BeEquivalentTo(
                "cwd/user",
                "cwd/foo",
                "cwd/bar",
                "cwd/subdir2/relativePath1.txt",
                "c:\\absPath1");
        }

        [TestMethod]
        public void Define_Macro()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/Dname1",
                      "/Dname2=value2"
                    },
                }
            });
            req.Predefines.Should().ContainAll("#define name1 1\n", "#define name2 value2\n");
        }

        [TestMethod]
        public void Undefine_Macro_With_Dash_Syntax()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "-Uname"
                    },
                }
            });
            req.Predefines.Should().Contain("#undef name\n");
        }

        [TestMethod]
        public void Forced_Include()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/FI", "file.h"
                    },
                }
            });
            req.Predefines.Should().EndWith("#include \"file.h\"\n");
        }

        [TestMethod]
        public void Arch()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/arch:IA32"
                    },
                }
            });
            req.Predefines.Should().ContainAll("#define _M_IX86_FP 0\n");
            req.Predefines.Should().NotContainAny("#define __AVX__", "#define __AVX2__");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/arch:SSE"
                    },
                }
            });
            req.Predefines.Should().ContainAll("#define _M_IX86_FP 1\n");
            req.Predefines.Should().NotContainAny("#define __AVX__", "#define __AVX2__");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/arch:AVX2"
                    },
                }
            });
            req.Predefines.Should().ContainAll("#define _M_IX86_FP 2\n", "#define __AVX__ 1\n", "#define __AVX2__ 1\n");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/arch:AVX"
                    },
                }
            });
            req.Predefines.Should().ContainAll("#define _M_IX86_FP 2\n", "#define __AVX__ 1\n");
            req.Predefines.Should().NotContainAny("#define __AVX2__");
        }

        [TestMethod]
        public void Version()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    StdOut = "",
                    CompilerVersion = "18.00.21005.1",
                    X64=false
                },
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe"
                    },
                }
            });
            req.MsVersion.Should().Be(180021005);
            req.Predefines.Should().Contain("#define _MSC_FULL_VER 180021005\n" +
              "#define _MSC_VER 1800\n" +
              "#define _MSC_BUILD 0\n");
            req.Predefines.Should().Contain(X86_MACROS);
            req.Predefines.Should().NotContain(X64_MACROS);
            req.Predefines.Should().NotContain("#define _HAS_CHAR16_T_LANGUAGE_SUPPORT 1\n");


            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    StdOut = "",
                    CompilerVersion = "19.10.25017",
                    X64=true
                },
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe"
                    },
                }
            });
            req.MsVersion.Should().Be(191025017);
            req.Predefines.Should().Contain("#define _MSC_FULL_VER 191025017\n" +
              "#define _MSC_VER 1910\n" +
              "#define _MSC_BUILD 0\n");
            req.Predefines.Should().NotContain(X86_MACROS);
            req.Predefines.Should().Contain(X64_MACROS);
            req.Predefines.Should().Contain("#define _HAS_CHAR16_T_LANGUAGE_SUPPORT 1\n");

        }

        [TestMethod]
        [DataRow("18.24.21005.1", false)] // major < 19, minor > 23
        [DataRow("19.22.21005.1", false)] // major = 19, minor < 23
        [DataRow("19.23.21005.1", false)] // major = 19, minor = 23
        [DataRow("19.24.21005.1", true)]  // major = 19, minor > 23
        [DataRow("19.101.21005.1", true)] // major = 19, minor > 23
        [DataRow("20.22.21005.1", false)] // v20 hasn't been released yet so we don't know whether the macro will still exist -> assume not
        public void Version_HasConditionalExplicit(string compilerVersion, bool expectedToContainHasConditionalExplicit)
        {
            var req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    StdOut = "",
                    CompilerVersion = compilerVersion,
                    X64=true
                },
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe"
                    },
                }
            });

            req.Predefines.Contains("#define _HAS_CONDITIONAL_EXPLICIT 0\n").Should().Be(expectedToContainHasConditionalExplicit);
        }

        [TestMethod]
        public void Char_Is_Unsigned()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/std:c++14",
                      "/J"
                    },
                }
            });
            req.Flags.Should().Be(Request.MS | Request.CPlusPlus | Request.CPlusPlus11 | Request.CPlusPlus14
                | Request.CharIsUnsigned | Request.SonarLint);
            req.Predefines.Should().Contain("#define _CHAR_UNSIGNED 1\n");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe"
                    },
                }
            });
            req.Flags.Should().Be(Request.MS | Request.CPlusPlus | Request.CPlusPlus11 | Request.CPlusPlus14 | Request.SonarLint);
            req.Predefines.Should().NotContain("#define _CHAR_UNSIGNED 1\n");
        }

        [TestMethod]
        public void Microsoft_Extensions()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/Za",
                      "file.c"
                    },
                }
            });
            req.Flags.Should().Be(Request.C99 | Request.C11 | Request.SonarLint);
            req.Predefines.Should().Contain("#define __STDC__ 1\n");
            req.File.Should().Be("basePath/file.c");

            req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "basePath",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      "/Za",
                      "/std:c++17",
                      "c:\\file.cpp"
                    },
                }
            });
            req.Flags.Should().Be(Request.CPlusPlus | Request.CPlusPlus11 | Request.CPlusPlus14 |
                Request.CPlusPlus17 | Request.OperatorNames | Request.SonarLint);
            req.File.Should().Be("c:\\file.cpp");
        }

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException), "'/std:latest' is not supported. This test should throw an exception.")]
        public void unsupported_std_version()
        {
            MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                compiler,
                new CFamilyHelper.Capture()
                {
                    Executable = "",
                    Cwd = "",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                    "cl.exe",
                    "/std:latest",
                    "/J"
                    },
                }
            });
        }

        [TestMethod]
        public void Compatibility_With_SonarLint()
        {
            Request req = MsvcDriver.ToRequest(new CFamilyHelper.Capture[] {
                new CFamilyHelper.Capture()
                {
                    Executable = "cl.exe",
                    // stdout is empty and stderr contains only toolset version and platform name:
                    StdOut = "",
                    CompilerVersion = "19.10.00",
                    X64=false
                },
                new CFamilyHelper.Capture()
                {
                    Executable = "cl.exe",
                    Cwd = "foo/bar",
                    Env = new List<string>(),
                    Cmd = new List<string>() {
                      "cl.exe",
                      // Note that in reality it will be absolute path
                      "test.cpp"
                    },
                }
            });
            req.File.Should().Be("foo/bar/test.cpp");
            req.Predefines.Should().Contain("#define _MSC_FULL_VER 191000\n");
            req.MsVersion.Should().Be(191000000);
            req.TargetTriple.Should().Be("i686-pc-windows");
        }

        [TestMethod]
        public void AbsolutePathConversion()
        {
            // 1. Only whitespace -> null
            MsvcDriver.Absolute("root", "\r\t\n   ")
                .Should().BeNull();

            // 2. Relative path with leading and trailing whitespce
            MsvcDriver.Absolute("root", "\r\t\nsubdir1/subDir2\\xxx.foo")
                    .Should().Be("root/subdir1/subDir2/xxx.foo");

            // 3. Absolute path with leading and trailing whitespace
            MsvcDriver.Absolute("root", "\r\t\nx:\\subdir1/subDir2\\xxx.foo")
                    .Should().Be("x:\\subdir1/subDir2\\xxx.foo");
        }
    }
}
