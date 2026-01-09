using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using TileMaster.Entity;
using TileMaster.Map;
using static System.Windows.Forms.Design.AxImporter;
using Chunk = TileMaster.Entity.Chunk;

namespace TileMaster.Manager
{
    public static class SaveDataManager
    {
        public static int Progress;

        /// <summary>
        /// Saves the map data into their respective files
        /// </summary>
        public static void SaveGame(WorldData worldData, Dictionary<int, Chunk> chunks)
        {
            if (Directory.Exists(Global.ChunkFolderLocation) == false)
            {
                Directory.CreateDirectory(Global.ChunkFolderLocation);
            }

            // remove existing single-archive if present
            var archivePath = Path.Combine(Global.ChunkFolderLocation, "map.tlm");
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = false
            };

            using (var fs = File.Open(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
            {
                var worldEntry = archive.CreateEntry("worlddata.json", CompressionLevel.Optimal);
                using (var entryStream = worldEntry.Open())
                {
                    var worldBytes = JsonSerializer.SerializeToUtf8Bytes(worldData, options);
                    entryStream.Write(worldBytes, 0, worldBytes.Length);
                }

                var iii = 0;
                foreach (var item in chunks.Values)
                {
                    // Save Foreground Tiles
                    var entry = archive.CreateEntry($"chunk{iii}.json", CompressionLevel.SmallestSize);
                    using (var entryStream = entry.Open())
                    {
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(item.ToBaseTiles(), options);
                        entryStream.Write(bytes, 0, bytes.Length);
                    }

                    // Save Background Tiles
                    if (item.BackgroundTiles != null && item.BackgroundTiles.Count > 0)
                    {
                        var entryBg = archive.CreateEntry($"chunk{iii}_bg.json", CompressionLevel.SmallestSize);
                        using (var entryStreamBg = entryBg.Open())
                        {
                            var bytesBg = JsonSerializer.SerializeToUtf8Bytes(item.ToBaseBGTiles(), options);
                            entryStreamBg.Write(bytesBg, 0, bytesBg.Length);
                        }
                    }

                    iii++;
                }
            }
        }

        /// <summary>
        /// Loads a map from a binary source
        /// </summary>
        /// <param name="content"></param>
        public static WorldData LoadGame()
        {
            var data = new WorldData();
            var options = new JsonSerializerOptions { IncludeFields = true };
            var archivePath = Path.Combine(Global.ChunkFolderLocation, "map.tlm");

            if (File.Exists(archivePath))
            {
                var chunks = new List<Tuple<int, string>>();
                var bgChunks = new List<Tuple<int, string>>();

                using (var fs = File.OpenRead(archivePath))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
                {
                    // 1. Load World Data first
                    var worldEntry = archive.GetEntry("worlddata.json");
                    if (worldEntry != null)
                    {
                        using (var stream = worldEntry.Open())
                        {
                            data = JsonSerializer.Deserialize<WorldData>(stream, options);
                            data.RawMapData = new Dictionary<int, Dictionary<int, CollisionTile>>();
                            data.RawBackgroundData = new Dictionary<int, Dictionary<int, BackgroundTile>>();
                        }
                    }
                    foreach (var entry in archive.Entries)
                    {
                        // Expect entry names like "chunk{n}.json" or similar
                        if (entry.Name.StartsWith("chunk", StringComparison.OrdinalIgnoreCase))
                        {
                            var name = entry.Name;
                            // Check for background file
                            if (name.Contains("_bg"))
                            {
                                var numPart = name.Replace("chunk", "").Replace("_bg.json", "");
                                if (int.TryParse(numPart, out var id))
                                {
                                    bgChunks.Add(new Tuple<int, string>(id, name));
                                }
                            }
                            else
                            {
                                // Foreground file
                                var numPart = name.Replace("chunk", "").Replace(".json", "").Replace(".bin", "");
                                if (int.TryParse(numPart, out var id))
                                {
                                    chunks.Add(new Tuple<int, string>(id, name));
                                }
                            }
                        }
                        // Sort chunks to ensure deterministic order and matching between foreground/background
                        chunks.Sort((a, b) => a.Item1.CompareTo(b.Item1));
                        bgChunks.Sort((a, b) => a.Item1.CompareTo(b.Item1));



                    }


                    var chunkId = 1;

                    // Load Foreground
                    foreach (var file in chunks)
                    {
                        var fgEntry = archive.GetEntry(file.Item2);
                        if (fgEntry == null) continue;                         
                        Dictionary<int, CollisionTile> dict = null;
                        using (var entryStream = fgEntry.Open())
                        {
                            dict = JsonSerializer.Deserialize<Dictionary<int, CollisionTile>>(entryStream, options);
                        }

                        data.RawMapData.Add(chunkId, dict);
                        chunkId++;
                    }

                    // Load Background
                    foreach (var file in bgChunks)
                    {
                        var bgEntry = archive.GetEntry(file.Item2);
                        if (bgEntry == null) continue;                         
                        Dictionary<int, BackgroundTile> dict = null;
                        using (var entryStream = bgEntry.Open())
                        {
                            dict = JsonSerializer.Deserialize<Dictionary<int, BackgroundTile>>(entryStream, options);
                        }     
                        data.RawBackgroundData.Add(file.Item1, dict);
                    }

                    return data;
                }

            }
            return null;
        }
    }
}