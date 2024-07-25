using System;
using System.IO;
using System.Text;

namespace TewiMP.DataEditor
{
    public static class LogHelper
    {
        static object _lockfile = new();
        public static void WriteLog(string title, string message, bool showDialog = true)
        {
            lock (_lockfile)
            {
                if (showDialog)
                {
                    ShowDialog(title, message);
                }
                var last = File.ReadAllText(DataFolderBase.LogDataPath, Encoding.UTF8);
                string log = $"{last}[{DateTime.Now} | {title}]\n{message}\n\n\n";
                File.WriteAllText(DataFolderBase.LogDataPath, log);
            }
        }

        private static void ShowDialog(string title, string message)
        {
            MainWindow.AddNotify(title, message);
        }
    }
}
