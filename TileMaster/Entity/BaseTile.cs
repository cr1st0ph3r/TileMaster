using System.Collections.Generic;

namespace TileMaster.Entity
{
    public class BaseTile
    {
        /// <summary>
        /// The X position of the tile (in tiles)
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// The Y position of the tile (in tiles)
        /// </summary>
        public int Y { get; set; }
        /// <summary>
        /// The Height of the tile (in pixels)
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// The Width of the tile (in pixels)
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// The Id of the texture used by the tile
        /// </summary>
        public int textureId { get; set; }
        /// <summary>
        /// the global identifier of the tile (zero based)
        /// </summary>
        public int GlobalId { get; set; }
        /// <summary>
        /// The chunk which the block belongs to (one based)
        /// </summary>
        public int ChunkId { get; set; }
        /// <summary>
        /// The tile Texture's Id
        /// </summary>
        public int TileId { get; set; }
        /// <summary>
        /// The id of the tile respective to its chunk (local Id) (zero based)
        /// </summary>
        public int LocalId { get; set; }
        /// <summary>
        /// Defines the parent tile in case of multiple tiles of the same kind
        /// </summary>
        public int ParentTileId { get; set; }
        /// <summary>
        /// Denotes if the space is occupied
        /// </summary>
        public bool IsOccupied { get; set; }
        /// <summary>
        /// Denotes if the block is solid
        /// </summary>
        public bool IsSolid { get; set; }
        /// <summary>
        /// Denotes if this tile is on the edge of a chunk
        /// </summary>
        public bool isEdgeTile { get; set; }
        /// <summary>
        /// Denotes if this tile is neighboring a different tile type. Used for blend logic
        /// </summary>
        public bool isNeighboringDifferentTile { get; set; }
        /// <summary>
        /// The name of the tile´s texture
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The texture´s file name
        /// </summary>
        public string TextureName { get; set; }
        /// <summary>
        /// The color filter which MonoGame uses to paint the tile upon drawing. White for no filter.
        /// Kept as a string for serialization backwards-compatibility (existing data uses names).
        /// </summary>
        public string Color { get; set; } = "White";
        /// <summary>
        /// Indicates whether the object is exposed to open air.
        /// </summary>
        public bool isOpenToAir { get; set; }
        /// <summary>
        /// Serialized integer ARGB representation of the runtime color filter.
        /// Storing this allows tiles to be deserialized with the calculated color already present,
        /// avoiding expensive recalculation for off-screen chunks.
        /// Format: (A << 24) | (R << 16) | (G << 8) | B
        /// </summary>
        public int? ColorArgb = null;
        /// <summary>
        /// Rotation in radians. Default 0. Set to MathHelper.ToRadians(90/180/270) to rotate sprite on draw.
        /// </summary>
        public float Rotation { get; set; } = 0f;
        /// <summary>
        /// Gets the collection of neighboring tiles represented as key-value pairs of tile coordinates.
        /// </summary>
        public List<KeyValuePair<int, int>> neighboringTiles;
    }
}
