using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TewiMP.Services;
using TewiMP.Services.Storage;
using TewiMP.Helpers;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT.Interop;

namespace TewiMP.Windowed
{
    public partial class ImageViewerWindow : Window
    {
        public static ImageViewerWindow ShowWindow(Uri image, string saveName = null)
        {
            var window = new ImageViewerWindow();
            window.SetSource(image);
            window.SetSaveName(saveName);
            window.SystemBackdrop = new DesktopAcrylicBackdrop();
            window.AppWindow.TitleBar.IconShowOptions = Microsoft.UI.Windowing.IconShowOptions.HideIconAndSystemMenu;
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            window.AppWindow.TitleBar.ButtonBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 0, 0, 0);
            window.AppWindow.TitleBar.ButtonForegroundColor =
                App.Current.RequestedTheme == ApplicationTheme.Light ? Color.FromArgb(255, 0, 0, 0) : Color.FromArgb(255, 255, 255, 255);
            window.AppWindow.Resize(new(500,500));
            window.Maximize();
            window.Activate();
            window.Title = "查看图片";
            window.AppWindow.SetIcon(Path.Combine("Images", "Icons", "icon.ico"));
            return window;
        }

        OverlappedPresenter overlappedPresenter = null;
        IRandomAccessStream storageFileStream;
        static bool IsOpened = false;
        public ImageViewerWindow()
        {
            InitializeComponent();
            overlappedPresenter = OverlappedPresenter.Create();
            AppWindow.SetPresenter(overlappedPresenter);
            ImageScrollView.Focus(FocusState.Programmatic);
        }

        public void Maximize()
        {
            overlappedPresenter.Maximize();
        }

        public void SetSource(Uri imageSource)
        {
            ImageControl_Source.UriSource = imageSource;
        }

        public string SaveName { get; set; }
        public void SetSaveName(string name)
        {
            SaveName = name;
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ZoomTextBlock.Text = $"{Math.Round(ImageScrollView.ZoomFactor * 100, 0)}%";
        }

        ContentDialog contentDialog = null;
        private async Task ShowDialog(string title, string content)
        {
            if (contentDialog is null)
            {
                contentDialog = new ContentDialog()
                {
                    XamlRoot = Content.XamlRoot
                };
            }
            contentDialog.Background = App.Current.Resources["AcrylicNormal"] as AcrylicBrush;
            contentDialog.Title = title;
            contentDialog.Content = content;
            contentDialog.CloseButtonText = "确定";
            await contentDialog.ShowAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            StorageFile targetFile = null;

            //if (ImageControl.Source.GetType() == typeof(WriteableBitmap))
            if (true)
            {
                targetFile = await FileHelper.UserSaveFile(SaveName is null ? "一张图片" : SaveName, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary, [".png", ".gif"], "图片",
                    windowHandle: WindowNative.GetWindowHandle(this));
            }
            else
            {
                await ShowDialog("无法保存图片", "不支持保存此类型的图片。");
            }
            var sourceFile = await StorageFile.GetFileFromPathAsync(ImageControl_Source.UriSource.LocalPath);
            if (targetFile != null)
            {
                try
                {
                    await sourceFile.CopyAndReplaceAsync(targetFile);
                }
                catch (Exception err)
                {
                    LogService.Error("ImageEx MenuFlyoutItem_Click", err.ToString());
                    await ShowDialog("保存图片失败", $"保存图片时出现错误：\n{err.Message}");
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ImageScrollView.ChangeView(null, null, 1f);
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as RepeatButton;
            bool zoommax = true;
            switch (btn.Tag as string)
            {
                case "1":
                    zoommax = false;
                    break;
                case "2":
                    zoommax = true;
                    break;
            }
            ImageScrollView.ChangeView(
                null, null,
                ImageScrollView.ZoomFactor + (zoommax ? 0.1f : -0.1f));
        }

        private void ImageScrollView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                ImageScrollView.VerticalScrollMode = ScrollMode.Disabled;
            }
        }

        private void ImageScrollView_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                ImageScrollView.VerticalScrollMode = ScrollMode.Enabled;
            }
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
            storageFileStream.Dispose();
        }
    }
}
