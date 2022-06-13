using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace imgv
{
    public class BitmapsourceHelp
    {
        public PictureTypeAndName GetPictureType(string filePath)//filePath是文件的完整路径 
        {
            try
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryReader reader = new BinaryReader(fs);
                string fileClass;
                byte buffer;
                byte[] b = new byte[2];
                buffer = reader.ReadByte();
                b[0] = buffer;
                fileClass = buffer.ToString();
                buffer = reader.ReadByte();
                b[1] = buffer;
                fileClass += buffer.ToString();
                reader.Close();
                fs.Close();
                switch (fileClass)
                {
                    case "255216":
                        return new PictureTypeAndName() { pictureType = PictureType.jpg, name = "jpg" };
                    case "13780":
                        return new PictureTypeAndName() { pictureType = PictureType.png, name = "png" };
                    case "7173":
                        return new PictureTypeAndName() { pictureType = PictureType.gif, name = "gif" };
                    case "6677":
                        return new PictureTypeAndName() { pictureType = PictureType.bmp, name = "bmp" };
                    default:
                        return null;
                }

            }
            catch
            {
                return null;
            }
        }
        public BitmapDecoder GetBitmapDecoder(PictureType type, Stream stream)
        {
            switch (type)
            {
                case PictureType.jpg:
                    return new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                case PictureType.png:
                    return new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default); ;
                case PictureType.gif:
                    return new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                case PictureType.bmp:
                    return new BmpBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                default:
                    return null;
            }
        }
        public BitmapEncoder getBitmapEncoder(PictureType type)
        {
            switch (type)
            {
                case PictureType.jpg:
                    return new JpegBitmapEncoder();
                case PictureType.png:
                    return new PngBitmapEncoder();
                case PictureType.gif:
                    return new GifBitmapEncoder();
                case PictureType.bmp:
                    return new BmpBitmapEncoder();
                default:
                    return null;
            }
        }

        public enum PictureType
        {
            png = 1,
            jpg = 2,
            jpeg = 3,
            gif = 4,
            bmp = 5,
            jp2 = 6,
            webp = 7,
            error = 0
        }
        public class PictureTypeAndName
        {
            public PictureType pictureType { get; set; }
            public string name { get; set; }
        }
    }
}
