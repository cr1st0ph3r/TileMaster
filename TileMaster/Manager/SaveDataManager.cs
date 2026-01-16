using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Map;

namespace TileMaster.Manager
{
    public static class SaveDataManager
    {
        public static int Progress;

        /// <summary>
        /// Saves the map data. Updates the existing archive with active chunks.
        /// </summary>
        public static void SaveGame(WorldData worldData, Dictionary<int, Chunk> activeChunks)
        {
            if (Directory.Exists(Global.SaveDataFolderName) == false)
            {
                Directory.CreateDirectory(Global.SaveDataFolderName);
            }

            var archivePath = Path.Combine(Global.SaveDataFolderName, "map.tlm");
            
            // Open in Update mode to preserve unloaded chunks that are already on disk
            using (var fs = File.Open(archivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                // 1. Save World Data
                var worldEntry = archive.GetEntry("worlddata.json");
                if (worldEntry != null) worldEntry.Delete();
                
                worldEntry = archive.CreateEntry("worlddata.json", CompressionLevel.Optimal);
                using (var entryStream = worldEntry.Open())
                {
                    var options = new JsonSerializerOptions { IncludeFields = true };
                    var worldBytes = JsonSerializer.SerializeToUtf8Bytes(worldData, options);
                    entryStream.Write(worldBytes, 0, worldBytes.Length);
                }

                // 2. Save Active Chunks
                foreach (var kvp in activeChunks)
                {
                    if (kvp.Value == null) continue;

                    string entryName = $"chunks/{kvp.Key}.bin";
                    var chunkEntry = archive.GetEntry(entryName);
                    if (chunkEntry != null) chunkEntry.Delete();

                    chunkEntry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                    using (var stream = chunkEntry.Open())
                    using (var writer = new BinaryWriter(stream))
                    {
                        WriteChunkEntry(writer, kvp.Value);
                    }
                }
            }
        }

        private static void WriteChunkEntry(BinaryWriter writer, Chunk chunk)
        {
            // Chunk Metadata
            writer.Write(chunk.HasGrass);
            
            // Foreground
            WriteTileArray(writer, (BaseTile[])chunk.Tiles, true);
            
            // Background
            WriteTileArray(writer, (BaseTile[])chunk.BackgroundTiles, false);
        }

        private static void WriteTileArray(BinaryWriter writer, BaseTile[] tiles, bool isForeground)
        {
            writer.Write(tiles.Length);
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null || !tile.IsOccupied)
                {
                    writer.Write(false); // IsOccupied
                    continue;
                }

                writer.Write(true); // IsOccupied
                writer.Write((ushort)tile.TileId);
                writer.Write(tile.ColorArgb ?? -1);
                writer.Write(tile.Rotation);

                if (isForeground && tile is CollisionTile ct && ct.PlacedItem != null)
                {
                    writer.Write(true); // HasItem
                    writer.Write(ct.PlacedItem.TileId);
                }
                else
                {
                    writer.Write(false); // HasItem
                }
            }
        }

