using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TewiMP.DataEditor;

namespace TewiMP.Controls
{
    public sealed partial class SongHistoryInfo : UserControl
    {
        public SongHistoryInfo()
        {
            this.InitializeComponent();
            Init();
        }

        private async void Init()
        {
            var datas = await SongHistoryHelper.GetHistories();
            Dictionary<MusicData, int> listenSongMost = new();
            Dictionary<Artist, int> listenArtistMost = new();
            Dictionary<Album, int> listenAlbumMost = new();
            IOrderedEnumerable<KeyValuePair<MusicData, int>> listenSongMost5 = null;
            IOrderedEnumerable<KeyValuePair<Artist, int>> listenArtistMost5 = null;
            IOrderedEnumerable<KeyValuePair<Album, int>> listenAlbumMost5 = null;
            await Task.Run(() =>
            {
                foreach (var songdata in datas)
                {
                    var data = songdata.MusicData;
                    if (data is null) continue;
                    // 计算歌曲出现次数
                    if (!listenSongMost.ContainsKey(data))
                    {
                        listenSongMost.Add(data, 1);
                    }
                    else
                    {
                        listenSongMost[data]++;
                    }

                    // 计算艺术家出现次数
                    foreach (Artist art in data.Artists)
                    {
                        if (!listenArtistMost.ContainsKey(art))
                        {
                            listenArtistMost.Add(art, 1);
                        }
                        else
                        {
                            listenArtistMost[art]++;
                        }
                    }

                    // 计算专辑出现次数
                    if (!listenAlbumMost.ContainsKey(data.Album))
                    {
                        listenAlbumMost.Add(data.Album, 1);
                    }
                    else
                    {
                        listenAlbumMost[data.Album]++;
                    }
                }
                listenSongMost5 = from pair in listenSongMost orderby pair.Value descending select pair;
                listenArtistMost5 = from pair in listenArtistMost orderby pair.Value descending select pair;
                listenAlbumMost5 = from pair in listenAlbumMost orderby pair.Value descending select pair;
            });
            int count = 0;
            foreach (var d in listenSongMost5)
            {
                count++;
                if (count > 10) break;
                d.Key.Count = count;
                d.Key.Title2 = $"听了 {d.Value} 次";
                ListenedMusicMost.Items.Add(new Helpers.SongItemBindBase() { MusicData = d.Key });
            }

            int countartist = 0;
            foreach (var a in listenArtistMost5)
            {
                countartist++;
                if (countartist > 10) break;
                a.Key.Count = countartist;
                a.Key.Name2 = $"听了共 {a.Value} 次";
                ListenedArtistMost.Items.Add(a.Key);
            }

            int countalbum = 0;
            foreach (var l in listenAlbumMost5)
            {
                countalbum++;
                if (countalbum > 10) break;
                l.Key.Count = countalbum;
                l.Key.Title2 = $"听了共 {l.Value} 次";
                ListenedAlbumMost.Items.Add(l.Key);
            }
        }

        private void ImageEx_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var image = sender as ImageEx;
            var data = image.DataContext as MusicData;
            if (image != null)
            {

            }
        }
    }
}
