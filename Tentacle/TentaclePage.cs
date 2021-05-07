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
// TentaclePage.cs is part of the Tentacle project.

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
       
        static public bool GetTrendingCodes(out List<int> a_TrendingCodes)
        {
            TentaclePage Page = new TentaclePage("https://nhentai.net/");
            Page.Load();
            return Page.GetTrending(out a_TrendingCodes);
        }

        private bool GetTrending(out List<int> a_TrendingCodes)
        {
            var Content = m_MainPageContent.GetElementbyId("content").ChildNodes;
            a_TrendingCodes = new List<int>();
            foreach (var Node in Content)
            {
                if (!Node.HasClass("index-popular"))
                    continue;

                var PopularNodes = Node.ChildNodes;
                foreach (var PopularNode in PopularNodes)
                {
                    if (!PopularNode.HasClass("gallery"))
                        continue;

                    var Attribs = PopularNode.ChildNodes[0].Attributes;
                    foreach (var Attrib in Attribs)
                    {
                        if (Attrib.Name != "href")
                            continue;

                        a_TrendingCodes.Add(int.Parse(Attrib.Value.Substring(3, Attrib.Value.Length - 4)));
                    }
                }

                return true;
            }

            return false;
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
