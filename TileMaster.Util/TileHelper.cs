
using Newtonsoft.Json;

namespace TileMaster.Util
{
    public static class TileHelper
    {
 
        public static List<TileColor> GetTileColors(string path)
        {
            var tileColors = new List<TileColor>();

            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                var json = File.ReadAllText(file);
                var tc = JsonConvert.DeserializeObject<TileColor>(json);
              tileColors.Add(tc);  
            }

            return tileColors;
        }


        public static void SaveTileColors(TileColor tc,string path) 
        {         
            var json = JsonConvert.SerializeObject(tc);             
            File.WriteAllText(path+@"\Tile" + tc.Id+".json", json);
        }
    }
}
