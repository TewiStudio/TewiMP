namespace TewiMP.Services.Plugin.BuildInPlugins.Encryptor;

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TewiMP.Services.Plugin;

public class Main : Plugin
{
    public override PluginInfo PluginInfo => new()
    {
        Name = "Encryptor",
        Author = "TewiStudio",
        Version = "0.0",
    };
    protected override Dictionary<string, object> PluginSettings { get; set; } = new()
        {
            { "Encrypt String", "" },
            { "Password", "" },
            { "Decrypt String", "" },
            { "Result", "" },
            { "IV", "" }
        };

    public override void OnEnable()
    {
        base.OnEnable();
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    protected override void OnSettingsChanged(string key, object value)
    {
        base.OnSettingsChanged(key, value);
        if (!string.IsNullOrEmpty(GetSetting<string>("Encrypt String")))
        {
            var (encryptedBase64, ivBase64) = Encrypt(GetSetting<string>("Encrypt String"), "Hellomynameiscn.com1");
            SetSetting("Result", encryptedBase64);
            SetSetting("IV", ivBase64);
            SetSetting("Encrypt String", "");
        }
        else if (!string.IsNullOrEmpty(GetSetting<string>("Decrypt String")) && !string.IsNullOrEmpty(GetSetting<string>("IV")))
        {
            var decryptedText = Decrypt(GetSetting<string>("Decrypt String"), "Hellomynameiscn.com1", GetSetting<string>("IV"));
            SetSetting("Result", decryptedText);
            SetSetting("Decrypt String", "");
            SetSetting("IV", "");
        }
    }

    public static (string EncryptedBase64, string IVBase64) Encrypt(string plainText, string password)
    {
        using var aes = Aes.Create();

        // 生成 256 位密钥
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        // 随机生成 IV
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
            sw.Write(plainText);

        byte[] cipherBytes = ms.ToArray();

        // 返回 Base64 编码的密文和 IV
        string encryptedBase64 = Convert.ToBase64String(cipherBytes);
        string ivBase64 = Convert.ToBase64String(aes.IV);

        return (encryptedBase64, ivBase64);
    }

    public static string Decrypt(string encryptedBase64, string password, string ivBase64)
    {
        var cipherBytes = Convert.FromBase64String(encryptedBase64);
        var ivBytes = Convert.FromBase64String(ivBase64);

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        aes.IV = ivBytes;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
