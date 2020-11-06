using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebScraper
{
    public partial class WebScraper : Form
    {
        #region Fields
        /// <summary>
        /// List of images found in html
        /// </summary>
        public static List<string> LastImagesCaptured;

        /// <summary>
        /// Last directory selected by user
        /// </summary>
        public static string LastFolderDirectory;

        /// <summary>
        /// HTTP Client to download images
        /// </summary>
        public static readonly HttpClient s_client = new HttpClient
        {
            MaxResponseContentBufferSize = 1_000_000
        };
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public WebScraper()
        {
            InitializeComponent();
        }

        #endregion

        #region Events
        /// <summary>
        /// Click event of extract button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnExtract_Click(object sender, EventArgs e)
        {
            await ExtractImagesAsync();
        }

        /// <summary>
        /// Click event of save images button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnSaveImages_Click(object sender, EventArgs e)
        {
            if (LastImagesCaptured != null && LastImagesCaptured.Count > 0)
            {
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.ShowNewFolderButton = true;
                // Show the FolderBrowserDialog.  
                DialogResult result = folderDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    LastFolderDirectory = folderDlg.SelectedPath;
                    Environment.SpecialFolder root = folderDlg.RootFolder;
                    await DownloadImagesAsync();
                }
            }
        }
        #endregion
        
        #region Methods
        /// <summary>
        /// Find image tags from the input html
        /// </summary>
        /// <param name="inputHTML">input html</param>
        /// <returns>List of urls of image tags</returns>
        public static IEnumerable<String> GetImageLinks(String inputHTML)
        {
            const string pattern = @"<img\b[^\<\>]+?\bsrc\s*=\s*[""'](?<L>.+?)[""'][^\<\>]*?\>";

            foreach (Match match in Regex.Matches(inputHTML, pattern, RegexOptions.IgnoreCase))
            {
                var imageLink = match.Groups["L"].Value;

                yield return imageLink;
            }
        }

        /// <summary>
        /// Download HTML and then extract images tags from html
        /// </summary>
        /// <returns></returns>
        public async Task ExtractImagesAsync()
        {
            try
            {
                string url = txtLink.Text;
                btnExtract.Text = "Extracting..";
                txtFoundLinks.Text = "";

                btnExtract.Enabled = false;
                HttpClient client = new HttpClient();
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpResponseMessage response = await client.GetAsync(url);
                HttpContent content = response.Content;
                string result = await content.ReadAsStringAsync();

                var lst = GetImageLinks(result);

                LastImagesCaptured = lst.ToList();

                for (int i = 0; i < LastImagesCaptured.Count; i++)
                {
                    if (!LastImagesCaptured[i].StartsWith("http"))
                    {
                        LastImagesCaptured[i] = url + LastImagesCaptured[i];
                    }
                }

                txtFoundLinks.Text = string.Join(Environment.NewLine, LastImagesCaptured);
                lblCount.Text = "Found: " + LastImagesCaptured.Count;
                btnExtract.Text = "Extract";
                btnExtract.Enabled = true;
            }
            catch (Exception ex)
            {
                lblCount.Text = "Found: " + 0;
                btnExtract.Text = "Extract";
                btnExtract.Enabled = true;
            }
        }

        /// <summary>
        /// This will create list of tasks and then will exxecute each one in async mode and will download images
        /// </summary>
        /// <returns></returns>
        public static async Task DownloadImagesAsync()
        {
            IEnumerable<Task> downloadTasksQuery =
                from url in LastImagesCaptured
                select ProcessUrlAsync(url, s_client);

            List<Task> downloadTasks = downloadTasksQuery.ToList();

            while (downloadTasks.Any())
            {
                Task finishedTask = await Task.WhenAny(downloadTasks);
                if (finishedTask.Exception != null)
                {
                    //Exception
                }

                downloadTasks.Remove(finishedTask);
            }

            MessageBox.Show("Downloading Completed.", "Web Scrapper");
        }

        /// <summary>
        /// This method will download individual image from its URL using http client
        /// </summary>
        /// <param name="url">url to download</param>
        /// <param name="client">http client</param>
        /// <returns></returns>
        static async Task ProcessUrlAsync(string url, HttpClient client)
        {
            byte[] content = await client.GetByteArrayAsync(url);
            Uri uri = new Uri(url);
            string filename = "";
            if (uri.IsFile)
                filename = System.IO.Path.GetFileName(uri.LocalPath);
            else
                filename = Guid.NewGuid() + (url.EndsWith(".png") ? ".png" : ".jpg");

            var fileFullPath = Path.Combine(LastFolderDirectory, filename);
            using (FileStream fs = new FileStream(fileFullPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                await fs.WriteAsync(content, 0, content.Length);
            }

        }
        #endregion

    }
}
