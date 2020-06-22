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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


/**
 * This is a port of the Java MsvcDriver
 * https://github.com/SonarSource/sonar-cpp/blob/master/sonar-cfamily-plugin/src/main/java/com/sonar/cpp/analyzer/MsvcDriver.java
 */
namespace SonarLint.VisualStudio.Integration.Vsix.CFamily
{

    internal static class MsvcDriver
    {
        public static Request ToRequest(CFamilyHelper.Capture[] captures)
        {
            Request request = new Request();
            Dictionary<string, Probe> probes = new Dictionary<string, Probe>();

            foreach (CFamilyHelper.Capture capture in captures)
            {
                if (capture.CompilerVersion != null)
                {
                    probes.Add(capture.Executable, new Probe(capture.CompilerVersion, capture.X64));
                    continue;
                }

                ArgumentCursor args = new ArgumentCursor(capture.Cmd);

                List<string> includeDirs = new List<string>();
                StringBuilder predefines = new StringBuilder();
                string arch = "";
                StringBuilder includes = new StringBuilder();
                List<string> files = new List<string>();
                bool? cpp = null;
                string std = "c++14";
                bool ignoreStandardIncludePaths = false;
                bool charIsUnsigned = false;
                bool enableMsExtensions = true;
                bool operatorNames = false;

                while (args.Index < capture.Cmd.Count)
                {
                    string arg = capture.Cmd[args.Index];
                    if (arg.StartsWith("-"))
                    {
                        arg = "/" + arg.JavaSubstring(1, arg.Length);
                    }

                    if (arg.StartsWith("/I"))
                    {
                        string dirStr = args.readPrefix("/I");
                        var absolutePath = Absolute(capture.Cwd, dirStr);
                        if (absolutePath != null)
                        {
                            includeDirs.Add(absolutePath);
                        }
                    }
                    else if ("/X" == arg)
                    {
                        ignoreStandardIncludePaths = true;
                        args.Index++;

                    }
                    else if (arg.StartsWith("/D"))
                    {
                        string defineStr = args.readPrefix("/D");
                        defineStr = defineStr.Replace("\"", "");

                        if (defineStr.Contains("="))
                        {
                            defineStr = defineStr.ReplaceFirst("=", " ");
                        }
                        else if (defineStr.Contains("#"))
                        {
                            defineStr = defineStr.ReplaceFirst("#", " ");
                        }
                        else
                        {
                            defineStr += " 1";
                        }
                        predefines.Append("#define ").Append(defineStr).Append('\n');

                    }
                    else if (arg.StartsWith("/U"))
                    {
                        string undefStr = args.readPrefix("/U");
                        undefStr = undefStr.Replace("\"", "");

                        predefines.Append("#undef ").Append(undefStr).Append('\n');

                    }
                    else if (arg.StartsWith("/FI"))
                    {
                        string includeStr = args.readPrefix("/FI");
                        if (!includeStr.StartsWith("\""))
                        {
                            includeStr = "\"" + includeStr + "\"";
                        }

                        includes.Append("#include ").Append(includeStr).Append('\n');

                    }
                    else if (arg.StartsWith("/ZW") || arg.StartsWith("/clr"))
                    {
                        // C++/CX or C++/CLI
                        files.Clear();
                        break;

                    }
                    else if (arg.StartsWith("/MT"))
                    {
                        if (arg.EndsWith("d"))
                        {
                            predefines.Append("#define _DEBUG 1\n");
                        }
                        else
                        {
                            predefines.Append("#undef _DEBUG\n");
                        }
                        predefines.Append("#undef _DLL\n");
                        args.Index++;

                    }
                    else if (arg.StartsWith("/MD"))
                    {
                        if (arg.EndsWith("d"))
                        {
                            predefines.Append("#define _DEBUG 1\n");
                        }
                        else
                        {
                            predefines.Append("#undef _DEBUG\n");
                        }
                        predefines.Append("#define _DLL 1\n");
                        args.Index++;

                    }
                    else if (arg.StartsWith("/LD"))
                    {
                        // TODO(Godin): why non consistent with /MD and /MT ?
                        if (arg.EndsWith("d"))
                        {
                            predefines.Append("#define _DEBUG 1\n");
                        }
                        args.Index++;

                    }
                    else if ("/Zl" == arg)
                    {
                        predefines.Append("#define _VC_NODEFAULTLIB 1\n");
                        args.Index++;

                    }
                    else if ("/Za" == arg)
                    {
                        enableMsExtensions = false;
                        operatorNames = true;
                        args.Index++;

                    }
                    else if (arg.StartsWith("/Zc"))
                    {
                        // TODO should be handled, skipped for now
                        args.Index++;

                    }
                    else if ("/J" == arg)
                    {
                        charIsUnsigned = true;
                        args.Index++;

                    }
                    else if (arg.StartsWith("/arch"))
                    {
                        arch = args.readPrefix("/arch:");

                    }
                    else if (arg.StartsWith("/GR"))
                    {
                        // TODO should be handled, skipped for now
                        args.Index++;

                    }
                    else if (arg.StartsWith("/EH"))
                    {
                        // TODO should be handled, skipped for now
                        args.Index++;

                    }
                    else if (arg.StartsWith("/RTC") || arg.StartsWith("/GZ"))
                    {
                        predefines.Append("#define __MSVC_RUNTIME_CHECKS 1\n");
                        args.Index++;

                    }
                    else if ("/TP" == arg)
                    {
                        // all files are CPP
                        cpp = true;
                        args.Index++;

                    }
                    else if ("/TC" == arg)
                    {
                        // all files are C
                        cpp = false;
                        args.Index++;

                    }
                    else if (arg.StartsWith("/Tc"))
                    {
                        // TODO completely skipped for now - used in ReactOS only for compilation of resources
                        files.Clear();
                        break;

                    }
                    else if (arg.StartsWith("/Tp"))
                    {
                        throw new InvalidOperationException(arg);

                    }
                    else if (arg == "/Yc" || arg == "/Yu")
                    {
                        // skip
                        args.Index++;

                    }
                    else if (arg.StartsWith("/Yc") || arg.StartsWith("/Yu"))
                    {
                        // read name of precompiled header
                        args.readPrefix("/Yc");

                    }
                    else if (arg.StartsWith("/std:"))
                    {
                        std = args.readPrefix("/std:");

                    }
                    else if (arg.StartsWith("/"))
                    {
                        // skip
                        args.Index++;

                    }
                    else if (arg.StartsWith("@"))
                    {
                        throw new InvalidOperationException("unexpanded response file");

                    }
                    else
                    {
                        var relativeOrAbsolutePath = arg.Replace("\"", "");
                        string file = Absolute(capture.Cwd, relativeOrAbsolutePath);
                        if (file != null)
                        {
                            files.Add(file);
                        }
                        args.Index++;
                    }
                }

                if (cpp == null)
                {
                    cpp = files.All(f => !f.EndsWith(".c"));
                }

                if (!ignoreStandardIncludePaths)
                {
                    foreach (string env in capture.Env
                        .Where(env => env.StartsWith("INCLUDE=")))
                    {
                        foreach (string dirStr in env.Substring("INCLUDE=".Length).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var path = Absolute(capture.Cwd, dirStr);
                            if (path != null)
                            {
                                includeDirs.Add(path);
                            }
                        }
                    }
                }

                Probe probe;
                probes.TryGetValue(capture.Executable, out probe);

                predefines.Append("#define _MSC_FULL_VER ").Append(probe.Major).Append(probe.Minor).Append(probe.Micro).Append("\n");
                predefines.Append("#define _MSC_VER ").Append(probe.Major).Append(probe.Minor).Append("\n");
                // TODO
                predefines.Append("#define _MSC_BUILD 0\n");

                predefines.Append("#define _MSC_EXTENSIONS 1\n");
                predefines.Append("#define _INTEGRAL_MAX_BITS 64\n");
                predefines.Append("#define _WIN32 1\n");

                predefines.Append("#define _MT 1\n");
                predefines.Append("#define __BOOL_DEFINED 1\n");

                if (probe.Isx64)
                {
                    predefines.Append("#define _WIN64 1\n");
                    predefines.Append("#define _M_X64 100\n");
                    predefines.Append("#define _M_AMD64 100\n");
                }
                else
                {
                    predefines.Append("#define _M_IX86 600\n");
                }

                switch (arch)
                {
                    // Note that "IA32", "SSE" and "SSE2" are available only for x86, but we don't need to verify correctness
                    default:
                        throw new InvalidOperationException("/arch:" + arch);
                    case "IA32":
                        predefines.Append("#define _M_IX86_FP 0\n");
                        break;
                    case "SSE":
                        predefines.Append("#define _M_IX86_FP 1\n");
                        break;
                    case "AVX2":
                        predefines.Append("#define __AVX2__ 1\n");
                        // fallthrough
                        goto case "AVX";
                    case "AVX":
                        predefines.Append("#define __AVX__ 1\n");
                        // fallthrough
                        goto case "SSE2";
                    case "SSE2":
                    case "":
                        if (!probe.Isx64)
                        {
                            predefines.Append("#define _M_IX86_FP 2\n");
                        }
                        break;
                }

                int major = Int32.Parse(probe.Major);
                int minor = Int32.Parse(probe.Minor);
                int micro = Int32.Parse(probe.Micro);

                if (enableMsExtensions)
                {
                    request.Flags = Request.MS;
                }
                if (cpp.Value)
                {
                    predefines.Append("#define _CPPRTTI 1\n");
                    predefines.Append("#define _NATIVE_NULLPTR_SUPPORTED 1\n");
                    predefines.Append("#define _WCHAR_T_DEFINED 1\n");
                    predefines.Append("#define _NATIVE_WCHAR_T_DEFINED 1\n");
                    request.Flags |= Request.CPlusPlus;
                    if (major < 16)
                    {
                        predefines.Append("#define __cplusplus 199711L\n");
                    }
                    else if (major < 19)
                    {
                        request.Flags |= Request.CPlusPlus11;
                        predefines.Append("#define __cplusplus 201103L\n");
                    }
                    else
                    {
                        // if major > 19 we rely on /std.  By default, /std:c++14 is specified
                        predefines.Append("#define _HAS_CHAR16_T_LANGUAGE_SUPPORT 1\n");
                        if (minor > 23)
                        {
                            predefines.Append("#define _HAS_CONDITIONAL_EXPLICIT 0\n");
                        }
                        switch (std)
                        {
                            default:
                                throw new InvalidOperationException("/std:" + std + " is not supported.");
                            case "c++14":
                                request.Flags |= Request.CPlusPlus11 | Request.CPlusPlus14;
                                predefines.Append("#define __cplusplus 201402L\n");
                                predefines.Append("#define _MSVC_LANG 201402L\n");
                                break;
                            case "c++17":
                                request.Flags |= Request.CPlusPlus11 | Request.CPlusPlus14 | Request.CPlusPlus17;
                                predefines.Append("#define __cplusplus 201703L\n");
                                predefines.Append("#define _MSVC_LANG 201703L\n");
                                break;
                        }
                    }
                    /*
                     * https://github.com/SonarSource/clang/blob/v6.0.1/lib/Frontend/InitPreprocessor.cpp#L411
                     * https://github.com/SonarSource/clang/blob/v6.0.1/test/Preprocessor/init.c#L9921
                     * https://github.com/SonarSource/clang/blob/v6.0.1/test/Preprocessor/init.c#L9934
                     */
                    predefines.Append("#define __STDCPP_DEFAULT_NEW_ALIGNMENT__ ").Append(probe.Isx64 ? "16ULL" : "8U").Append("\n");
                    if (operatorNames)
                    {
                        request.Flags |= Request.OperatorNames;
                    }
                }
                else
                {
                    request.Flags |= Request.C99 | Request.C11;
                    predefines.Append("#define __STDC_VERSION__ 201112L\n");
                    if (!enableMsExtensions)
                    {
                        predefines.Append("#define __STDC__ 1\n");
                    }
                }
                if (charIsUnsigned)
                {
                    request.Flags |= Request.CharIsUnsigned;
                    predefines.Append("#define _CHAR_UNSIGNED 1\n");
                }
                request.Flags |= Request.SonarLint;
                request.MsVersion = major * 10000000 + minor * 100000 + micro;
                request.IncludeDirs = includeDirs.ToArray();
                request.FrameworkDirs = Array.Empty<string>();
                request.VfsOverlayFiles = Array.Empty<string>();
                request.Predefines = predefines.ToString()
                  // TODO can be passed directly to Clang as PreprocessorOptions.Includes
                  + includes.ToString();
                request.TargetTriple = probe.Isx64 ? "x86_64-pc-windows" : "i686-pc-windows";
                if (files.Count == 1)
                {
                    request.File = files[0];
                }
            }

            return request;
        }

