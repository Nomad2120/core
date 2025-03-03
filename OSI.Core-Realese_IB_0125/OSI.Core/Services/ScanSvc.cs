using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OSI.Core.Models.Db;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.Core.Services
{
    public interface IScanSvc
    {
        string ScansFolder { get; }
        Task<Scan> SaveDataToFile(string filename, byte[] data);
        Task DeleteScanById(int id);
        Task<Scan> GetScanById(int id);
        Task<string> GetScanBase64Data(Scan scan);
        Task<string> GetScanByteDataById(int id);
        Task<(string Base64Data, string MimeType)> GetScanData(Scan scan);
    }

    public class ScanSvc : IScanSvc
    {
        public string ScansFolder => Path.Combine(env.WebRootPath, "scans");

        private readonly IWebHostEnvironment env;

        public ScanSvc(IWebHostEnvironment env)
        {
            this.env = env;
            Directory.CreateDirectory(ScansFolder);
        }

        public async Task<Scan> SaveDataToFile(string filename, byte[] data)
        {
            string path = Path.Combine(ScansFolder, filename);
            await File.WriteAllBytesAsync(path, data);

            Scan scan = new Scan()
            {
                FileName = filename
            };
            using var db = OSIBillingDbContext.DbContext;
            db.Scans.Add(scan);
            await db.SaveChangesAsync();

            return scan;
        }

        public async Task<Scan> GetScanById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            var scan = await db.Scans.FirstOrDefaultAsync(u => u.Id == id);
            if (scan == null)
                throw new Exception("Скан не найден");
            return scan;
        }

        public async Task<string> GetScanByteDataById(int id)
        {
            Scan scan = await GetScanById(id);
            string base64 = await GetScanBase64Data(scan);
            return base64;
        }

        public async Task<string> GetScanBase64Data(Scan scan)
        {
            string path = Path.Combine(ScansFolder, scan.FileName);
            string base64 = "";
            if (File.Exists(path))
            {
                byte[] bytes = await File.ReadAllBytesAsync(path);
                base64 = Convert.ToBase64String(bytes);
            }
            return base64;
        }

        private static readonly byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] jpegStartOfImage = { 0xFF, 0xD8 };
        private static readonly byte[] jpegEndOfImage = { 0xFF, 0xD9 };
        private static readonly byte[] bmpHeader = { 0x42, 0x4D };

        public async Task<(string Base64Data, string MimeType)> GetScanData(Scan scan)
        {
            string path = Path.Combine(ScansFolder, scan.FileName);
            if (!File.Exists(path))
                return (null, null);

            byte[] bytes = await File.ReadAllBytesAsync(path);
            var base64 = Convert.ToBase64String(bytes);

            if (bytes.Length > 2 * 1024 * 1024 /* 2Mb */)
                return (base64, null);

            string mimeType = null;

            if (bytes.AsSpan().StartsWith(pngHeader))
            {
                mimeType = "image/png";
            }
            else if (bytes.AsSpan().StartsWith(jpegStartOfImage) && bytes.AsSpan().EndsWith(jpegEndOfImage))
            {
                mimeType = "image/jpeg";
            }
            else if (bytes.AsSpan().StartsWith(bmpHeader) &&
                (BitConverter.IsLittleEndian
                ? BitConverter.ToUInt32(bytes, 2) == bytes.Length
                : BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(bytes, 2)) == bytes.Length))
            {
                mimeType = "image/bmp";
            }

            return (base64, mimeType);
        }

        public async Task DeleteScanById(int id)
        {
            using var db = OSIBillingDbContext.DbContext;
            Scan scan = await db.Scans.FirstOrDefaultAsync(s => s.Id == id);
            // не будем ругаться если документа по магическим причинам не окажется, всё равно же удаляем
            if (scan != null)
                db.Scans.Remove(scan);
        }
    }
}
