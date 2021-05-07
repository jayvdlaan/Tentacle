// Copyright (c) Teitoku42. All Rights Reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// TentacleEngine.cs is part of the Tentacle project.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Reflection;

namespace Tentacle
{
    struct TentacleEntry
    {
        public int MagicCode;
        public TentaclePage Page;
    }

    public class TentacleEngine
    {
        public TentacleEngine()
        {
            m_Form = new MainForm(this);
            m_Entries = new List<TentacleEntry>();
            Application.Run(m_Form);
        }

        public void DownloadEntries()
        {
            if (string.IsNullOrEmpty(m_DownloadDir))
            {
                MessageBox.Show("Please select a download directory first!");
                return;
            }

            int TotalPages = 0;
            foreach (var Entry in m_Entries)
                TotalPages += Entry.Page.GetPageCount();

            List<int> RemovalQueue = new List<int>();
            int Index = 0;
            m_DownloadProgress = 0f;
            m_DownloadProgressStep = 100f / TotalPages / 100f;
            AdvanceProgress();
            foreach (var Entry in m_Entries)
            {
                if (!DownloadEntry(Entry))
                {
                    MessageBox.Show("Something went wrong when downloading entry with code: " + Entry.MagicCode.ToString());
                    break;
                }

                RemovalQueue.Add(Index);
                Index++;
            }

            foreach (var CurrIndex in RemovalQueue.Reverse<int>())
                RemoveEntry(CurrIndex);

            m_DownloadProgress = 100f;
            AdvanceProgress();
        }

        public void AddTrendingEntries()
        {
            List<int> TrendingCodes;
            if (!TentaclePage.GetTrendingCodes(out TrendingCodes))
                return;

            m_DownloadProgress = 0f;
            m_DownloadProgressStep = 100f / 5f;
            foreach (var Code in TrendingCodes)
            { 
                AddEntry(Code);
                m_DownloadProgress += m_DownloadProgressStep;
                AdvanceProgress();
            }
        }

        public void AddEntry(int a_MagicCode)
        {
            TentacleEntry Entry;
            Entry.MagicCode = a_MagicCode;
            Entry.Page = new TentaclePage("https://nhentai.net/g/" + a_MagicCode + '/');
            if (!Entry.Page.Load())
                MessageBox.Show("Page with magic code: " + a_MagicCode + "could not be found.");
            else
            {
                m_Entries.Add(Entry);

                string[] ListEntry = { a_MagicCode.ToString(), Entry.Page.GetURL(), Entry.Page.GetPrettyTitle(), Entry.Page.GetPageCount().ToString() };
                InvokeOnForm(() => m_Form.m_ListView.Items.Add(new ListViewItem(ListEntry)));
            }
        }

        public void RemoveEntry(int a_Index)
        {
            m_Entries.RemoveAt(a_Index);
            InvokeOnForm(() => m_Form.m_ListView.Items.RemoveAt(a_Index));
        }

        public void PromptForDirectory()
        {
            FolderBrowserDialog Dialog = new FolderBrowserDialog();
            if (Dialog.ShowDialog() == DialogResult.OK)
            {
                m_DownloadDir = Dialog.SelectedPath;
                m_Form.m_DirectoryBox.Text = m_DownloadDir;
            }
        }

        public void AdvanceProgress()
        {
            if (m_DownloadProgress > 100f)
                m_DownloadProgress = 100f;

            int Percentage = (int)Math.Ceiling(m_DownloadProgress);
            InvokeOnForm(() => m_Form.m_ProgressBar.Value = Percentage);
            InvokeOnForm(() => m_Form.m_ProgressLabel.Text = Percentage + "%");
        }

        private bool DownloadEntry(TentacleEntry a_Entry)
        {
            var Client = new WebClient();
            Client.DownloadProgressChanged += PageDownloadProgressChanged;
            Client.DownloadFileCompleted += PageDownloadComplete;
            var DownloadDir = m_DownloadDir + '/' + a_Entry.MagicCode.ToString();
            try
            {
                Directory.CreateDirectory(DownloadDir);
            }
            catch (Exception E)
            {
                return false;
            }

            var PageCount = a_Entry.Page.GetPageCount();
            for (var Index = 1; Index < PageCount + 1; Index++)
            {
                var ImageLink = a_Entry.Page.GetImageLink(Index);
                if (ImageLink.Length == 0)
                    return false;

                var FileNamePos = ImageLink.LastIndexOf('/');
                var FullFilePath = DownloadDir + '/' + ImageLink.Substring(FileNamePos + 1);

                m_SubTaskProgress = 0;
                m_DownloadContinueLock = new Object();
                lock (m_DownloadContinueLock)
                {
                    Client.DownloadFileAsync(new Uri(ImageLink), FullFilePath);
                    Monitor.Wait(m_DownloadContinueLock);
                    Thread.Sleep(100);
                }
            }

            return true;
        }

        private void PageDownloadProgressChanged(object a_Sender, DownloadProgressChangedEventArgs a_Args)
        {
            int ProgressDelta = a_Args.ProgressPercentage - m_SubTaskProgress;
            m_SubTaskProgress = a_Args.ProgressPercentage;
            m_DownloadProgress += ProgressDelta * m_DownloadProgressStep;
            AdvanceProgress();
        }

        private void PageDownloadComplete(object a_Sender, System.ComponentModel.AsyncCompletedEventArgs a_Args)
        {
            lock (m_DownloadContinueLock)
            {
                Monitor.Pulse(m_DownloadContinueLock);
            }
        }

        private void InvokeOnForm(Action a_Delegate)
        {
            m_Form.Invoke(a_Delegate);
        }

        private List<TentacleEntry> m_Entries;
        private float m_DownloadProgress;
        private float m_DownloadProgressStep;
        private string m_DownloadDir;
        private object m_DownloadContinueLock;
        private int m_SubTaskProgress;
        private MainForm m_Form;
    }
}
