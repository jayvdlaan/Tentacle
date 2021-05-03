using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Tentacle
{
    public partial class MainForm : Form
    {
        public MainForm(TentacleEngine a_TentacleEngine)
        {
            InitializeComponent();
            m_TentacleEngine = a_TentacleEngine;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (MagicCodeInput.Text.Length == 0 || !int.TryParse(MagicCodeInput.Text, out int Result))
                return;

            m_TentacleEngine.AddEntry(Result);
            MagicCodeInput.Text = "";
        }
       
        private void PathButton_Click(object sender, EventArgs e)
        {
            m_TentacleEngine.PromptForDirectory();
        }
        
        private void DownloadButton_Click(object sender, EventArgs e)
        {
            //Thread Thread = new Thread(new ThreadStart(m_TentacleEngine.DownloadEntries));
            //Thread.IsBackground = true;
            //Thread.Start();
            m_TentacleEngine.DownloadEntries();
        }
      
        public ProgressBar m_ProgressBar
        {
            get { return progressBar1; }
        }

        public Label m_ProgressLabel
        {
            get { return ProgressLabel; }
        }

        public ListView m_ListView
        {
            get { return listView1; }
        }

        public TextBox m_DirectoryBox
        {
            get { return PathInput; }
        }

        private TentacleEngine m_TentacleEngine;
    }
}
