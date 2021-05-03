using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;

namespace Tentacle
{
    class TentaclePage
    {
        public TentaclePage(string a_URL)
        {
            m_URL = a_URL;
            m_Client = new HtmlWeb();
        }

        public bool Load()
        {
            m_MainPageContent = m_Client.Load(m_URL);
            return m_Client.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public string GetURL()
        {
            return m_URL;
        }

        public string GetPrettyTitle()
        {
            var Nodes = GetPageInfo();
            HtmlNode TargetNode = FindInfoNode("title");
            if (TargetNode == null)
            {
                MessageBox.Show("Couldn't find title.");
                return "";
            }

            foreach (var CurrNode in TargetNode.ChildNodes)
            {
                if (CurrNode.Attributes.Count > 0 && CurrNode.Attributes[0].Value == "pretty")
                    return HtmlEntity.DeEntitize(CurrNode.InnerHtml);
            }

            return "";
        }

        public int GetPageCount()
        {
            var Nodes = GetPageInfo();
            HtmlNode TargetNode = FindInfoNode("tags");
            if (TargetNode == null)
            {
                MessageBox.Show("Couldn't find page count.");
                return 0;
            }

            foreach (var CurrNode in TargetNode.ChildNodes)
            {
                if (CurrNode.FirstChild.InnerHtml.Contains("Pages:"))
                {
                    var Text = HtmlEntity.DeEntitize(CurrNode.ChildNodes[1].FirstChild.FirstChild.InnerHtml);
                    int.TryParse(Text, out int Count);
                    return Count;
                }
            }

            return 0;
        }

        public string GetImageLink(int a_PageIndex)
        {
            if (a_PageIndex > GetPageCount())
                return "";

            var Page = m_Client.Load(m_URL + "/" + a_PageIndex + "/");
            if (m_Client.StatusCode != System.Net.HttpStatusCode.OK)
                return "";

            var Node = Page.GetElementbyId("image-container").FirstChild.FirstChild;
            return Node.Attributes[0].Value;
        }

        private HtmlNodeCollection GetPageInfo()
        {
            return m_MainPageContent.GetElementbyId("info").ChildNodes;
        }

        private HtmlNode FindInfoNode(string a_Tag)
        {
            var Nodes = GetPageInfo();
            HtmlNode TargetNode = null;
            foreach (var CurrNode in Nodes)
            {
                bool HasEntry = false;
                var Attributes = CurrNode.Attributes;
                foreach (var CurrAttrib in Attributes)
                {
                    if (CurrAttrib.Value == a_Tag)
                    {
                        HasEntry = true;
                        break;
                    }
                }

                if (HasEntry)
                {
                    TargetNode = CurrNode;
                    break;
                }
            }

            return TargetNode;
        }

        private string m_URL;
        private HtmlAgilityPack.HtmlDocument m_MainPageContent;
        private HtmlWeb m_Client;
    }
}
