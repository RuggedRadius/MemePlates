using ColorPickerWPF;
using Notifications.Wpf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        
        string[] acceptedFileTypes = { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };
        List<Image> uiElements = new List<Image>();
        List<WrapPanel> uiWrapPanels = new List<WrapPanel>();

        NotificationManager toastMan = new NotificationManager();

        // Constructor
        public MainWindow()
        {
            InitializeComponent();

            // Load images
            LoadMemes();
        }

        private void LoadMemes()
        {
            // Initialise list of directories
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();

            // Get files
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Memes";
            dirs.Add(new DirectoryInfo(path));

            // Check if folder initialisation is needed
            if (!Directory.Exists(path))
            {
                MessageBox.Show("No folder");
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path + "\\Reactions");
                Directory.CreateDirectory(path + "\\Rename me 1");
                Directory.CreateDirectory(path + "\\Rename me 2");
                File.Create(path + "\\put memes here.txt");
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

                // Add all accepted file types
                foreach (string filetype in acceptedFileTypes)
                {
                    FileInfo[] files = dir.GetFiles(filetype);
                    foreach (FileInfo file in files)
                    {
                        // Add UI element
                        newPanel.Children.Add(CreateUIElement(file));
                    }
                }
            }
        }

        private void RefreshMemes()
        {
            if (miToastsEnabled.IsChecked)
            {
                // Toast
                toastMan.Show(new NotificationContent
                {
                    Title = "Refreshing memes",
                    Message = "Refreshing your memes now...",
                    Type = NotificationType.Information
                });
            }


            // Clear all UI elements
            uiElements.Clear();
            uiWrapPanels.Clear();
            tabMain.Items.Clear();

            // Reload memes
            LoadMemes();
        }

        private Image CreateUIElement(FileInfo file)
        {
            // Create UI element from bitmap image source
            Image uiElement = new Image();
            uiElement.Source = new BitmapImage(new Uri(file.FullName));

            // Set size
            uiElement.Width = CalculateImageWidth();
            uiElement.Height = Double.NaN;

            // Add location to tag
            uiElement.Tag = file.FullName;

            // Add handler
            uiElement.MouseDown += UiElement_MouseDown;

            // Add to list
            uiElements.Add(uiElement);

            return uiElement;
        }

        private void UiElement_MouseDown(object sender, MouseButtonEventArgs e)
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

            if (miToastsEnabled.IsChecked)
            {
                // Toast
                toastMan.Show(new NotificationContent
                {
                    Title = "Copied to clipboard!",
                    Message = "Meme cannon loaded",
                    Type = NotificationType.Success
                });
            }
        }

        private void sliderImageSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Resize all images
            foreach (Image img in uiElements)
            {
                img.Width = CalculateImageWidth();
                img.Height = Double.NaN;
            }

            // Resize all wrap panel items
            foreach (WrapPanel wrap in uiWrapPanels)
            {
                wrap.ItemWidth = CalculateImageWidth();
                wrap.ItemHeight = Double.NaN;
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

        private void miRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshMemes();
        }

        private void miToastsEnabled_Click(object sender, RoutedEventArgs e)
        {
            miToastsEnabled.IsChecked = !miToastsEnabled.IsChecked;
        }

        private void miBackgroundColour_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Color color;

            bool ok = ColorPickerWindow.ShowDialog(out color);

            if (ok)
            {
                RadialGradientBrush bg = new RadialGradientBrush();
                bg.GradientOrigin = new System.Windows.Point(0.5, 0.5);
                bg.Center = new System.Windows.Point(0.5, 0.5);

                GradientStop inner = new GradientStop();
                inner.Color = color;
                inner.Offset = 0.5;
                bg.GradientStops.Add(inner);

                GradientStop outer = new GradientStop();
                outer.Color = Colors.Black;
                outer.Offset = 1;
                bg.GradientStops.Add(outer);

                dpMaster.Background = bg;
            }
        }
    }
}
