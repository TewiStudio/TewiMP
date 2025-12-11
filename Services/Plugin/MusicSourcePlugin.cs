namespace TewiMP.Services.Plugin;

using System;
using System.Threading.Tasks;
using TewiMP.Core.Music;

/// <summary>
/// 音乐源插件基类。继承此类可被插件系统识别为音乐源插件，会被系统用于加载音乐数据。
/// </summary>
public abstract class MusicSourcePlugin : Plugin
{
    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲音频文件地址。
    /// </summary>
    /// <param name="id"></param>
    /// <param name="br">音频文件质量</param>
    /// <returns></returns>
    public abstract Task<string> GetUrl(string id, int br);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲歌词，返回值为歌词内容和翻译内容的元组。
    /// </summary>
    /// <param name="id"></param>
    /// <returns><see cref="Tuple{Lyric, LyricTranslate}"/> (Lyric, LyricTranslate)</returns>
    public abstract Task<Tuple<string, string>> GetLyric(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲专辑封面图片地址。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<string> GetPic(string id);

    /// <summary>
    /// 通过 <paramref name="musicData"/> 获取歌曲专辑封面图片地址。
    /// </summary>
    /// <param name="musicData"></param>
    /// <returns></returns>
    public abstract Task<string> GetPicFromMusicData(MusicData musicData);

    /// <summary>
    /// 通过 <paramref name="keyword"/> 搜索音乐，返回搜索结果对象。对象类型由具体插件决定。
    /// TODO: 规范返回对象类型。
    /// </summary>
    /// <param name="keyword"></param>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public abstract Task<object> GetSearch(string keyword, int pageNumber = 1, int pageSize = 30, int type = 0);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌单信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<MusicListData> GetPlayList(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取艺术家信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<Artist> GetArtist(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取专辑信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<Album> GetAlbum(string id);

    /// <summary>
    /// 通过 <paramref name="id"/> 获取歌曲信息。
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public abstract Task<MusicData> GetMusicData(string id);

    public override string ToString()
    {
        return PluginInfo.Name;
    }
}
