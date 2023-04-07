
using Microsoft.Xna.Framework.Graphics;

namespace TileMaster
{
    public static class Global
    {
        public static readonly int Tilesize = 16;
        public static readonly int ChunkSize = 32;
        public static readonly int MapWidthMultiplier =12;
        /// <summary>
        /// for debug purposes
        /// </summary>
        public static bool GenerateMapOnStartup = true;
        public static bool FullScreen = false;
        /// <summary>
        /// X - needs to be a multiple of chunksive
        /// </summary>
        public static int MapWidth = ChunkSize* MapWidthMultiplier;
        /// <summary>
        /// Y - needs to be a multiple of chunksive
        /// </summary>
        public static int MapHeight = ChunkSize * MapWidthMultiplier;
        /// <summary>
        /// defines the layers where grass should be planted on map generation
        /// </summary>
        public static int GroundLevel = (int)(MapHeight * 0.3);
        public static int GrassLevel = GroundLevel + 5;
        public static int RockLevel = (int)(MapHeight * 0.4);
        public static bool isDebugging = true;

        public static int WindowWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        public static int WindowHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        public static bool isCursorOveraButton = false;
        public static bool RenderOnlyPlayerAtChunk = false;

        public static bool UseTileTextureRandomization = true;
        public static bool UseAlternateTiles = true;
        public static int RandomizationFactorAmount = 20;
    }
}
