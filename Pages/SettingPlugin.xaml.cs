﻿using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Composition;
using CommunityToolkit.WinUI.Controls;
using TewiMP.Plugin;
using TewiMP.Helpers;
using TewiMP.DataEditor;

namespace TewiMP.Pages
{
    public partial class SettingPlugin : Page
    {
        public SettingPlugin()
        {
            InitializeComponent();
        }


        bool isInLoading = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            isInLoading = true;
            PluginMusicSource.ItemsSource = PluginManager.MusicSourcePlugins;
            PluginOther.ItemsSource = PluginManager.Plugins;
            isInLoading = false;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShyHeader();
        }

        public void UpdateShyHeader()
        {
            // 设置 header 为顶层
            var headerPresenter = (UIElement)VisualTreeHelper.GetParent((UIElement)ListViewBase.Header);
            var headerContainer = (UIElement)VisualTreeHelper.GetParent(headerPresenter);
            Canvas.SetZIndex(HeaderBaseGrid, 1);

            var scrollViewer = (VisualTreeHelper.GetChild(ListViewBase, 0) as Border).Child as ScrollViewer;
            scrollViewer.CanContentRenderOutsideBounds = true;

            CompositionPropertySet scrollerPropertySet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(scrollViewer);
            Compositor compositor = scrollerPropertySet.Compositor;

            var padingSize = 40;
            // Get the visual that represents our HeaderTextBlock 
            // And define the progress animation string
            var headerVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseGrid);
            String progress = $"Clamp(-scroller.Translation.Y / {padingSize}, 0, 1.0)";

            // Shift the header by 50 pixels when scrolling down
            var offsetExpression = compositor.CreateExpressionAnimation($"-scroller.Translation.Y - {progress} * {padingSize}");
            offsetExpression.SetReferenceParameter("scroller", scrollerPropertySet);
            headerVisual.StartAnimation("Offset.Y", offsetExpression);

            /*
            Visual textVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            Vector3 finalOffset = new Vector3(0, 10, 0);
            var headerOffsetAnimation = compositor.CreateExpressionAnimation($"Lerp(Vector3(0,0,0), finalOffset, {progress})");
            headerOffsetAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            headerOffsetAnimation.SetVector3Parameter("finalOffset", finalOffset);
            textVisual.StartAnimation(nameof(Visual.Offset), headerOffsetAnimation);
            */

            // Logo scale and transform                                          from               to
            var logoHeaderScaleAnimation = compositor.CreateExpressionAnimation("Lerp(Vector2(1,1), Vector2(0.7, 0.7), " + progress + ")");
            logoHeaderScaleAnimation.SetReferenceParameter("scroller", scrollerPropertySet);

            var logoVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseTextBlock);
            logoVisual.StartAnimation("Scale.xy", logoHeaderScaleAnimation);

            var logoVisualOffsetYAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 24, {progress})");
            logoVisualOffsetYAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.Y", logoVisualOffsetYAnimation);

            var logoVisualOffsetXAnimation = compositor.CreateExpressionAnimation($"Lerp(0, -12, {progress})");
            logoVisualOffsetXAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            logoVisual.StartAnimation("Offset.X", logoVisualOffsetXAnimation);

            var backgroundVisual = ElementCompositionPreview.GetElementVisual(HeaderBaseRectangle);
            var backgroundVisualOpacityAnimation = compositor.CreateExpressionAnimation($"Lerp(0, 1, {progress})");
            backgroundVisualOpacityAnimation.SetReferenceParameter("scroller", scrollerPropertySet);
            backgroundVisual.StartAnimation("Opacity", backgroundVisualOpacityAnimation);
        }

        private void SettingsCard_Click(object sender, RoutedEventArgs e)
        {
            PluginManager.Init();
        }

        private async void SettingsCard_Click_1(object sender, RoutedEventArgs e)
        {
            if (sender is SettingsCard card)
            {
                if (card.DataContext is Plugin.Plugin plugin)
                {
                    await plugin.ShowSettingsDialog();
                    PluginManager.SavePluginInfoSettings();
                }
            }
        }

        private void SettingsCard_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (sender is SettingsCard card && args.NewValue is Plugin.Plugin dataContent)
            {
                card.Header = $"{dataContent.PluginInfo.Name} ({dataContent.PluginInfo.Version})";
                card.Description = string.IsNullOrEmpty(dataContent.PluginInfo.Describe) ?
                    $"by {dataContent.PluginInfo.Author}" :
                    $"{dataContent.PluginInfo.Describe}\nby {dataContent.PluginInfo.Author}";
            }
        }

        private async void SettingsCard_Click_2(object sender, RoutedEventArgs e)
        {
            await FileHelper.ExploreFolder(DataFolderBase.PluginFolder);
        }
    }
}
