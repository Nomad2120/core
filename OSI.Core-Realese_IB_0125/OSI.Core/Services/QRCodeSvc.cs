using QRCoder;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System;

namespace OSI.Core.Services
{
    public interface IQRCodeSvc
    {
        string GetQRCodeBase64(int pixelsPerModule, string data);
    }

    public class QRCodeSvc : IQRCodeSvc
    {
        public string GetQRCodeBase64(int pixelsPerModule, string data)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            Bitmap qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            // this option will print a logo in the middle of the bar code
            //Bitmap qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.White, (Bitmap)Bitmap.FromFile("E:\\logo.png"));

            MemoryStream memoryStream = new MemoryStream();
            qrCodeImage.Save(memoryStream, ImageFormat.Png);

            // converting to base64
            memoryStream.Position = 0;
            byte[] byteBuffer = memoryStream.ToArray();

            memoryStream.Close();

            string base64String = Convert.ToBase64String(byteBuffer);
            byteBuffer = null;

            return base64String;
            // display the barcode in image
            //Response.Write("<img width='200px' src='data:image/png;base64, " + base64String + "' />");
        }
    }
}
