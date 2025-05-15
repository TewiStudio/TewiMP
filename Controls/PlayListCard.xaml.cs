using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using Newtonsoft.Json.Linq;
using Windows.UI;
using TewiMP.Media;
using TewiMP.Helpers;
using TewiMP.DataEditor;
using TewiMP.Pages.ListViewPages;

namespace TewiMP.Controls
{
    public partial class PlayListCard : Grid
    {
        private MusicListData MusicListData { get; set; }
        public double ImageScaleDPI { get; set; } = 1.0;
        public string ID { get; set; }
        public ImageEx ConnectAnimationElement { get; set; }
        public TextBlock ConnectAnimationElement1 { get; set; }

        public PlayListCard()
        {
            InitializeComponent();
            ConnectAnimationElement = PlayListImage;
            ConnectAnimationElement1 = TextBaseTb;
            DataContextChanged += PlayListCard_DataContextChanged;
        }

        private void PlayListCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is not null)
            {
                if (DataContext is MusicListData)
                {
                    Init(DataContext as MusicListData);
                }
            }
        }

        public async Task Init(MusicFrom musicFrom, string id)
        {/*
            MusicListData = await App.metingServices.NeteaseServices.GetPlayList(id);
            Init(MusicListData);
            UILoaded(null, null);*/
        }

        public void Init(MusicListData musicListData)
        {
            MusicListData = musicListData;
            UpdateImage();
            if (MusicListData.ListDataType != DataType.歌单)
            {
                RefreshPlayListButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                RefreshPlayListButton.Visibility = Visibility.Visible;
            }
            if (musicListData.ListDataType == DataType.本地歌单)
            {
                EditPlayListButton.Visibility = Visibility.Visible;
            }
            else
            {
                EditPlayListButton.Visibility = Visibility.Collapsed;
            }
        }

        public async void UpdateImage()
        {
            //ExitMass();
            if (MusicListData != null)
            {
                int size = 0;//(int)(200 * ImageScaleDPI);
                var imageSources = await ImageManage.GetImageSource(MusicListData, size, size, true);
                PlayListImage.Source = imageSources.Item1;
                PlayListImage.SaveName = $"{MusicListData.ListShowName}";
            }
            //CrateShadow();
        }

        Compositor compositor;
        DropShadow dropShadow;
        private void CrateShadow()
        {
            return;
            var visual = ElementCompositionPreview.GetElementVisual(ShadowBaseRectangle);
            compositor = visual.Compositor;

            var basicRectVisual = compositor.CreateSpriteVisual();
            basicRectVisual.Size = new Vector2((float)(ActualWidth - 8), (float)ActualHeight);

            dropShadow = compositor.CreateDropShadow();
            dropShadow.BlurRadius = 150f;
            dropShadow.Opacity = 1f;
            dropShadow.Color = Color.FromArgb(255, 0, 0, 0);
            dropShadow.Offset = new Vector3(0, 0, 0);

            basicRectVisual.Shadow = dropShadow;
            ElementCompositionPreview.SetElementChildVisual(ShadowBaseRectangle, basicRectVisual);
        }

        private async void UILoaded(object sender, RoutedEventArgs e)
        {
            //System.Diagnostics.App.logManager.Log(MusicListData.PicturePath);
        }

        private void UIUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (PlayListImage != null)
            {
               PlayListImage.Source = null;
            }
            MusicListData = null;
            DataContext = null;
            //PlayListImage = null;
            //System.Diagnostics.App.logManager.Log("[PlayListCard]: Disposed.");
        }

        private void Grid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            return;
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                AnimateHelper.AnimateOffset(
                    Children[1] as Border,
                    0, -2, 0,
                    0.2,
                    0.2f, 1f, 0.22f, 1f,
                    out Visual visual, out Compositor compositor, out Vector3KeyFrameAnimation animation);
                visual.StartAnimation(nameof(visual.Offset), animation);
