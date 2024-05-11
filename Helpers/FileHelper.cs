using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using static NMeCab.Core.DoubleArray;

namespace TewiMP.Helpers
{
    public static class FileHelper
    {
        /// <summary>
        /// 查询或获取音频缓存文件路径
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// !=null - 音频缓存文件路径 /
        /// null - 未查询到音频缓存
        /// </returns>
        public static async Task<string> GetAudioCache(DataEditor.MusicData musicData)
        {
            return await Task.Run(() =>
            {
                System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(DataEditor.DataFolderBase.AudioCacheFolder);
                System.IO.FileInfo[] fileInfo = directory.GetFiles();
                foreach (System.IO.FileInfo file in fileInfo)
                {
                    string name = file.Name.Split('.')[0];
                    if (name == musicData.From + musicData.ID)
                    {
                        return file.FullName;
                    }
                }
                return null;
            });
        }
        
        /// <summary>
        /// 查询或获取图片缓存文件路径
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>
        /// !=null - 图片缓存文件路径 /
        /// null - 未查询到图片缓存
        /// </returns>
        public static async Task<string> GetImageCache(string fileName)
        {
            return await Task.Run(() =>
            {
                DirectoryInfo directory = new(DataEditor.DataFolderBase.ImageCacheFolder);
                FileInfo[] fileInfo = directory.GetFiles();
                foreach (FileInfo file in fileInfo)
                {
                    if (file.Name == fileName)
                    {
                        return file.FullName;
                    }
                }
                return null;
            });
        }
        
        /// <summary>
        /// 查询或获取图片缓存文件路径
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// !=null - 图片缓存文件路径 /
        /// null - 未查询到图片缓存
        /// </returns>
        public static async Task<string> GetImageCache(DataEditor.MusicData musicData)
        {
            var filename = musicData.From == DataEditor.MusicFrom.localMusic ?
                $"{musicData.From}{musicData.MD5.Replace(@"/", "#")}" :
                $"{musicData.From}{(string.IsNullOrEmpty(musicData.Album?.ID) ? musicData.MD5.Replace(@"/", "#") : musicData.Album.ID)}";
            return await GetImageCache(filename);
        }
        
        /// <summary>
        /// 查询或获取图片缓存文件路径
        /// </summary>
        /// <param name="musicListData"></param>
        /// <returns>
        /// !=null - 图片缓存文件路径 /
        /// null - 未查询到图片缓存。如果为本地歌单会返回歌单记录的图片文件地址
        /// </returns>
        public static async Task<string> GetImageCache(DataEditor.MusicListData musicListData)
        {
            if (musicListData.ListDataType == DataEditor.DataType.本地歌单)
            {
                return musicListData.PicturePath;
            }
            return await GetImageCache($"{musicListData.ListFrom}{musicListData.ListDataType}{musicListData.ID}");
        }
        
        /// <summary>
        /// 查询或获取歌词缓存文件路径
        /// </summary>
        /// <param name="musicData"></param>
        /// <returns>
        /// !=null - 歌词缓存文件路径 /
        /// null - 未查询到歌词缓存
        /// </returns>
        public static async Task<string> GetLyricCache(DataEditor.MusicData musicData)
        {
            return await Task.Run(() =>
            {
                if (musicData.From == DataEditor.MusicFrom.localMusic)
                {
                    var file = new FileInfo(musicData.InLocal);
                    string lrcPath = $"{file.FullName.Replace(file.Extension, "")}.lrc";
                    if (File.Exists(lrcPath)) return lrcPath;
                }
                else
                {
                    DirectoryInfo directory = new DirectoryInfo(DataEditor.DataFolderBase.LyricCacheFolder);
                    FileInfo[] fileInfo = directory.GetFiles();
                    foreach (FileInfo file in fileInfo)
                    {
                        if (file.Name == musicData.From + musicData.ID)
                        {
                            return file.FullName;
                        }
                    }
                }
                return null;
            });
        }

        public static async Task<ImageSource> GetImageSource(string filePath, int decodePixelWidth = 0, int decodePixelHeight = 0, bool useBitmapImage = false)
        {
            //System.Diagnostics.Debug.WriteLine(filePath);
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = @"ms-appx:///Images/icon.png";
                return GetImageSource(new Uri(filePath), decodePixelWidth, decodePixelHeight);
            }
            else if (Path.GetExtension(filePath) == ".gif" || useBitmapImage)
            {
                return GetImageSource(new Uri(filePath), decodePixelWidth, decodePixelHeight);
            }
            else
            {
                try
                {
                    return await OpenWriteableBitmapFile(await StorageFile.GetFileFromPathAsync(filePath));
                }
                catch
                {
                    return null;
                }
            }
        }

