/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
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
            this.batchesStarted.Should().Be(expectedBatches, "Unexpected number of batches were started");
            this.batchesFinished.Should().Be(expectedBatches, "Unexpected number of batches were completed");
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
