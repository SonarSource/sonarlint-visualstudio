/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
 * Licensed under the GNU Lesser General Public License, version 3 or later.
 */

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SonarLint.VisualStudio.Smoke
{
    public static class VisualStudioAutomation
    {
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx context);

        public static object FindDte(int processId)
        {
            IRunningObjectTable table = null;
            IBindCtx context = null;
            IEnumMoniker enumerator = null;
            try
            {
                Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out table));
                Marshal.ThrowExceptionForHR(CreateBindCtx(0, out context));
                table.EnumRunning(out enumerator);
                var monikers = new IMoniker[1];
                while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    try
                    {
                        string name;
                        monikers[0].GetDisplayName(context, null, out name);
                        if (name.StartsWith("!VisualStudio.DTE.17.0:", StringComparison.Ordinal)
                            && name.EndsWith(":" + processId, StringComparison.Ordinal))
                        {
                            object dte;
                            table.GetObject(monikers[0], out dte);
                            return dte;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Other applications may publish inaccessible ROT entries.
                    }
                    catch (COMException)
                    {
                        // Entries can disappear while Visual Studio is starting.
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(monikers[0]);
                    }
                }
                return null;
            }
            finally
            {
                if (enumerator != null) Marshal.ReleaseComObject(enumerator);
                if (context != null) Marshal.ReleaseComObject(context);
                if (table != null) Marshal.ReleaseComObject(table);
            }
        }
    }
}