        /// <summary>
        /// Loads WorldData metadata only. Does NOT load chunks.
        /// </summary>
        public static WorldData LoadGame()
        {
            var archivePath = Path.Combine(Global.SaveDataFolderName, "map.tlm");
            if (!File.Exists(archivePath)) return null;

            using (var fs = File.OpenRead(archivePath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
            {
                var worldEntry = archive.GetEntry("worlddata.json");
                if (worldEntry != null)
                {
                    using (var stream = worldEntry.Open())
                    {
                        var options = new JsonSerializerOptions { IncludeFields = true };
                        return JsonSerializer.Deserialize<WorldData>(stream, options);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Loads a specific chunk asynchronously.
        /// </summary>
        public static Task<Chunk> LoadChunkAsync(int chunkId)
        {
            return Task.Run(() => LoadChunk(chunkId));
        }

        /// <summary>
        /// Loads a specific chunk from the save file.
        /// </summary>
        public static Chunk LoadChunk(int chunkId)
        {
            var archivePath = Path.Combine(Global.SaveDataFolderName, "map.tlm");
            if (!File.Exists(archivePath)) return null;

            using (var fs = File.OpenRead(archivePath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
            {
                var entryName = $"chunks/{chunkId}.bin";
                var entry = archive.GetEntry(entryName);
                if (entry == null) return null;

                using (var stream = entry.Open())
                using (var reader = new BinaryReader(stream))
                {
                    return ReadChunkEntry(reader, chunkId);
                }
            }
        }

        private static Chunk ReadChunkEntry(BinaryReader reader, int chunkId)
        {
            var chunk = new Chunk();
            chunk.PositionOnscreen = chunkId;
            
            // Metadata
            chunk.HasGrass = reader.ReadBoolean();
            chunk.NeedUpdate = true; // Force update when loaded

            // Foreground
            var fgTiles = ReadTileArray(reader, true, chunkId);
            for(int i=0; i<fgTiles.Length; i++)
            {
                if (fgTiles[i] != null) 
                    chunk.Tiles[i] = (CollisionTile)fgTiles[i];
            }

            // Background
            var bgTiles = ReadTileArray(reader, false, chunkId);
            for (int i = 0; i < bgTiles.Length; i++)
            {
                if (bgTiles[i] != null)
                    chunk.BackgroundTiles[i] = (BackgroundTile)bgTiles[i];
            }

            chunk.SetRectangles();
            chunk.InitializeTextures();
            
            return chunk;
        }

        private static BaseTile[] ReadTileArray(BinaryReader reader, bool isForeground, int chunkId)
        {
            int count = reader.ReadInt32();
            var result = new BaseTile[count];

            // Calculate chunk position
            int worldWidthMultiplier = Global.MapWidthMultiplier; // Accessing global as we don't pass it down
            int chunksPerRow = worldWidthMultiplier;
            int chunkX = (chunkId - 1) % chunksPerRow;
            int chunkY = (chunkId - 1) / chunksPerRow;
            int totalMapWidth = worldWidthMultiplier * Global.ChunkSize;

            for (int i = 0; i < count; i++)
            {
                int localX = i % Global.ChunkSize;
                int localY = i / Global.ChunkSize;
                int globalX = chunkX * Global.ChunkSize + localX;
                int globalY = chunkY * Global.ChunkSize + localY;
                int globalId = globalY * totalMapWidth + globalX;

                bool isOccupied = reader.ReadBoolean();
                if (!isOccupied)
                {
                    var airRefTile = Global.ReferenceTiles[(int)TileType.Air];
                    if (isForeground)
                    {
                        result[i] = new CollisionTile
                        {
                            TileId = (int)TileType.Air,
                            textureId = airRefTile.textureId,
                            Name = airRefTile.Name,
                            TextureName = airRefTile.TextureName,
                            IsSolid = airRefTile.IsSolid,
                            IsOccupied = false,
                            ColorArgb = null,
                            Rotation = 0,
                            LocalId = i,
                            GlobalId = globalId,
                            X = globalX,
                            Y = globalY,
                            ChunkId = chunkId,
                            Width = Global.TileSize,
                            Height = Global.TileSize
                        };
                    }
                    else
                    {
                        result[i] = new BackgroundTile
                        {
                            TileId = (int)TileType.Air,
                            textureId = airRefTile.textureId,
                            Name = airRefTile.Name,
                            TextureName = airRefTile.TextureName,
                            IsOccupied = false,
                            ColorArgb = null,
                            Rotation = 0,
                            LocalId = i,
                            GlobalId = globalId,
                            X = globalX,
                            Y = globalY,
                            ChunkId = chunkId,
                            Width = Global.TileSize,
                            Height = Global.TileSize
                        };
                    }
                    continue;
                }
                ushort tileId = reader.ReadUInt16();
                int colorArgb = reader.ReadInt32();
                float rotation = reader.ReadSingle();
                bool hasItem = reader.ReadBoolean();
                int itemId = hasItem ? reader.ReadInt32() : -1;

                var refTile = Global.ReferenceTiles[tileId];
                if (refTile == null)
                {
                    var airRefTile = Global.ReferenceTiles[(int)TileType.Air];
                    if (isForeground)
                    {
                        result[i] = new CollisionTile
                        {
                            TileId = (int)TileType.Air,
                            textureId = airRefTile.textureId,
                            Name = airRefTile.Name,
                            TextureName = airRefTile.TextureName,
                            IsSolid = airRefTile.IsSolid,
                            IsOccupied = false,
                            ColorArgb = null,
                            Rotation = 0,
                            LocalId = i,
                            GlobalId = globalId,
                            X = globalX,
                            Y = globalY,
                            ChunkId = chunkId,
                            Width = Global.TileSize,
                            Height = Global.TileSize
                        };
                    }
                    else
                    {
                        result[i] = new BackgroundTile
                        {
                            TileId = (int)TileType.Air,
                            textureId = airRefTile.textureId,
                            Name = airRefTile.Name,
                            TextureName = airRefTile.TextureName,
                            IsOccupied = false,
                            ColorArgb = null,
                            Rotation = 0,
                            LocalId = i,
                            GlobalId = globalId,
                            X = globalX,
                            Y = globalY,
                            ChunkId = chunkId,
                            Width = Global.TileSize,
                            Height = Global.TileSize
                        };
                    }
                    continue;
                }


                if (isForeground)
                {
                    var ct = new CollisionTile
                    {
                        TileId = tileId,
                        textureId = refTile.textureId,
                        Name = refTile.Name,
                        TextureName = refTile.TextureName,
                        IsSolid = refTile.IsSolid,
                        IsOccupied = true,
                        ColorArgb = colorArgb == -1 ? null : (int?)colorArgb,
                        Rotation = rotation,
                        LocalId = i,
                        GlobalId = globalId,
                        X = globalX,
                        Y = globalY,
                        ChunkId = chunkId,
                        Width = Global.TileSize,
                        Height = Global.TileSize
                    };

                    if (hasItem && itemId != -1 && itemId < Global.Items.Count)
                    {
                        ct.PlacedItem = Global.Items[itemId];
                    }
                    result[i] = ct;
                }
                else
                {
                    var bt = new BackgroundTile
                    {
                        TileId = tileId,
                        textureId = refTile.textureId,
                        Name = refTile.Name,
                        TextureName = refTile.TextureName,
                        IsOccupied = true,
                        ColorArgb = colorArgb == -1 ? null : (int?)colorArgb,
                        Rotation = rotation,
                        LocalId = i,
                        GlobalId = globalId,
                        X = globalX,
                        Y = globalY,
                        ChunkId = chunkId,
                        Width = Global.TileSize,
                        Height = Global.TileSize
                    };
                    result[i] = bt;
                }
            }
            return result;
        }
    }
}