/*
                AnimateHelper.AnimateScalar(
                    ABackColorBaseRectAngle,
                    1f, 0.2,
                    0.2f, 1, 0.22f, 1,
                    out Visual visual2, out Compositor compositor2, out ScalarKeyFrameAnimation animation2);
                visual2.StartAnimation(nameof(visual2.Opacity), animation2);
*/
                if (dropShadow != null)
                {
                    ScalarKeyFrameAnimation blurAnimation = compositor.CreateScalarKeyFrameAnimation();
                    blurAnimation.InsertKeyFrame(0.5f, 1f);
                    blurAnimation.Duration = TimeSpan.FromSeconds(0.5);
                    dropShadow.StartAnimation("Opacity", blurAnimation);
                }
            }
        }

        private void Grid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            return;
            if (e.GetCurrentPoint(sender as UIElement).PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                AnimateHelper.AnimateOffset(
                    Children[1] as Border,
                    0, 0, 0,
                    0.2,
                    0.2f, 1f, 0.22f, 1f,
                    out Visual visual, out Compositor compositor, out Vector3KeyFrameAnimation animation);
                visual.StartAnimation(nameof(visual.Offset), animation);

                ExitMass();

                if (dropShadow != null)
                {
                    ScalarKeyFrameAnimation blurAnimation = compositor.CreateScalarKeyFrameAnimation();
                    blurAnimation.InsertKeyFrame(1, 0f);
                    blurAnimation.Duration = TimeSpan.FromSeconds(0.5);
                    dropShadow.StartAnimation("Opacity", blurAnimation);
                }
            }
        }

        private void ExitMass()
        {/*
            AnimateHelper.AnimateScalar(
                ABackColorBaseRectAngle,
                0, 0.2,
                0.2f, 1, 0.22f, 1,
                out Visual visual2, out Compositor compositor2, out ScalarKeyFrameAnimation animation2);
            visual2.StartAnimation(nameof(visual2.Opacity), animation2);
*/
        }

        bool isPressed = false;
        bool isRightPressed = false;
        private void Grid_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            isPressed = true;
            isRightPressed = e.GetCurrentPoint(sender as UIElement).Properties.IsRightButtonPressed;
        }

        private void Grid_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (isPressed && MusicListData != null)
            {
                if (isRightPressed)
                {
                    FlyoutMenu.ShowAt(sender as FrameworkElement);
                    isRightPressed = false;
                }
                else
                {
                    ListViewPage.SetPageToListViewPage(new() { PageType = PageType.PlayList, Param = MusicListData.MD5 });
                }
            }
            isPressed = false;
        }

        private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            FlyoutMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private void Grid_Holding(object sender, Microsoft.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            FlyoutMenu.ShowAt(sender as UIElement, e.GetPosition(sender as UIElement));
        }

        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var isDelete = await MainWindow.ShowDialog("确认删除列表", $"真的要删除列表 \"{MusicListData.ListShowName}\" 吗？\n此操作不可逆。", "取消", "确定", defaultButton: ContentDialogButton.Close);
            if (isDelete == ContentDialogResult.Primary)
            {
                MainWindow.AddNotify("正在删除", $"正在删除列表 \"{MusicListData.ListShowName}\"。");
                await PlayListHelper.DeletePlayList(MusicListData);
                await App.playListReader.Refresh();
                MainWindow.AddNotify("删除列表成功。", null, NotifySeverity.Complete);
            }
        }

        private static async void UpdateMusicList(MusicListData musicListData)
        {
            MainWindow.AddNotify("正在更新歌单...", null);

            try
            {
                var deletePath = (await ImageManage.GetImageSource(musicListData)).Item2;
                await Task.Run(() =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(deletePath)) File.Delete(deletePath);
                    }
                    catch { }
                });


                var playlist = await musicListData.PluginSource.GetPlayList(musicListData.ID);
                musicListData = playlist;

                var data = await PlayListHelper.ReadData();
                data[musicListData.ListName] = JObject.FromObject(playlist);
                await PlayListHelper.SaveData(data);

                await App.playListReader.Refresh();
                MainWindow.AddNotify("更新歌单成功。", null, NotifySeverity.Complete);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("PlayingList Update Error", ex.ToString(), false);
                MainWindow.AddNotify("更新歌单失败", $"更新歌单时遇到错误，请重试。\n错误信息：{ex}", NotifySeverity.Error);
            }
        }

        private async void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            UpdateMusicList(MusicListData);
        }

        private void Grid_AccessKeyInvoked(UIElement sender, Microsoft.UI.Xaml.Input.AccessKeyInvokedEventArgs args)
        {
            if (MusicListData != null)
                ListViewPage.SetPageToListViewPage(new() { PageType = PageType.PlayList, Param = MusicListData.MD5 });
        }

        private async void EditPlayListButton_Click(object sender, RoutedEventArgs e)
        {
            await Pages.DialogPages.EditPlayListPage.ShowDialog(MusicListData);
        }

        private async void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.ShowDialog("排序播放列表", "");
        }
    }
}
