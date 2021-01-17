using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            InitBrowser();
        }

        public ChromiumWebBrowser browser;
        private bool _browserIsLoading;
        private string _browserAddress;
        private string _browserTitle;
        private string _browserSource;

        public void InitBrowser()
        {
            Cef.Initialize(new CefSettings());
            browser = new ChromiumWebBrowser("" /* "www.google.com" */);
            panel1.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;

            browser.LoadingStateChanged += Browser_LoadingStateChanged;
            browser.TitleChanged += Browser_TitleChanged;
            browser.AddressChanged += Browser_AddressChanged;
        }

        private void Browser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            this.Invoke(new Action(BrowserAddressChanged));
        }

        private void BrowserAddressChanged()
        {
            _browserAddress = browser.Address;
        }

        private void Browser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            _browserTitle = e.Title;
            this.Invoke(new Action(BrowserTitleChanged));
        }

        void BrowserTitleChanged()
        {
            browserTitle.Text = _browserTitle;
        }

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            _browserIsLoading = browser.IsLoading;
            this.Invoke(new Action(LoadingStateChanged));
        }

        void LoadingStateChanged()
        {
            Log("Load state changed: " + (_browserIsLoading ? "Loading" : "Complete"));
            if (!_browserIsLoading)
                LoadingComplete();
        }

        private void LoadingComplete()
        {
            Task.Run(new Action(BlockingGetSource));
        }

        private void BlockingGetSource()
        {
            var t = browser.GetSourceAsync();
            t.Wait();
            _browserSource = t.Result;
            this.Invoke(new Action(SourceComplete));
        }

        void SourceComplete()
        {
            txtSource.Text = _browserSource;
            Log("Source complete");

            var hd = new HtmlAgilityPack.HtmlDocument();
            hd.LoadHtml(_browserSource);

            // get A links...
            lvLinks.Items.Clear();
            var aNodes = hd.DocumentNode.SelectNodes("//a");
            if (aNodes != null)
            {
                foreach (var n in aNodes)
                {
                    string r = n.GetAttributeValue("href", null);
                    string t = n.InnerText;
                    lvLinks.Items.Add(new ListViewItem( new [] { r, t }));
                }
                Log("Source Links: found " + lvLinks.Items.Count);
            }
            else
            {
                Log("Source Links: none found");
            }

            // reformat
            txtSourceFormatted.Text = WriteHtmlDocument(hd);
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            LoadUrl(cbUrl.Text);
        }

        void LoadUrl(string url)
        {
            Log("Loading URL: " + url);
            browser.Load(url);
        }

        void Log(string logMsg)
        {
            lbLog.Items.Add(logMsg);
        }


        string WriteHtmlDocument(HtmlAgilityPack.HtmlDocument doc)
        {
            using (var writer = new StringWriter())
            {
                WriteHtmlNode(writer, doc.DocumentNode, 0);
                return writer.GetStringBuilder().ToString();
            }
        }

        void WriteHtmlNode(StringWriter file, HtmlAgilityPack.HtmlNode node, int indentLevel)
        {
            // check parameter
            if (file == null) return;
            if (node == null) return;

            // init 
            string INDENT = " ";
            string NEW_LINE = System.Environment.NewLine;

            // case: no children
            if (node.HasChildNodes == false)
            {
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);
                file.Write(node.OuterHtml);
                file.Write(NEW_LINE);
            }

            // case: node has childs
            else
            {
                // indent
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);

                // open tag
                file.Write(string.Format("<{0} ", node.Name));
                if (node.HasAttributes)
                    foreach (var attr in node.Attributes)
                        file.Write(string.Format("{0}=\"{1}\" ", attr.Name, attr.Value));
                file.Write(string.Format(">{0}", NEW_LINE));

                // childs
                foreach (var chldNode in node.ChildNodes)
                    WriteHtmlNode(file, chldNode, indentLevel + 1);

                // close tag
                for (int i = 0; i < indentLevel; i++)
                    file.Write(INDENT);
                file.Write(string.Format("</{0}>{1}", node.Name, NEW_LINE));
            }
        }

        private void lvLinks_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lvLinks_DoubleClick(object sender, EventArgs e)
        {
            var newUrl = lvLinks.SelectedItems[0].Text;
            LoadUrl(newUrl);
        }
    }
}
