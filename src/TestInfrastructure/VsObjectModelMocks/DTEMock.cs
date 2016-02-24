//-----------------------------------------------------------------------
// <copyright file="DTEMock.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class DTEMock : DTE, DTE2
    {
        public DTEMock()
        {
            this.ToolWindows = new ToolWindowsMock(this);
        }

        #region DTE
        Document _DTE.ActiveDocument
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object _DTE.ActiveSolutionProjects
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Window _DTE.ActiveWindow
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        AddIns _DTE.AddIns
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE _DTE.Application
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        object _DTE.CommandBars
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.CommandLineArguments
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Commands _DTE.Commands
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ContextAttributes _DTE.ContextAttributes
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Debugger _DTE.Debugger
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        vsDisplay _DTE.DisplayMode
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        Documents _DTE.Documents
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE _DTE.DTE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.Edition
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Events _DTE.Events
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.FileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Find _DTE.Find
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.FullName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Globals _DTE.Globals
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ItemOperations _DTE.ItemOperations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int _DTE.LocaleID
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Macros _DTE.Macros
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        DTE _DTE.MacrosIDE
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Window _DTE.MainWindow
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        vsIDEMode _DTE.Mode
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ObjectExtenders _DTE.ObjectExtenders
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.RegistryRoot
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        SelectedItems _DTE.SelectedItems
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        Solution _DTE.Solution
        {
            get
            {
                return this.Solution;
            }
        }

        SourceControl _DTE.SourceControl
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        StatusBar _DTE.StatusBar
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _DTE.SuppressUI
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        UndoContext _DTE.UndoContext
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool _DTE.UserControl
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        string _DTE.Version
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        WindowConfigurations _DTE.WindowConfigurations
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        EnvDTE.Windows _DTE.Windows
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        void _DTE.ExecuteCommand(string CommandName, string CommandArgs)
        {
            throw new NotImplementedException();
        }

        object _DTE.GetObject(string Name)
        {
            throw new NotImplementedException();
        }

        bool _DTE.get_IsOpenFile(string ViewKind, string FileName)
        {
            throw new NotImplementedException();
        }

        Properties _DTE.get_Properties(string Category, string Page)
        {
            throw new NotImplementedException();
        }

        wizardResult _DTE.LaunchWizard(string VSZFile, ref object[] ContextParams)
        {
            throw new NotImplementedException();
        }

        Window _DTE.OpenFile(string ViewKind, string FileName)
        {
            throw new NotImplementedException();
        }

        void _DTE.Quit()
        {
            throw new NotImplementedException();
        }

        string _DTE.SatelliteDllPath(string Path, string Name)
        {
            throw new NotImplementedException();
        }

        void DTE2.Quit()
        {
            throw new NotImplementedException();
        }

        object DTE2.GetObject(string Name)
        {
            throw new NotImplementedException();
        }

        Window DTE2.OpenFile(string ViewKind, string FileName)
        {
            throw new NotImplementedException();
        }

        void DTE2.ExecuteCommand(string CommandName, string CommandArgs)
        {
            throw new NotImplementedException();
        }

        wizardResult DTE2.LaunchWizard(string VSZFile, ref object[] ContextParams)
        {
            throw new NotImplementedException();
        }

        string DTE2.SatelliteDllPath(string Path, string Name)
        {
            throw new NotImplementedException();
        }

        uint DTE2.GetThemeColor(vsThemeColors Element)
        {
            throw new NotImplementedException();
        }

        Properties DTE2.get_Properties(string Category, string Page)
        {
            throw new NotImplementedException();
        }

        bool DTE2.get_IsOpenFile(string ViewKind, string FileName)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region DTE2
        string DTE2.Name
        {
            get
            {
                return ((DTE)this).Name;
            }
        }

        string DTE2.FileName
        {
            get
            {
                return ((DTE)this).FileName;
            }
        }

        string DTE2.Version
        {
            get
            {
                return ((DTE)this).Version;
            }
        }

        object DTE2.CommandBars
        {
            get
            {
                return ((DTE)this).CommandBars;
            }
        }

        EnvDTE.Windows DTE2.Windows
        {
            get
            {
                return ((DTE)this).Windows;
            }
        }

        Events DTE2.Events
        {
            get
            {
                return ((DTE)this).Events;
            }
        }

        AddIns DTE2.AddIns
        {
            get
            {
                return ((DTE)this).AddIns;
            }
        }

        Window DTE2.MainWindow
        {
            get
            {
                return ((DTE)this).MainWindow;
            }
        }

        Window DTE2.ActiveWindow
        {
            get
            {
                return ((DTE)this).ActiveWindow;
            }
        }

        vsDisplay DTE2.DisplayMode
        {
            get
            {
                return ((DTE)this).DisplayMode;
            }

            set
            {
                ((DTE)this).DisplayMode = value;
            }
        }

        Solution DTE2.Solution
        {
            get
            {
                return ((DTE)this).Solution;
            }
        }

        Commands DTE2.Commands
        {
            get
            {
                return ((DTE)this).Commands;
            }
        }

        SelectedItems DTE2.SelectedItems
        {
            get
            {
                return ((DTE)this).SelectedItems;
            }
        }

        string DTE2.CommandLineArguments
        {
            get
            {
                return ((DTE)this).CommandLineArguments;
            }
        }

        DTE DTE2.DTE
        {
            get
            {
                return this;
            }
        }

        int DTE2.LocaleID
        {
            get
            {
                return ((DTE)this).LocaleID;
            }
        }

        WindowConfigurations DTE2.WindowConfigurations
        {
            get
            {
                return ((DTE)this).WindowConfigurations;
            }
        }

        Documents DTE2.Documents
        {
            get
            {
                return ((DTE)this).Documents;
            }
        }

        Document DTE2.ActiveDocument
        {
            get
            {
                return ((DTE)this).ActiveDocument;
            }
        }

        Globals DTE2.Globals
        {
            get
            {
                return ((DTE)this).Globals;
            }
        }

        StatusBar DTE2.StatusBar
        {
            get
            {
                return ((DTE)this).StatusBar;
            }
        }

        string DTE2.FullName
        {
            get
            {
                return ((DTE)this).FullName;
            }
        }

        bool DTE2.UserControl
        {
            get
            {
                return ((DTE)this).UserControl;
            }

            set
            {
                ((DTE)this).UserControl = value;
            }
        }

        ObjectExtenders DTE2.ObjectExtenders
        {
            get
            {
                return ((DTE)this).ObjectExtenders;
            }
        }

        Find DTE2.Find
        {
            get
            {
                return ((DTE)this).Find;
            }
        }

        vsIDEMode DTE2.Mode
        {
            get
            {
                return ((DTE)this).Mode;
            }
        }

        ItemOperations DTE2.ItemOperations
        {
            get
            {
                return ((DTE)this).ItemOperations;
            }
        }

        UndoContext DTE2.UndoContext
        {
            get
            {
                return ((DTE)this).UndoContext;
            }
        }

        Macros DTE2.Macros
        {
            get
            {
                return ((DTE)this).Macros;
            }
        }

        object DTE2.ActiveSolutionProjects
        {
            get
            {
                return ((DTE)this).ActiveSolutionProjects;
            }
        }

        DTE DTE2.MacrosIDE
        {
            get
            {
                return ((DTE)this).MacrosIDE;
            }
        }

        string DTE2.RegistryRoot
        {
            get
            {
                return ((DTE)this).RegistryRoot;
            }
        }

        DTE DTE2.Application
        {
            get
            {
                return ((DTE)this).Application;
            }
        }

        ContextAttributes DTE2.ContextAttributes
        {
            get
            {
                return ((DTE)this).ContextAttributes;
            }
        }

        SourceControl DTE2.SourceControl
        {
            get
            {
                return ((DTE)this).SourceControl;
            }
        }

        bool DTE2.SuppressUI
        {
            get
            {
                return ((DTE)this).SuppressUI;
            }

            set
            {
                ((DTE)this).SuppressUI = value;
            }
        }

        Debugger DTE2.Debugger
        {
            get
            {
                return ((DTE)this).Debugger;
            }
        }

        string DTE2.Edition
        {
            get
            {
                return ((DTE)this).Edition;
            }
        }

        ToolWindows DTE2.ToolWindows
        {
            get
            {
                return this.ToolWindows;
            }
        }
        #endregion

        #region Test helpers
        public SolutionMock Solution
        {
            get;
            set;
        }

        public ToolWindowsMock ToolWindows
        {
            get;
            set;
        }
        #endregion
    }
}
