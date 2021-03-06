using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace NPS
{
    public partial class NPSBrowser : Form
    {

        List<Item> currentDatabase = new List<Item>();
        List<Item> gamesDbs = new List<Item>();
        List<Item> dlcsDbs = new List<Item>();
        HashSet<string> regions = new HashSet<string>();
        int currentOrderColumn = 0;
        bool currentOrderInverted = false;

        List<DownloadWorker> downloads = new List<DownloadWorker>();

        public NPSBrowser()
        {
            InitializeComponent();
            new Settings();

            if (string.IsNullOrEmpty(Settings.instance.GamesUri) && string.IsNullOrEmpty(Settings.instance.DLCUri))
            {
                MessageBox.Show("Application did not provide any links to external files or decrypt mechanism.\r\nYou need to specify tsv (tab splitted text) file with your personal links to pkg files on your own.\r\n\r\nFormat: TitleId Region Name Pkg Key", "Disclaimer!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Options o = new Options();
                o.ShowDialog();
            }
            else if (!File.Exists(Settings.instance.pkgPath))
            {
                MessageBox.Show("You are missing your pkg_dec.exe", "Whops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Options o = new Options();
                o.ShowDialog();
            }

        }



        /// <summary>
        /// Exit Application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Aboutbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>



        private void NoPayStationBrowser_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.instance.GamesUri))
                gamesDbs = LoadDatabase(Settings.instance.GamesUri);
            else radioButton1.Enabled = false;

            if (!string.IsNullOrEmpty(Settings.instance.DLCUri))
                dlcsDbs = LoadDatabase(Settings.instance.DLCUri);
            else radioButton2.Enabled = false;


            currentDatabase = gamesDbs;

            comboBox1.Items.Add("ALL");
            comboBox1.Text = "ALL";

            //RefreshList(currentDatabase);
            if (Settings.instance.records != 0)
            {
                var _new = gamesDbs.Count - Settings.instance.records;
                if (_new > 0)
                    label1.Text += " (" + _new.ToString() + " new since last launch)";
            }

            Settings.instance.records = gamesDbs.Count;

            foreach (string s in regions)
                comboBox1.Items.Add(s);
        }

        List<Item> LoadDatabase(string path)
        {
            path = new Uri(path).ToString();
            List<Item> dbs = new List<Item>();
            try
            {
                WebClient wc = new WebClient();
                string content = wc.DownloadString(new Uri(path));
                wc.Dispose();
                content = Encoding.UTF8.GetString(Encoding.Default.GetBytes(content));

                string[] lines = content.Split(new string[] { "\r\n", "\n\r", "\n", "\r" }, StringSplitOptions.None);

                for (int i = 1; i < lines.Length; i++)
                {
                    var a = lines[i].Split('\t');
                    var itm = new Item(a[0], a[1], a[2], a[3], a[4]);
                    if (!itm.zRfi.ToLower().Contains("missing") && itm.pkg.ToLower().Contains("http://"))
                    {
                        dbs.Add(itm);
                        regions.Add(itm.Region.Replace(" ", ""));
                    }
                }

                dbs = dbs.OrderBy(i => i.TitleName).ToList();
            }
            catch (Exception err)
            {

            }
            return dbs;
        }


        void RefreshList(List<Item> items)
        {
            label1.Text = items.Count + " items";
            listView1.Items.Clear();
            foreach (var item in items)
            {
                var a = new ListViewItem(item.TitleId);
                a.SubItems.Add(item.Region);
                a.SubItems.Add(item.TitleName);
                a.Tag = item;

                listView1.Items.Add(a);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            List<Item> itms = new List<Item>();

            foreach (var item in currentDatabase)
            {
                if (item.CompareName(textBox1.Text) && (comboBox1.Text == "ALL" || item.Region.Contains(comboBox1.Text)))
                    itms.Add(item);
            }

            RefreshList(itms);
        }


        private void button1_Click(object sender, EventArgs e)
        {


            if (string.IsNullOrEmpty(Settings.instance.downloadDir) || string.IsNullOrEmpty(Settings.instance.pkgPath))
            {
                MessageBox.Show("You don't have proper config", "Whops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Options o = new Options();
                o.ShowDialog();
                return;
            }

            if (!File.Exists(Settings.instance.pkgPath))
            {
                MessageBox.Show("You missing your pkg dec", "Whops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Options o = new Options();
                o.ShowDialog();
                return;
            }

            if (listView1.SelectedItems.Count == 0) return;
            var a = (listView1.SelectedItems[0].Tag as Item);


            foreach (var d in downloads)
                if (d.currentDownload == a)
                    return; //already downloading


            DownloadWorker dw = new DownloadWorker(a);
            listViewEx1.Items.Add(dw.lvi);
            listViewEx1.AddEmbeddedControl(dw.progress, 3, listViewEx1.Items.Count - 1);
            downloads.Add(dw);

        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Options o = new Options();
            o.ShowDialog();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                currentDatabase = dlcsDbs;
                textBox1_TextChanged(null, null);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                currentDatabase = gamesDbs;
                textBox1_TextChanged(null, null);
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            Item itm = (listView1.SelectedItems[0].Tag as Item);
            lbl_query_size.Text = "0 MB";

            Helpers.Renascene r = new Helpers.Renascene(itm.TitleId);

            if (r.imgUrl != null)
            {
                pictureBox1.LoadAsync(r.imgUrl);
                label5.Text = r.ToString();
            }
            else
            {
                pictureBox1.Image = null;
                label5.Text = "";
            }


        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0) return;
            (listViewEx1.SelectedItems[0].Tag as DownloadWorker).Cancel();
            (listViewEx1.SelectedItems[0].Tag as DownloadWorker).DeletePkg();

        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0) return;
            (listViewEx1.SelectedItems[0].Tag as DownloadWorker).DeletePkg();

        }

        private void retryUnpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0) return;

            (listViewEx1.SelectedItems[0].Tag as DownloadWorker).Unpack();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int workingThreads = 0;
            foreach (var dw in downloads)
            {
                if (!dw.isCompleted && dw.isRunning)
                    workingThreads++;
            }

            if (workingThreads < 2)
            {
                foreach (var dw in downloads)
                {
                    if (!dw.isCompleted && !dw.isRunning && !dw.isCanceled)
                    {
                        dw.Start();
                        break;
                    }
                }
            }
        }

        private void clearCompletedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<DownloadWorker> toDel = new List<DownloadWorker>();
            List<ListViewItem> toDelLVI = new List<ListViewItem>();

            foreach (var i in downloads)
            {
                if (i.isCompleted || i.isCanceled)
                {
                    toDel.Add(i);
                }
            }


            foreach (ListViewItem i in listViewEx1.Items)
            {
                if (toDel.Contains(i.Tag as DownloadWorker))
                    toDelLVI.Add(i);

            }

            foreach (var i in toDel)
                downloads.Remove(i);
            toDel.Clear();

            foreach (var i in toDelLVI)
                listViewEx1.Items.Remove(i);
            toDelLVI.Clear();


        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox1_TextChanged(null, null);
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (currentOrderColumn == e.Column) { currentOrderInverted = !currentOrderInverted; }
            else
            {
                currentOrderColumn = e.Column; currentOrderInverted = false;
            }

            this.listView1.ListViewItemSorter = new ListViewItemComparer(currentOrderColumn, currentOrderInverted);
            // Call the sort method to manually sort.
            listView1.Sort();
        }

        private void btn_query_size_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            var a = (listView1.SelectedItems[0].Tag as Item);

            var webRequest = HttpWebRequest.Create(a.pkg);
            webRequest.Method = "HEAD";

            using (var webResponse = webRequest.GetResponse())
            {
                var fileSize = webResponse.Headers.Get("Content-Length");
                var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / 1024.0 / 1024.0, 2);
                lbl_query_size.Text = fileSizeInMegaByte + " MB";
            }

        }
    }

    class ListViewItemComparer : IComparer
    {
        private int col;
        private bool invertOrder = false;
        public ListViewItemComparer()
        {
            col = 0;
        }
        public ListViewItemComparer(int column, bool invertedOrder)
        {
            col = column;
            invertOrder = invertedOrder;

        }
        public int Compare(object x, object y)
        {
            int returnVal = -1;
            if (!invertOrder)
            {
                returnVal = String.Compare(((ListViewItem)x).SubItems[col].Text, ((ListViewItem)y).SubItems[col].Text);
            }
            else
            {
                returnVal = String.Compare(((ListViewItem)y).SubItems[col].Text, ((ListViewItem)x).SubItems[col].Text);
            }
            return returnVal;
        }
    }


}
