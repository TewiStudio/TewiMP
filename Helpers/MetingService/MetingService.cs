using System;
using System.Threading.Tasks;
using TewiMP.DataEditor;
using TewiMP.Helpers.MetingService;

namespace TewiMP.Helpers
{
    public interface IMetingService
    {
        public Meting4Net.Core.Meting Services { get; set; }
        public Task<string> GetUrl(string id, int br);
        public Task<Tuple<string, string>> GetLyric(string id);
        public Task<string> GetPic(string id);
        public Task<string> GetPicFromMusicData(MusicData musicData);
        public Task<object> GetSearch(string keyword, int pageNumber = 1, int pageSize = 30, int type = 0);
        public Task<MusicListData> GetPlayList(string id);
        public Task<Artist> GetArtist(string id);
        public Task<Album> GetAlbum(string id);
        public Task<MusicData> GetMusicData(string id);
    }

    public class MetingServices
    {
        public int RetryCount = 15;
        public string NeteaseCookie = "MUSIC_U=00870CD16FF9A2C399E286D034532F5A95A6BDC5D29D6B2CD9F927E6AB57513D3DD7BBEB7082A04E2EBB367B45DD8EE96CF199ABE9670277016495018E29AD2B98C7B72C3500F161D47C2A86AE30E4DF5F3D1568CD28AD91F58C520FF3C1092B3122C79F62C7282819D166049EA1C1BED5B149166E260A5823CF33B9F2A383BEA96145083B4666A3FF372DAECF482BB0283B2CB6E7271E234BEF4BA00FD1E37E118829DCE591233A5FA118306B1EC2D46B7108FF7DCA51852A5C033EEAF15316F85B56709EC3BC1BE14EF4534634862E5617CD9372F97784ED0F729C74701482C4E612A9DB844CF8E6CCEF9FDCAB6EA9B6D7891F9E7A7112DF2DE1E658AD28DF8F67E310C190E2631780289C5CA85FC1B915F97C708697A99719B4C2E305385C80637A44A0C756D5CBE7D819E282D7D2A5CE3E177A88CE5C57C4D290F7641AA2CEDDF1AF5CC1F2B838F4DB50E84CC10E9B4F697DAB6A1016D882995E4864AEB414948218B329F61D65D37A8743686A40D9";
        private Meting4Net.Core.Meting NeteaseMeting = new(Meting4Net.Core.ServerProvider.Netease);
        private Meting4Net.Core.Meting KgMeting = new(Meting4Net.Core.ServerProvider.Kugou);
        public IMetingService NeteaseServices { get; set; }
        public IMetingService KgServices { get; set; }

        public MetingServices()
        {
            NeteaseServices = new NeteaseMeting(NeteaseMeting);
            KgServices = new KgMeting(KgMeting);
        }

        public async void InitMeting()
        {
            NeteaseMeting.Cookie("os=pc; " + NeteaseCookie);
        }

    }
}
