using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.DataEditor;

namespace TewiMP.Controls
{
    public partial class LyricItem : Grid
    {
        private static List<LyricItem> Items = new List<LyricItem>();

        public static void SetTextAlignmentS(TextAlignment textAlignment)
        {
            foreach (var item in Items)
            {
                if (item == null) continue;
                item.SetTextAlignment(textAlignment);
            }
        }

        public LyricItem()
        {
            InitializeComponent();
        }

        static TextAlignment alignment = TextAlignment.Left;
        public void SetTextAlignment(TextAlignment textAlignment)
        {
            if (LyricTextBlock == null) return;
            alignment = textAlignment;
            LyricTextBlock.TextAlignment = textAlignment;
            RomajiTextBlock.TextAlignment = textAlignment;
        }

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null)
            {
                Items.Remove(this);
                return;
            }
            if (DataContext.GetType() != typeof(LyricData))
            {
                Items.Remove(this);
                return;
            }
            Items.Add(this);
            SetTextAlignment(alignment);
            LyricData lyricData = (LyricData)DataContext;
            LyricTextBlock.Text = lyricData.LyricAllString;
            if (!string.IsNullOrEmpty(lyricData.Romaji))
            {
                RomajiTextBlock.Text = lyricData.Romaji;
                RomajiTextBlock.Height = double.NaN;
            }
            else
            {
                RomajiTextBlock.Text = null;
                RomajiTextBlock.Height = 0;
            }
            /*
            RomajiTextBlock.Text = lyricData.LyricTimeSpan.ToString();
            RomajiTextBlock.Height = double.NaN;*/
#if DEBUG
            DebugTimeDisplayer_TextBlock.Text = lyricData.LyricTimeSpan.ToString();
#endif
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            DebugTimeDisplayer.XamlRoot = this.XamlRoot;
            DebugTimeDisplayer.IsOpen = true;
#endif
        }

        private void Grid_Unloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
