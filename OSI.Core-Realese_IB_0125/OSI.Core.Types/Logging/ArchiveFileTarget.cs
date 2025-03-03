using ESoft.CommonLibrary;
using NLog;
using NLog.Common;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OSI.Core.Logging
{
    [Target("ArchiveFile")]
    public class ArchiveFileTarget : FileTarget
    {
        private static readonly object ArchiveLock = new();

        private static readonly List<string> Folders = new();
        private static readonly ConcurrentDictionary<string, DateTime> LastArchiveTimes = new();

        public static bool ArchiveSubFolders { get; set; } = false;

        public static byte ArchiveSubFoldersMaxLevel { get; set; } = byte.MaxValue;

        protected override void Write(LogEventInfo logEvent)
        {
            base.Write(logEvent);
            string folder = Path.GetDirectoryName(FileName.Render(logEvent));
            if (!Folders.Contains(folder))
                Folders.Add(folder);
            Task.Run(() => ArchiveOldLogFiles());
        }

        private static void ArchiveOldLogFiles()
        {
            lock (ArchiveLock)
            {
                foreach (var folder in Folders.ToArray().Where(folder => LastArchiveTimes.GetOrAdd(folder, DateTime.MinValue) < DateTime.Today))
                {
                    ArchiveLogFiles(folder);
                    LastArchiveTimes.AddOrUpdate(folder, DateTime.Today, (_, _) => DateTime.Today);
                }
            }
        }

        private static void ArchiveLogFiles(string folder, int level = 0)
        {
            Zip.ArchiveFiles(
                files: Directory.GetFiles(folder).Select(f => new FileInfo(f)).Where(f => f.Extension == ".log" && f.LastWriteTime.Date != DateTime.Today),
                zipFileNameForEachFile: fileName => Path.ChangeExtension(fileName, ".zip"),
                pathInArchiveForEachFile: (string fileName) => "",
                deleteAfterArchiving: true);

            if (ArchiveSubFolders && level < ArchiveSubFoldersMaxLevel)
                foreach (string subFolder in Directory.EnumerateDirectories(folder))
                {
                    ArchiveLogFiles(subFolder, level + 1);
                }
        }
    }
}
