using System;
using System.Collections.Generic; 

namespace TileMaster.Util
{
    [Serializable]
    public class TileColor
    {
        /// <summary>
        /// The id of the game tile
        /// </summary>
        public int Id { get; set; }
        public IList<ColorData> ColorDatas { get; set; } = new List<ColorData>();

        public TileColor()
        {

        }
  
        public TileColor(int tileId, List<System.Drawing.Color> colors)
        {
            this.Id = tileId;         
            var grouping = colors.GroupBy(x=>x);
            foreach (var colorG in grouping)
            {
                var cd = new ColorData();
                cd.Color = System.Drawing.ColorTranslator.ToHtml(colorG.FirstOrDefault());
                cd.Amount = colorG.Count();
                ColorDatas.Add(cd);
            }
        }

        public List<System.Drawing.Color> BuildColors()
        {
            List<System.Drawing.Color> colors = new List<System.Drawing.Color>();     
            foreach (var cd in ColorDatas)
            {
                for (int i = 0; i < cd.Amount; i++)
                {
                    colors.Add(System.Drawing.ColorTranslator.FromHtml(cd.Color));
                }
               
            }
            return colors;
        }
    }
    public class ColorData {
        public string Color { get; set; }
        /// <summary>
        /// color frequency
        /// </summary>
        public int Amount{ get; set; }
    }
}
