using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Image = System.Windows.Controls.Image;

namespace MemePlates
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Globals
        
        string[] ACCEPTED_IMAGE_FORMATS = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };
        string[] ACCEPTED_VIDEO_FORMATS = { ".mp4" }; //"*.webm", 
        List<Image> uiImageElements = new List<Image>();
        List<MediaElement> uiVideoElements = new List<MediaElement>();
        List<WrapPanel> uiWrapPanels = new List<WrapPanel>();

        System.Windows.Forms.NotifyIcon ni;

        public string MEME_DIRECTORY_ROOT = "null";
        private int totalMemes = 0;
        private int totalSubDirs = 0;

        // Constructor
        public MainWindow()
        {
            InitializeComponent();

            InitialiseTrayIcon();

            // Load images
            LoadMemes();

            // Create custom cursor
            //this.Cursor = new Cursor(new MemoryStream(Properties.Resources.NormalSelect));
            tabMain.SelectedIndex = 1;

            wndMain.KeyDown += WndMain_KeyDown;
        }

        private void InitialiseTrayIcon()
        {
            ni = new System.Windows.Forms.NotifyIcon();

            // Tray Icon
            ni.Icon = new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/pepe.ico")).Stream);
            ni.Visible = true;
            ni.Click +=
                delegate (object sender, EventArgs args)
                {
                    System.Windows.Forms.MouseEventArgs mouseEvent = (System.Windows.Forms.MouseEventArgs)args;
                    if (mouseEvent.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        if (this.IsVisible)
                        {
                            this.Hide();
                            Console.WriteLine("Hiding");
                        }
                        else
                        {
                            this.Show();
                            wndMain.Activate();
                            sliderImageSize.Focus();
                            Console.WriteLine("Showing");
                        }
                    }

                };

            // Add context menu to tray icon
            ni.ContextMenu = BuildContextMenu();
        }

        private System.Windows.Forms.ContextMenu BuildContextMenu()
        {
            // Initialise Context menu
            System.Windows.Forms.ContextMenu cMenu = new System.Windows.Forms.ContextMenu();

            // Refresh
            System.Windows.Forms.MenuItem cmRefresh = new System.Windows.Forms.MenuItem();
            cmRefresh.Text = "Refresh";
            cmRefresh.Click += delegate (object sender, EventArgs args) { RefreshMemes(); };
            cMenu.MenuItems.Add(cmRefresh);

            // Shutdown
            System.Windows.Forms.MenuItem cmExit = new System.Windows.Forms.MenuItem();
            cmExit.Text = "Exit";
            cmExit.Click += CmExit_Click;
            cMenu.MenuItems.Add(cmExit);

            return cMenu;
        }

        private void CmExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private void WndMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageUp)
            {
                sliderImageSize.Value++;
            }
            if (e.Key == Key.PageDown)
            {
                sliderImageSize.Value--;
            }
            
        }

        private void LoadMemes()
        {
            // Initialise list of directories
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();

            // Get files
            MEME_DIRECTORY_ROOT = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Memes";
            dirs.Add(new DirectoryInfo(MEME_DIRECTORY_ROOT));

            // Check if folder initialisation is needed
            if (!Directory.Exists(MEME_DIRECTORY_ROOT))
            {
                
                Directory.CreateDirectory(MEME_DIRECTORY_ROOT);
                Directory.CreateDirectory(MEME_DIRECTORY_ROOT + "\\Reactions");
                Directory.CreateDirectory(MEME_DIRECTORY_ROOT + "\\Rename me 1");
                Directory.CreateDirectory(MEME_DIRECTORY_ROOT + "\\Rename me 2");
                File.Create(MEME_DIRECTORY_ROOT + "\\put memes here.txt");
                MessageBox.Show("A 'Memes' folder has been created in the same directory as this application.\n\nStore your memes in this folder, or it's sub-folders, to use them in MemePlates.", "Meme Folder Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Get sub-directories
            foreach (DirectoryInfo dir in dirs[0].GetDirectories())
            {
                dirs.Add(dir);
                
            }

            // For each found directory...
            foreach (DirectoryInfo dir in dirs)
            {
                // Create tab page and add to main tab control
                TabItem newTab = new TabItem();
                newTab.Header = dir.Name;
                newTab.Height = 50;
                newTab.Width = 250;
                tabMain.Items.Add(newTab);

                // Create wrap panel
                WrapPanel newPanel = new WrapPanel();
                newPanel.Orientation = Orientation.Horizontal;
                newPanel.ItemHeight = CalculateImageWidth();
                newPanel.ItemWidth = CalculateImageWidth();
                uiWrapPanels.Add(newPanel);

                // Add scroll viewer around wrap panel
                ScrollViewer newScroller = new ScrollViewer();
                newScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                newScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                newScroller.Height = double.NaN;
                newScroller.CanContentScroll = true;
                newScroller.Content = newPanel;
                newScroller.Focusable = true;
                newScroller.Focus();

                // Set tab contents
                newTab.Content = newScroller;

                totalSubDirs++;

                // Add all accepted image types
                foreach (string filetype in ACCEPTED_IMAGE_FORMATS)
                {
                    FileInfo[] files = dir.GetFiles(filetype);
                    foreach (FileInfo file in files)
                    {
                        newPanel.Children.Add(CreateImageElement(file));
                        totalMemes++;
                    }
                }

                // Add all accepted videos
                foreach (string filetype in ACCEPTED_VIDEO_FORMATS)
                {
                    FileInfo[] files = dir.GetFiles(filetype);
                    foreach (FileInfo file in files)
                    {
                        // Add UI element
                        newPanel.Children.Add(CreateVideoElement(file));

                        totalMemes++;
                    }
                }

            }

            // Update labels
            lblTotalMemeCount.Content = string.Format("{0} Memes", totalMemes.ToString());
            lblTotalSubDirsCount.Content = string.Format("{0} Folders", totalSubDirs.ToString());
        }

        private void RefreshMemes()
        {
            // Clear all UI elements
            uiImageElements.Clear();
            uiWrapPanels.Clear();
            tabMain.Items.Clear();

            // Reload memes
            LoadMemes();
        }

        //private Image CreateGIFElement(FileInfo file)
        //{
        //    // Create UI element from bitmap image source
        //    var gifMeme = new BitmapImage();
        //    gifMeme.BeginInit();
        //    gifMeme.UriSource = new Uri(file.FullName);
        //    gifMeme.EndInit();

        //    // Set animated source
        //    Image newImg = new Image();
        //    ImageBehavior.SetAnimatedSource(newImg, gifMeme);

        //    // Set size
        //    newImg.Width = CalculateImageWidth();
        //    newImg.Height = Double.NaN;

        //    // Add location to tag
        //    newImg.Tag = file.FullName;

        //    // Add handler
        //    newImg.MouseDown += gifImage_MouseDown;






        //    // Add to list
        //    uiImageElements.Add(newImg);

        //    return newImg;
        //}

        private void gifImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get clicked image
            Image uiElementClicked = (Image)sender;

            // Get path from tag
            string path = uiElementClicked.Tag.ToString();

            StringCollection paths = new StringCollection();
            paths.Add(path);

            // add to clipboard
            Clipboard.Clear();
            Clipboard.SetFileDropList(paths);

            if (chkNotificationsOn.IsChecked == true)
            {
                // Toast
                ni.ShowBalloonTip(2, "", "Copied to clipboard.", System.Windows.Forms.ToolTipIcon.None);
            }
        }

        private MediaElement CreateVideoElement(FileInfo file)
        {
            // Create UI element from bitmap image source
            MediaElement videoMeme = new MediaElement();
            videoMeme.Source = new Uri(file.FullName);

            // Set size
            videoMeme.Width = CalculateImageWidth();
            videoMeme.Height = Double.NaN;

            // Add location to tag
            videoMeme.Tag = file.FullName;

            // Add handler
            videoMeme.MouseDown += UiVideoElement_MouseDown;
            videoMeme.MouseEnter += VideoMeme_MouseEnter;
            videoMeme.MouseLeave += VideoMeme_MouseLeave; ;

            // Mute videos
            videoMeme.IsMuted = true;
            videoMeme.MediaEnded += VideoMeme_MediaEnded;
            videoMeme.UnloadedBehavior = MediaState.Stop;
            videoMeme.LoadedBehavior = MediaState.Manual;

            // Add to list
            uiVideoElements.Add(videoMeme);

            return videoMeme;
        }

        private void VideoMeme_MouseLeave(object sender, MouseEventArgs e)
        {
            MediaElement video = (MediaElement)sender;
            video.Play();
        }

        private void VideoMeme_MouseEnter(object sender, MouseEventArgs e)
        {
            MediaElement video = (MediaElement)sender;
            video.Stop();
        }

        private void VideoMeme_MediaEnded(object sender, RoutedEventArgs e)
        {
            MediaElement vid = (MediaElement)e.Source;
            vid.Play();
        }

        private Image CreateImageElement(FileInfo file)
        {
            // Create UI element from bitmap image source
            Image imageMeme = new Image();
            imageMeme.Source = new BitmapImage(new Uri(file.FullName));

            // Set size
            imageMeme.Width = CalculateImageWidth();
            imageMeme.Height = Double.NaN;

            // Add location to tag
            imageMeme.Tag = file.FullName;

            // Add handler
            imageMeme.MouseDown += UiImageElement_MouseDown;

            // Add to list
            uiImageElements.Add(imageMeme);

            return imageMeme;
        }

        private void UiVideoElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //// add to clipboard
            //StringCollection paths = new StringCollection();
            //paths.Add(((MediaElement)sender).Tag.ToString());
            //Clipboard.Clear();
            //Clipboard.SetFileDropList(paths);

            if (chkNotificationsOn.IsChecked == true)
            {
                // Toast
                ni.ShowBalloonTip(2, "Cannot copy to clipboard", "Format not supported yet.", System.Windows.Forms.ToolTipIcon.Error);
            }
        }

        private void UiImageElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get clicked image
            Image uiElementClicked = (Image)sender;

            // Get path from tag
            string path = uiElementClicked.Tag.ToString();

            // Create new uri and bitmap
            var uri = new Uri(path);
            BitmapImage img = new BitmapImage(uri);

            // Create clipboard image
            Bitmap bm = new Bitmap(path);

            // add to clipboard
            Clipboard.Clear();
            Clipboard.SetImage(img);

            if (chkNotificationsOn.IsChecked == true)
            {
                // Toast
                ni.ShowBalloonTip(2, "", "Copied to clipboard.", System.Windows.Forms.ToolTipIcon.None);
            }
        }


        private void sliderImageSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Resize all images
            foreach (Image img in uiImageElements)
            {
                img.Width = CalculateImageWidth();
                img.Height = Double.NaN;
            }

            // Resize all wrap panels
            foreach (WrapPanel wrap in uiWrapPanels)
            {
                wrap.ItemWidth = CalculateImageWidth();
                wrap.ItemHeight = Double.NaN;
            }

            // Resize all videos
            foreach (MediaElement video in uiVideoElements)
            {
                video.Width = CalculateImageWidth();
                video.Height = Double.NaN;
            }
        }

        private double CalculateImageWidth()
        {
            return sliderImageSize.Value * 10;
        }

        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void miToastsEnabled_Click(object sender, RoutedEventArgs e)
        {
            chkNotificationsOn.IsChecked = !chkNotificationsOn.IsChecked;
        }

        private void btnRescanMemesDir_Click(object sender, RoutedEventArgs e)
        {
            RefreshMemes();
        }

        private void btnOpenMemesDir_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(MEME_DIRECTORY_ROOT);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
            return;
        }

        private void wndMain_Closed(object sender, EventArgs e)
        {

        }
    }
}
