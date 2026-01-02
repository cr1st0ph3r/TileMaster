using System.Drawing.Imaging;
using TileMaster.Util;

namespace TileRandomizer
{
    public partial class MainForm : Form
    {
        public static readonly string ContentFolder = @"C:\Users\Cristopher-PC\Desktop\Tiles\TileMaster 2\TileMaster\Content";
        public Dictionary<string, string> files;
        public static Random rnd = new Random(DateTime.Now.GetHashCode());
        List<Color> colors;
        Dictionary<Color,int > colorDict;
        public MainForm()
        {
            InitializeComponent();
            files = new Dictionary<string, string>();

            var ext = new List<string> { "png" };
            var myFiles = Directory
                .EnumerateFiles(ContentFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));
            foreach (var fdi in myFiles)
            {
                FileInfo fi = new FileInfo(fdi);
                files.Add(fi.Name, fdi);
            }

            lbFiles.DataSource = files.Select(x => x.Key).ToList();
            pbImage.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            pbNewImage.Interpolation = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        }

        private void lbFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            colors = new List<Color>();
            colorDict = new Dictionary<Color,int>();
            pbImage.Image = Image.FromFile(files.ElementAt(lbFiles.SelectedIndex).Value);

            var myBitmap = new Bitmap(files.ElementAt(lbFiles.SelectedIndex).Value);

            for (int x = 0; x < myBitmap.Width; x++)
            {
                for (int y = 0; y < myBitmap.Height; y++)
                {
                    // Get the color of a pixel within myBitmap.
                    colors.Add(myBitmap.GetPixel(x, y));
                    //if(colorDict.ContainsKey(myBitmap.GetPixel(x, y)))
                    //{

                    //}
                    //else
                    //{
                    //    colorDict.Add(myBitmap.GetPixel(x, y), 1);
                    //}
                }
            }

            lblUnique.Text = colors.Distinct().Count() + " Unique Colors";

            lblColors.Text = colors.Count + " Colors";
        }

        private void btnRandomize_Click(object sender, EventArgs e)
        { 
            pbNewImage.Image = TileMaster.Util.ColorRandomizer.RandomTile(colors, 16, 16);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            pbNewImage.Image.Save("NewTile",ImageFormat.Png);
        }

        private void btnSaveTileColorData_Click(object sender, EventArgs e)
        {
            TileColor tc = new TileColor(Convert.ToInt32(txtTileId.Text), colors);
            //TODO add the path to Data/TileColor
            TileHelper.SaveTileColors(tc,"");
        }
    }
}