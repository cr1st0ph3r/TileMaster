
using Microsoft.Xna.Framework.Graphics;

namespace TileMaster
{
    public static class Global
    {
        /// <summary>
        /// Defines the size of each tile
        /// </summary>
        public static readonly int Tilesize = 16;
        /// <summary>
        /// Defines the size of each chunk
        /// </summary>
        public static readonly int ChunkSize = 32;
        /// <summary>
        /// Defines the map width multiplier
        /// </summary>
        public static readonly int MapWidthMultiplier =12;
        /// <summary>
        /// for debug purposes
        /// </summary>
        public static bool GenerateMapOnStartup = true;
        /// <summary>
        /// Defines if the game will run in fullscreen mode
        /// </summary>
        public static bool FullScreen = false;
        /// <summary>
        /// X - needs to be a multiple of chunksize
        /// </summary>
        public static int MapWidth = ChunkSize* MapWidthMultiplier;
        /// <summary>
        /// Y - needs to be a multiple of chunksize
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
        /// Defines is the game is in debug mode
        /// </summary>
        public static bool isDebugging = true;

        /// <summary>
        /// Current window width
        /// </summary>
        public static int WindowWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;

        /// <summary>
        /// current window height
        /// </summary>
        public static int WindowHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        //debug variables
        public static bool isCursorOveraButton = false;
        public static bool RenderOnlyPlayerAtChunk = true;

        //this is an experimental feature that have no practical usage
        public static bool UseTileTextureRandomization = true;
        public static bool UseAlternateTiles = true;
        public static int RandomizationFactorAmount = 20;
    }
}