        /**
         * @return given path if it is absolute, otherwise prepends given base path
         */
        internal static string Absolute(string basePath, string relativeOrAbsolutePath)
        {
            Debug.Assert(!string.IsNullOrEmpty(basePath), "basePath should not be empty");
            Debug.Assert(!string.IsNullOrEmpty(relativeOrAbsolutePath), "relativeOrAbsolutePath should not be empty");

            var cleanedPath = relativeOrAbsolutePath.Trim();

            Debug.Assert(!cleanedPath.Any(c => Path.GetInvalidPathChars().Contains(c)), "relativeOrAbsolutePath should not contain invalid path characters");

            if (string.IsNullOrEmpty(cleanedPath))
            {
                return null;
            }

            if (!System.IO.Path.IsPathRooted(cleanedPath))
            {
                return Path.Combine(basePath, cleanedPath).Replace('\\', '/');
            }
            return cleanedPath;
        }
    }

    internal class Probe
    {
        public string Major { get; }
        public string Minor { get; }
        public string Micro { get; }
        public string Build { get; }

        public bool Isx64 { get; }

        public Probe(string version, bool x64)
        {
            string[] c = version.Split('.');
            Major = c[0];
            Minor = c[1];
            Micro = c[2];
            Build = c.Length < 4 ? "" : c[3];
            Isx64 = x64;
        }

    }

    internal class ArgumentCursor
    {
        List<string> Arguments { get; }
        public int Index { get; set; } = 1;

        public ArgumentCursor(List<string> arguments)
        {
            Arguments = arguments;
        }

        public string readPrefix(string prefix)
        {
            string arg = Arguments[Index];
            Index++;
            if (arg.Length == prefix.Length)
            {
                arg = Arguments[Index];
                Index++;
            }
            else
            {
                arg = arg.JavaSubstring(prefix.Length, arg.Length);
            }
            return arg;
        }

    }

    public static class JavaExtensions
    {
        /**
         * In Java, String::substring takes endIndex as second argument
         */
        public static string JavaSubstring(this string str, int startIndex, int endIndex)
        {
            return str.Substring(startIndex, endIndex - startIndex);
        }

        public static string ReplaceFirst(this string original, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(original))
                return string.Empty;
            if (string.IsNullOrEmpty(oldValue))
                return original;
            if (string.IsNullOrEmpty(newValue))
                newValue = string.Empty;
            int loc = original.IndexOf(oldValue);
            return original.Remove(loc, oldValue.Length).Insert(loc, newValue);
        }
    }
}
