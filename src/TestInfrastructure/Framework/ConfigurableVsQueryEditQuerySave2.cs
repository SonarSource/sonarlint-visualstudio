/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableVsQueryEditQuerySave2 : IVsQueryEditQuerySave2
    {
        private int batchesStarted;
        private int batchesFinished;
        private readonly List<string> editRequested = new List<string>();
        private readonly List<string> createRequested = new List<string>();

        #region IVsQueryEditQuerySave2

        int IVsQueryEditQuerySave2.BeginQuerySaveBatch()
        {
            this.batchesStarted++;
            return VSConstants.S_OK;
        }

        int IVsQueryEditQuerySave2.DeclareReloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.DeclareUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.EndQuerySaveBatch()
        {
            this.batchesFinished++;
            return VSConstants.S_OK;
        }

        int IVsQueryEditQuerySave2.IsReloadable(string pszMkDocument, out int pbResult)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.OnAfterSaveUnreloadableFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.QueryEditFiles(uint rgfQueryEdit, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pfEditVerdict, out uint prgfMoreInfo)
        {
            this.editRequested.AddRange(rgpszMkDocuments);
            pfEditVerdict = (uint)this.QueryEditFilesVerdict;
            prgfMoreInfo = (uint)this.QueryEditFilesMoreInfo;

            rgfQueryEdit.Should().Be(this.VerifyQueryEditFlags, "Unexpected flags: " + ((VsQueryEditFlags)rgfQueryEdit).ToString());

            return VSConstants.S_OK;
        }

        int IVsQueryEditQuerySave2.QuerySaveFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo, out uint pdwQSResult)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.QuerySaveFiles(uint rgfQuerySave, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
        {
            this.createRequested.AddRange(rgpszMkDocuments);
            pdwQSResult = (uint)(this.QuerySaveFilesVerification?.Invoke((VsQuerySaveFlags)rgfQuerySave) ?? tagVSQuerySaveResult.QSR_SaveOK);

            return VSConstants.S_OK;
        }

        #endregion IVsQueryEditQuerySave2

        public Func<VsQuerySaveFlags, tagVSQuerySaveResult> QuerySaveFilesVerification { get; set; }

        public tagVSQueryEditResult QueryEditFilesVerdict { get; set; } = tagVSQueryEditResult.QER_EditOK;

        public tagVSQueryEditResultFlags QueryEditFilesMoreInfo { get; set; }

        public uint VerifyQueryEditFlags { get; set; } = (uint)(VsQueryEditFlags.SilentMode | VsQueryEditFlags.DetectAnyChangedFile | VsQueryEditFlags.ForceEdit_NoPrompting);

        public void AssertEditRequested(params string[] expectedFiles)
        {
            CollectionAssert.AreEquivalent(expectedFiles, this.editRequested.ToArray(), "Actual: " + string.Join(", ", this.editRequested));
        }

        public void AssertNoEditRequested()
        {
            this.AssertEditRequested(new string[0]);
        }

        public void AssertCreateRequested(params string[] expectedFiles)
        {
            CollectionAssert.AreEquivalent(expectedFiles, this.createRequested.ToArray(), "Actual: " + string.Join(", ", this.createRequested));
        }

        public void AssertNoCreateRequested()
        {
            this.AssertCreateRequested(new string[0]);
        }

        public void AssertAllBatchesCompleted(int expectedBatches)
        {
            this.batchesStarted.Should().Be(expectedBatches);
            this.batchesFinished.Should().Be(expectedBatches);
        }

        public void Reset()
        {
            this.batchesFinished = 0;
            this.batchesStarted = 0;
            this.editRequested.Clear();
            this.createRequested.Clear();
        }
    }
}
