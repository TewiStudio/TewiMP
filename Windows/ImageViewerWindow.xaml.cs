using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using TewiMP.Helpers;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;
using TewiMP.DataEditor;
using WinRT.Interop;
using Windows.UI;

namespace TewiMP.Windowed
{
    public partial class ImageViewerWindow : Window
    {
        public static ImageViewerWindow ShowWindow(ImageSource image, string saveName = null)
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

        public void SetSource(ImageSource imageSource)
        {
            ImageControl.Source = imageSource;
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
            StorageFile f = null;

            if (ImageControl.Source.GetType() == typeof(WriteableBitmap))
            {
                f = await FileHelper.UserSaveFile(SaveName is null ? "一张图片" : SaveName, Windows.Storage.Pickers.PickerLocationId.PicturesLibrary, [".png"], "图片",
                    windowHandle: WindowNative.GetWindowHandle(this)
                    );
            }
            else
            {
                await ShowDialog("无法保存图片", "不支持保存此类型的图片。");
            }

            if (f != null)
            {
                try
                {
                    using (IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        WriteableBitmap wb = ImageControl.Source as WriteableBitmap;
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        Stream pixelStream = wb.PixelBuffer.AsStream();
                        byte[] pixels = new byte[pixelStream.Length];
                        await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                            (uint)wb.PixelWidth,
                                            (uint)wb.PixelHeight,
                                            96.0, 96.0,
                                            pixels);
                        await encoder.FlushAsync();
                    }
                }
                catch (Exception err)
                {
                    LogHelper.WriteLog("ImageEx MenuFlyoutItem_Click", err.ToString(), false);
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
    }
}
