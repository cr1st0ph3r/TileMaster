using Microsoft.Xna.Framework.Graphics;

namespace TileMaster
{
    public static class Global
    {
        /// <summary>
        /// Defines the size of each tile
        /// </summary>
        public static readonly int TileSize = 16;

        /// <summary>
        /// Defines the size of each chunk
        /// </summary>
        public static readonly int ChunkSize = 32;

        /// <summary>
        /// Defines the map width multiplier
        /// </summary>
        public static readonly int MapWidthMultiplier = 12;

        /// <summary>
        /// Defines if the game will run in full screen mode
        /// </summary>
        public static bool FullScreen = false;

        /// <summary>
        /// defines if the map is loaded or not
        /// </summary>
        public static bool IsMapLoaded = false;

        /// <summary>
        /// X - needs to be a multiple of chunkSize
        /// </summary>
        public static int MapWidth = ChunkSize * MapWidthMultiplier;

        /// <summary>
        /// Y - needs to be a multiple of chunkSize
        /// </summary>
        public static int MapHeight = ChunkSize * MapWidthMultiplier;

        /// <summary>
        /// defines the layers where grass should be planted on map generation
        /// </summary>
        public static int GroundLevel = (int)(MapHeight * 0.3);

        /// <summary>
        /// Defines where the grass is allowed to grow (world generation)
        /// </summary>
        public static int GrassLevel = GroundLevel + 5;

        /// <summary>
        /// Defines where the rock is allowed to spawn (world generation)
        /// </summary>
        public static int RockLevel = (int)(MapHeight * 0.4);

        /// <summary>
        /// Variable to store the game's current framerate
        /// </summary>
        public static string FrameRate;

        /// <summary>
        /// Current window width
        /// </summary>
        public static int WindowWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width-100;

        /// <summary>
        /// current window height
        /// </summary>
        public static int WindowHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height-100;

        /// <summary>
        /// Mouse coordinates data
        /// </summary>
        public static int CursorX = 0;
        public static int CursorY = 0;

        //debug variables
        public static bool isCursorOverAButton = false;
        public static bool RenderOnlyPlayerAtChunk = true;
        /// <summary>
        /// Defines if the game is in debug mode
        /// </summary>
        public static bool isDebugging = true;
        /// <summary>
        /// Defines if the game should update the player chunk only
        /// </summary>
        public static bool updatePlayerChunkOnly = true;
        /// <summary>
        /// Paints the tiles at the edge of a chunk of a different color
        /// </summary>
        public static bool MarkTilesOnTheEdge = false;


        //this is an experimental feature that have no practical usage
        public static bool UseTileTextureRandomization = false;
        public static bool UseAlternateTiles = true;
        public static int RandomizationFactorAmount = 20;

        //Files and folders locations
        public static readonly string ChunkFolderLocation = "Chunks";
        public static readonly string MapDataLocation = @"Chunks\data.bin";
        public static readonly string TileDataLocation = @"Data\Tiles.json";
        public static readonly string TileColorDataLocation = @"Data\TileColor";
    }
}
