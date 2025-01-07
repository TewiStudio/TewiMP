using System;
using System.IO;
using System.Net;
using System.Text;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace TewiMP.Helpers
{
    [Obsolete]
    public static class NeteaseHelper
    {
        public static async void Exec(int id, int br)
        {
            var postData = NeteaseAESCBC(id, br);
            postData = $"params=4Bd6cR0b%2F0wgb4Y3i%2FilHR0eyJLZpNsb8uJHKzYU%2BUljLqtGWEVWkmE4ZWVT7S097hM4Jw%2BJtLC%2FeVTqMRXopTPbthdS3LAB1%2FDPMAYzUIIGCea6FpLtl7XR40lN0GdcIUHsyN%2B4HW54digvwR0W9A%3D%3D&encSecKey=c595c184334dda21728d8b6bf9886d71659c1266ede962cb88c801a4d533fceec4f411bbc5eed98768f3563b8a29a301ecdf2d5803f6b03f27ec8547924948ed4dd0d79f28ecac74926746ff24215221dcc46cafc64ef8ac4ded43d79b30b65f586d8c00472eeaa6c3961306953a32beb3ddae5254ba3965069d54ac790345c3";
            var downloadData = await Post(NETEASE_AUDIO_URL, postData);
        }

        private const string NETEASE_AUDIO_URL = "https://music.163.com/weapi/song/enhance/player/url/v1?csrf_token=";//"http://music.163.com/api/song/enhance/player/url";
        private const string MODULUS = "157794750267131502212476817800345498121872783333389747424011531025366277535262539913701806290766479189477533597854989606803194253978660329941980786072432806427833685472618792592200595694346872951301770580765135349259590167490536138082469680638514416594216629258349130257685001248172188325316586707301643237607";
        private const string PUBKEY = "65537";
        private const string NONCE = "0CoJUm6Qyw8W8jud";
        private const string VI = "0102030405060708";
        public static string NeteaseAESCBC(int id, int br)
        {
            string skey = GetRandomHex(16);

            string data =
                new JObject()
                {
                    { "ids", "replaceToDo"},
                    { "br", br * 1000 }
                }.ToString(Newtonsoft.Json.Formatting.None).Replace("\"replaceToDo\"", $"[{id}]");

            // 两次加密
            var aesData = AesEncrypt(data, NONCE, VI);
            var body = AesEncrypt(aesData, skey, VI);

            skey = skey.ToReverse();
            skey = Bchexdec(Str2hex(skey));

            skey = BigInteger.ModPow(
                BigInteger.Parse(skey),
                int.Parse(PUBKEY),
                BigInteger.Parse(MODULUS)
            ).ToString();

            skey = Bcdechex(skey);
            skey.PadLeft(256, '0');
            //App.logManager.Log(skey);

            return $"params={body}&encSecKey={skey}";
        }

        static string GetRandomHex(int length)
        {
            byte[] bytes = new byte[length / 2];
            Random random = new Random();
            random.NextBytes(bytes);

            return BitConverter.ToString(bytes).Replace("-", "");
        }

        static string Str2hex(string str)
        {
            string hex = "";
            var strAsciiCode = Encoding.ASCII.GetBytes(str.Trim());
            for (int i = 0; i < str.Length; i++)
            {
                string ord = Convert.ToString(str[i], 16);
                hex += ord;
            }
            return hex;
        }

        static string Bcdechex(string dec)
        {
            BigInteger dec1 = BigInteger.Parse(dec);
            var hex = "";
            while (dec1 > 0)
            {
                var last = dec1 % 16;
                hex += last.ToString("x4").Replace("000", "");
                dec1 = BigInteger.Divide(BigInteger.Subtract(dec1, last), 16);
            }

            return hex.ToReverse();
        }

        static string Bchexdec(string hex)
        {
            // 超大数计算
            BigInteger dec = BigInteger.Zero;
            int len = hex.Length;

            for (int i = 1; i <= len; i++)
            {
                var a1 = BigInteger.Parse(hex[i - 1].ToString());
                var a2 = BigInteger.Parse(BigInteger.Pow(16, len - i).ToString());
                var a3 = a1 * a2;
                dec += a3;
                //App.logManager.Log($"[{a1} / {a2} / {a3} / {dec}]");
            }

            return dec.ToString();
        }

        static string AesEncrypt(string plainText, string Key, string IV)
        {
            // Check arguments.
            if (plainText is null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key is null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV is null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(Key);
                aesAlg.IV = Encoding.UTF8.GetBytes(IV);
                aesAlg.Mode = CipherMode.CBC;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return Convert.ToBase64String(encrypted, 0, encrypted.Length);
        }

        /// <summary>
        /// 指定Post地址使用Get 方式获取全部字符串
        /// </summary>
        /// <param name="url">请求后台地址</param>
        /// <param name="content">Post提交数据内容(utf-8编码的)</param>
        /// <returns></returns>
        public static async Task<string> Post(string url, string content)
        {
            return await Task.Run(() =>
            {
                string result = "";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.134 Safari/537.36 Edg/103.0.1264.71");
                req.Headers.Add("Referer", "https://music.163.com/");
                req.Headers.Add("Origin", "https://music.163.com/");
                req.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8,gl;q=0.6,zh-TW;q=0.4");
                req.Headers.Add("Cookie", "_ntes_nnid=e0720c34928112f57e4337b387e75a50,1654241864468; _ntes_nuid=e0720c34928112f57e4337b387e75a50; NMTID=00O84chCJ4UB0byzElkifyX9WkE3ZcAAAGBKH-xBw; WEVNSM=1.0.0; WNMCID=ujjiij.1654241864835.01.0; WM_TID=SWn5%2BrsBpSdEQBQERBeFA13BO%2BveMgTU; ntes_kaola_ad=1; _iuqxldmzr_=32; WM_NI=XQozOcYzcaCpDN93g4Dj0bPG2wp69WO6yTvymRLrerar51JwsGXjp3%2BTLqVGJk1UAav7LRkHXpt%2B2c4Cm2qxWgL1BX4I54KHEMgdDaPP9Qj%2Bh%2FkxAPZkpby32jBX5JoidnY%3D; WM_NIKE=9ca17ae2e6ffcda170e2e6eed9cc3b91b79f82cb80b2928aa6d85f839f8ab1d149919fa1b5c17b90a800a8db2af0fea7c3b92a9c8fa19bce699ba6ad82c27c8892f7d4e64088b60083d34df2a9c0d7bc5d8deab7b5f573af8e8493e87aa3aefbd2c97db199988bf339e98c8492d54d8ab5ffb6c46bb6e984b3ea449cb0bcccb169f89da3d3f47495b984d2c154f897fbd5ca448793a985c76b9490f7b2e921acf1a599c45c9ab5c083fc39f2b68eb9b842adb7ac8fdc37e2a3; JSESSIONID-WYYY=nbnnzUzR1Ap%2FRJpYqrIns7reQEmyC%2Bpu%2Bg9mQ8nDHajNFTsJkwrGyG5wXKQ1Xy4mkruApNERuC0V3vHukX%5Cmwx%5CEZ8%2FDvH2OjVwCGF1NP7acYy%5C5p%2F4%5CfRksIrli38UzUIsYGvMKeYc5PwmD3ZuqPqZMRw4EOUPX%2BRhd%5CHvhupvEAZrx%3A1659080893357");

                #region 添加Post 参数
                byte[] data = Encoding.UTF8.GetBytes(content);
                req.ContentLength = data.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                    reqStream.Close();
                }
                #endregion

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                //获取响应内容
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return result;
            });
        }
    }

    static class StringExtensions
    {
        public static string ToReverse(this string str)
        {
            char[] arr = str.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }

        public static string ChangeDataToInt(this string strData)
        {
            int iData = 0;
            if (strData.Contains("E"))
            {
                iData = Convert.ToInt32(int.Parse(strData.ToString(), System.Globalization.NumberStyles.Float));
            }
            return iData.ToString();

        }
    }
}
