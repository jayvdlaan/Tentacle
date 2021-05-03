using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;

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
            foreach(var Entry in m_Entries)
                TotalPages += Entry.Page.GetPageCount();

            List<int> RemovalQueue = new List<int>();
            int Index = 0;
            float Progress = 0;
            float ProgressIncrement = 100 / TotalPages;
            AdvanceProgress(Progress);
            foreach (var Entry in m_Entries)
            {
                if (DownloadEntry(Entry, ProgressIncrement))
                {
                    Progress += ProgressIncrement;
                    AdvanceProgress(Progress);
                }
                else
                {
                    MessageBox.Show("Something went wrong when downloading entry with code: " + Entry.MagicCode.ToString());
                    break;
                }

                RemovalQueue.Add(Index);
                Index++;
            }

            foreach(var CurrIndex in RemovalQueue.Reverse<int>())
                RemoveEntry(CurrIndex);
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
                m_Form.m_ListView.Items.Add(new ListViewItem(ListEntry));
            }
        }

        public void RemoveEntry(int a_Index)
        {
            m_Entries.RemoveAt(a_Index);
            m_Form.m_ListView.Items.RemoveAt(a_Index);
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

        public void AdvanceProgress(float a_ProgressPercentage)
        {
            if (a_ProgressPercentage > 100f)
                a_ProgressPercentage = 100f;

            int Percentage = (int)Math.Abs(a_ProgressPercentage);
            m_Form.m_ProgressBar.Value = Percentage;
            m_Form.m_ProgressLabel.Text = Percentage + "%";
        }

        private bool DownloadEntry(TentacleEntry a_Entry, float a_IncrementValue)
        {
            a_IncrementValue = a_IncrementValue / 100f;
            var Client = new WebClient();
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

                Client.DownloadFile(ImageLink, FullFilePath);
                Thread.Sleep(500);
            }

            return true;
        }

        private List<TentacleEntry> m_Entries;
        private string m_DownloadDir;
        private MainForm m_Form;
    }
}
