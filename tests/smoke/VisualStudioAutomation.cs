/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
 * Licensed under the GNU Lesser General Public License, version 3 or later.
 */

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SonarLint.VisualStudio.Smoke
{
    public static class VisualStudioAutomation
    {
        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx context);

        public static ErrorListEntry[] ReadErrorList(object dte)
        {
            var errorList = ((EnvDTE80.DTE2)dte).ToolWindows.ErrorList;
            errorList.ShowErrors = true;
            errorList.ShowWarnings = true;
            errorList.ShowMessages = true;
            var items = errorList.ErrorItems;
            var result = new ErrorListEntry[items.Count];
            for (var index = 0; index < result.Length; index++)
            {
                var item = items.Item(index + 1);
                result[index] = new ErrorListEntry { File = item.FileName, Line = item.Line, Description = item.Description };
            }
            return result;
        }

        public static string ReadSonarOutput(object dte)
        {
            var panes = ((EnvDTE80.DTE2)dte).ToolWindows.OutputWindow.OutputWindowPanes;
            var result = new StringBuilder();
            for (var index = 1; index <= panes.Count; index++)
            {
                var pane = panes.Item(index);
                if (pane.Name.IndexOf("Sonar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.AppendLine(pane.Name);
                    var document = pane.TextDocument;
                    result.AppendLine(document.StartPoint.CreateEditPoint().GetText(document.EndPoint));
                }
            }
            return result.ToString();
        }

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

    public sealed class ErrorListEntry
    {
        public string File { get; set; }
        public int Line { get; set; }
        public string Description { get; set; }
    }
}