        public static ImageSource GetImageSource(Uri fileUri, int decodePixelWidth = 0, int decodePixelHeight = 0)
        {
            return new BitmapImage(fileUri) { DecodePixelWidth = decodePixelWidth, DecodePixelHeight = decodePixelHeight };
        }

        private static async Task<WriteableBitmap> OpenWriteableBitmapFile(StorageFile file)
        {
            using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            WriteableBitmap image = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
            //pixelWidth == 0 ? (int)decoder.PixelWidth : pixelWidth,
            //pixelHeight == 0 ? (int)decoder.PixelHeight : pixelHeight);
            await image.SetSourceAsync(stream);
            return image;
        }

        public static async Task<IReadOnlyList<StorageFile>> UserSelectFiles(
            PickerViewMode viewMode = default,
            PickerLocationId suggestedStartLocation = default,
            string[] fileTypeFilter = null)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = viewMode;
            picker.SuggestedStartLocation = suggestedStartLocation;

            if (fileTypeFilter == null) fileTypeFilter = new string[] { "*" };
            foreach (var i in fileTypeFilter)
            {
                picker.FileTypeFilter.Add(i);
            }

            WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Handle);
            var files = await picker.PickMultipleFilesAsync();
            return files;
        }
        
        public static async Task<StorageFile> UserSelectFile(
            PickerViewMode viewMode = default,
            PickerLocationId suggestedStartLocation = default,
            string[] fileTypeFilter = null)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = viewMode;
            picker.SuggestedStartLocation = suggestedStartLocation;

            if (fileTypeFilter == null) fileTypeFilter = new string[] { "*" };
            foreach (var i in fileTypeFilter)
            {
                picker.FileTypeFilter.Add(i);
            }

            WinRT.Interop.InitializeWithWindow.Initialize(picker, MainWindow.Handle);
            var files = await picker.PickSingleFileAsync();
            return files;
        }

        public static async Task<StorageFolder> UserSelectFolder(PickerLocationId suggestedStartLocation = default)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = suggestedStartLocation;

            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, MainWindow.Handle);
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder;
        }

        public static async Task<StorageFile> UserSaveFile(
            string suggestedFileName = "SaveFile",
            PickerLocationId suggestedStartLocation = default,
            string[] fileTypeFilter = null, string fileTypeFilterKey = null,
            nint windowHandle = 0)
        {
            var saveFile = new FileSavePicker();
            saveFile.SuggestedStartLocation = suggestedStartLocation;
            saveFile.FileTypeChoices.Add(fileTypeFilterKey, fileTypeFilter);
            saveFile.SuggestedFileName = suggestedFileName;

            WinRT.Interop.InitializeWithWindow.Initialize(saveFile, windowHandle == 0 ? MainWindow.Handle : windowHandle);
            StorageFile sFile = await saveFile.PickSaveFileAsync();

            return sFile;
        }

        public static async Task OpenFilePath(string openPath)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(openPath);
            await Launcher.LaunchFolderAsync(folder);
        }

        public static async Task<string> FileTypeGetAsync(string name)
        {
            return await Task.Run(async () =>
            {
                FileStream fs = new FileStream(@name, FileMode.Open, FileAccess.Read);
                byte[] imagebytes = new byte[fs.Length];
                BinaryReader br = new BinaryReader(fs);
                imagebytes = br.ReadBytes(2);
                string ss = "";
                for (int i = 0; i < imagebytes.Length; i++)
                {
                    ss += imagebytes[i];
                }
                fs.Close();
                fs.Dispose();
                br.Close();
                br.Dispose();
                return ss;
            });
        }

        public static string FileTypeGet(string filename)
        {
            return FileTypeGet(File.Create(filename));
        }

        public static string FileTypeGet(FileStream fs)
        {
            byte[] imagebytes = new byte[fs.Length];
            BinaryReader br = new BinaryReader(fs);
            imagebytes = br.ReadBytes(2);
            string ss = "";
            for (int i = 0; i < imagebytes.Length; i++)
            {
                ss += imagebytes[i];
            }
            return ss;
        }

        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
            byte curByte; //当前分析的字节. 
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }

        public static Encoding GetEncodeingType(FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM 
            Encoding reVal = Encoding.Default;

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }

        public static Encoding GetEncodeingType(string FILE_NAME)
        {
            FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read);
            Encoding r = GetEncodeingType(fs);
            fs.Close();
            return r;
        }
    }
}
