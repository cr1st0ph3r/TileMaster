using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using TileMaster.Entity;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Map;

namespace TileMaster.Manager
{
    public class PlayerData
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Layer Layer { get; set; }
        public Dictionary<int, InventoryItem> Inventory { get; set; }
        public Dictionary<int, InventoryItem> ActionBar { get; set; }
    }

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

                // 3. Save Containers
                var containerEntry = archive.GetEntry("containers.json");
                if (containerEntry != null) containerEntry.Delete();

                containerEntry = archive.CreateEntry("containers.json", CompressionLevel.Optimal);
                using (var entryStream = containerEntry.Open())
                {
                    var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
                    var containerBytes = JsonSerializer.SerializeToUtf8Bytes(ContainerManager.Containers, options);
                    entryStream.Write(containerBytes, 0, containerBytes.Length);
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

                bool hasData = tile != null && (tile.TileId != (int)TileType.Air || (tile is CollisionTile ct && (ct.PlacedItem != null || ct.MultiTileOffset != Point.Zero)));

                if (!hasData)
                {
                    writer.Write(false); // IsOccupied (as in "Something other than air is here")
                    continue;
                }

                writer.Write(true); // IsOccupied
                writer.Write((ushort)tile.TileId);
                writer.Write(tile.TextureId);
                writer.Write(tile.ColorArgb ?? -1);
                writer.Write(tile.Rotation);
                writer.Write(tile.IsSlope);
                writer.Write(tile.SlopeRotation);
                writer.Write(tile.MultiTileOffset.X);
                writer.Write(tile.MultiTileOffset.Y);

                if (tile.ContainerId.HasValue)
                {
                    writer.Write(true);
                    writer.Write(tile.ContainerId.Value.ToByteArray());
                }
                else
                {
                    writer.Write(false);
                }

                if (tile is CollisionTile ctl && ctl.PlacedItem != null)
                {
                    writer.Write(true); // HasItem
                    writer.Write(ctl.PlacedItem.Id);

                    if (ctl.PlacedItem.LightColor.HasValue)
                    {
                        writer.Write(true);
                        writer.Write(Tile.PackArgb(ctl.PlacedItem.LightColor.Value));
                    }
                    else
                    {
                        writer.Write(false);
                    }
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
                var containerEntry = archive.GetEntry("containers.json");
                if (containerEntry != null)
                {
                    using (var stream = containerEntry.Open())
                    {
                        var options = new JsonSerializerOptions { IncludeFields = true };
                        var containers = JsonSerializer.Deserialize<Dictionary<System.Guid, Container>>(stream, options);
                        if (containers != null)
                        {
                            ContainerManager.Containers = containers;
                        }
                    }
                }

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
        #region Player Data
        public static void SavePlayerData(Player player)
        {
            if (Directory.Exists(Global.SaveDataFolderName) == false)
            {
                Directory.CreateDirectory(Global.SaveDataFolderName);
            }

            var archivePath = Path.Combine(Global.SaveDataFolderName, "map.tlm");

            using (var fs = File.Open(archivePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                var playerEntry = archive.GetEntry("player.json");
                if (playerEntry != null) playerEntry.Delete();

                playerEntry = archive.CreateEntry("player.json", CompressionLevel.Optimal);
                using (var entryStream = playerEntry.Open())
                {
                    var playerData = new PlayerData
                    {
                        X = player.GetPosition().X,
                        Y = player.GetPosition().Y,
                        Layer = player.Layer,
                        Inventory = player.Inventory,
                        ActionBar = player.ActionBar
                    };

                    var options = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
                    var playerBytes = JsonSerializer.SerializeToUtf8Bytes(playerData, options);
                    entryStream.Write(playerBytes, 0, playerBytes.Length);
                }
            }
        }

        public static PlayerData LoadPlayerData()
        {
            var archivePath = Path.Combine(Global.SaveDataFolderName, "map.tlm");
            if (!File.Exists(archivePath)) return null;

            using (var fs = File.OpenRead(archivePath))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
            {
                var playerEntry = archive.GetEntry("player.json");
                if (playerEntry != null)
                {
                    using (var stream = playerEntry.Open())
                    {
                        var options = new JsonSerializerOptions { IncludeFields = true };
                        return JsonSerializer.Deserialize<PlayerData>(stream, options);
                    }
                }
            }
            return null;
        }
        #endregion

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
            for (int i = 0; i < fgTiles.Length; i++)
            {
                if (fgTiles[i] != null)
                {
                    chunk.Tiles[i] = (CollisionTile)fgTiles[i];
                    if (chunk.Tiles[i].TileId == (int)TileType.Water)
                    {
                        chunk.HasWater = true;
                    }
                }
            }

            // Background
            var bgTiles = ReadTileArray(reader, false, chunkId);
            for (int i = 0; i < bgTiles.Length; i++)
            {
                if (bgTiles[i] != null)
                {
                    chunk.BackgroundTiles[i] = (BackgroundTile)bgTiles[i];
                }
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
                            TextureId = airRefTile.TextureId,
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
                            TextureId = airRefTile.TextureId,
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

                // NEW: Read textureId directly
                int textureId = reader.ReadInt32();

                int colorArgb = reader.ReadInt32();
                float rotation = reader.ReadSingle();
                bool isSlope = reader.ReadBoolean();
                int slopeRotation = reader.ReadInt32();
                int multiTileOffsetX = reader.ReadInt32();
                int multiTileOffsetY = reader.ReadInt32();

                bool hasContainer = reader.ReadBoolean();
                System.Guid? containerId = null;
                if (hasContainer)
                {
                    containerId = new System.Guid(reader.ReadBytes(16));
                }

                bool hasItem = reader.ReadBoolean();
                int itemId = hasItem ? reader.ReadInt32() : -1;

                // NEW: Read Item Properties (unconditional)
                Color? itemLightColor = null;
                if (hasItem)
                {
                    bool hasColor = reader.ReadBoolean();
                    if (hasColor)
                    {
                        itemLightColor = Tile.UnpackArgb(reader.ReadInt32());
                    }
                }

                var refTile = Global.ReferenceTiles[tileId];
                if (refTile == null)
                {
                    var airRefTile = Global.ReferenceTiles[(int)TileType.Air];
                    if (isForeground)
                    {
                        result[i] = new CollisionTile
                        {
                            TileId = (int)TileType.Air,
                            TextureId = airRefTile.TextureId,
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
                            TextureId = airRefTile.TextureId,
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
                        TextureId = textureId, // USE SAVED ID
                        Name = refTile.Name,
                        // Note: Texture and TextureName might be updated by InitializeTextures later based on textureId
                        TextureName = refTile.TextureName,
                        IsSolid = refTile.IsSolid,
                        IsOccupied = refTile.IsOccupied,
                        ColorArgb = colorArgb == -1 ? null : (int?)colorArgb,
                        Rotation = rotation,
                        IsSlope = isSlope,
                        SlopeRotation = slopeRotation,
                        LocalId = i,
                        GlobalId = globalId,
                        X = globalX,
                        Y = globalY,
                        ChunkId = chunkId,
                        Width = Global.TileSize,
                        Height = Global.TileSize,
                        MultiTileOffset = new Point(multiTileOffsetX, multiTileOffsetY),
                        ContainerId = containerId,
                        Hardness = refTile.Hardness
                    };

                    if (hasItem && itemId != -1 && itemId < Global.ReferenceItems.Count)
                    {
                        //TODO: add a cloning solution to prevent missed new properties
                        var templateItem = Global.ReferenceItems[itemId];
                        var newItem = new Item
                        {
                            Id = templateItem.Id,
                            Name = templateItem.Name,
                            IsInteractive = templateItem.IsInteractive,
                            InteractionType = templateItem.InteractionType,
                            Description = templateItem.Description,
                            TextureName = templateItem.TextureName,
                            LightColorName = templateItem.LightColorName,
                            StackSize = templateItem.StackSize,
                            IsTile = templateItem.IsTile,
                            TileId = templateItem.TileId, // ID of the tile this item places
                            Texture = templateItem.Texture,
                            IsPlaceable = templateItem.IsPlaceable,
                            IsLightSource = templateItem.IsLightSource,
                            IsFlickeringLight = templateItem.IsFlickeringLight,
                            LightColor = templateItem.LightColor,
                            LightIntensity = templateItem.LightIntensity,
                            LightRadius = templateItem.LightRadius,
                            Width = templateItem.Width,
                            Height = templateItem.Height,
                            ToolPower = templateItem.ToolPower,
                            IsTool = templateItem.IsTool,
                            UIIcon = templateItem.UIIcon,
                        };

                        if (itemLightColor.HasValue) newItem.LightColor = itemLightColor;

                        ct.PlacedItem = newItem;
                    }
                    result[i] = ct;
                }
                else
                {
                    var bt = new BackgroundTile
                    {
                        TileId = tileId,
                        TextureId = textureId, // USE SAVED ID
                        Name = refTile.Name,
                        TextureName = refTile.TextureName,
                        IsOccupied = refTile.IsOccupied,
                        ColorArgb = colorArgb == -1 ? null : (int?)colorArgb,
                        Rotation = rotation,
                        IsSlope = isSlope,
                        SlopeRotation = slopeRotation,
                        LocalId = i,
                        GlobalId = globalId,
                        X = globalX,
                        Y = globalY,
                        ChunkId = chunkId,
                        Width = Global.TileSize,
                        Height = Global.TileSize,
                        MultiTileOffset = new Point(multiTileOffsetX, multiTileOffsetY),
                        ContainerId = containerId
                    };

                    result[i] = bt;
                }
            }
            return result;
        }
    }
}