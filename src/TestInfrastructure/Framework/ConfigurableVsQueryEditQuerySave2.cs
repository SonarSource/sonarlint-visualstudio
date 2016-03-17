//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsQueryEditQuerySave2.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

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
            prgfMoreInfo = this.QueryEditFilesMoreInfo;

            Assert.AreEqual(this.VerifyQueryEditFlags, rgfQueryEdit, "Unexpected flags: " + ((VsQueryEditFlags)rgfQueryEdit).ToString());

            return VSConstants.S_OK;
        }

        int IVsQueryEditQuerySave2.QuerySaveFile(string pszMkDocument, uint rgf, VSQEQS_FILE_ATTRIBUTE_DATA[] pFileInfo, out uint pdwQSResult)
        {
            throw new NotImplementedException();
        }

        int IVsQueryEditQuerySave2.QuerySaveFiles(uint rgfQuerySave, int cFiles, string[] rgpszMkDocuments, uint[] rgrgf, VSQEQS_FILE_ATTRIBUTE_DATA[] rgFileInfo, out uint pdwQSResult)
        {
            this.createRequested.AddRange(rgpszMkDocuments);
            pdwQSResult = (uint) (this.QuerySaveFilesVerification?.Invoke((VsQuerySaveFlags)rgfQuerySave) ?? tagVSQuerySaveResult.QSR_SaveOK);

            return VSConstants.S_OK;
        }
        #endregion


        public Func<VsQuerySaveFlags, tagVSQuerySaveResult> QuerySaveFilesVerification { get; set; }

        public tagVSQueryEditResult QueryEditFilesVerdict { get; set; } = tagVSQueryEditResult.QER_EditOK;

        public uint QueryEditFilesMoreInfo { get; set; }

        public uint VerifyQueryEditFlags { get; set; } = (uint)(VsQueryEditFlags.SilentMode | VsQueryEditFlags.DetectAnyChangedFile);

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
            Assert.AreEqual(expectedBatches, this.batchesStarted, "Unexpected number of batches were started");
            Assert.AreEqual(expectedBatches, this.batchesFinished, "Unexpected number of batches were completed");
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
