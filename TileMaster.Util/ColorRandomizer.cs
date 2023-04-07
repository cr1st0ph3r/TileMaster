using System.Drawing;

namespace TileMaster.Util
{
    public static class ColorRandomizer
    {
        //public static string temp = @"C:\Users\Cristopher-PC\Desktop\Tiles\TileMaster 2\TileMaster\Temp";
        public static Random rnd = new Random(DateTime.Now.GetHashCode());
        public static Bitmap RandomTile(List<Color> colors, int width, int height)
        {
            var bmp = new Bitmap(width, height);
            // using (var bmp = new Bitmap(16, 16))
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        //get the pixel from the scrBitmap image
                        var color = colors[rnd.Next(colors.Count)];
                        bmp.SetPixel(i, j, color);
                    }
                }
                //bmp.Save(temp+ @"\"+StringHelper.RandomString(10)+".bmp");
                return bmp;
            }
        }
    }
}
