
using Newtonsoft.Json;

namespace TileMaster.Util
{
    public static class TileHelper
    {
        //change this
        public static string BasePath = @"F:\Workspace\Tile Game\Tiles\TileMaster 2\TileMaster\TileColor";
        public static List<TileColor> GetTileColors()
        {
            var tileColors = new List<TileColor>();

            foreach (string file in Directory.GetFiles(BasePath, "*.json"))
            {
                var json = System.IO.File.ReadAllText(file);
                var tc = JsonConvert.DeserializeObject<TileColor>(json);
              tileColors.Add(tc);  
            }

            return tileColors;
        }


        public static void SaveTileColors(TileColor tc) 
        {         
            var json = JsonConvert.SerializeObject(tc);             
            File.WriteAllText(BasePath+@"\Tile" + tc.Id+".json", json);
        }
    }
}
