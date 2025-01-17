﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GBX.NET.Engines.MwFoundations;

namespace GBX.NET
{
    public class GameBoxHeader<T> : GameBoxPart where T : CMwNod
    {
        public ChunkSet Chunks { get; set; }

        public short Version
        {
            get => GBX.Header.Version;
            set => GBX.Header.Version = value;
        }

        public char? ByteFormat
        {
            get => GBX.Header.ByteFormat;
            set => GBX.Header.ByteFormat = value;
        }

        public char? RefTableCompression
        {
            get => GBX.Header.RefTableCompression;
            set => GBX.Header.RefTableCompression = value;
        }

        public char? BodyCompression
        {
            get => GBX.Header.BodyCompression;
            set => GBX.Header.BodyCompression = value;
        }

        public char? UnknownByte
        {
            get => GBX.Header.UnknownByte;
            set => GBX.Header.UnknownByte = value;
        }

        public uint? ID
        {
            get => GBX.Header.ID;
            internal set => GBX.Header.ID = value;
        }

        public byte[] UserData
        {
            get => GBX.Header.UserData;
        }

        public int NumNodes
        {
            get => GBX.Header.NumNodes;
        }

        public GameBoxHeader(GameBox<T> gbx) : base(gbx)
        {

        }

        public void Read(byte[] userData, IProgress<GameBoxReadProgress> progress)
        {
            var gbx = (GameBox<T>)GBX;

            if (Version >= 6)
            {
                if (userData != null && userData.Length > 0)
                {
                    using (var ms = new MemoryStream(userData))
                    using (var r = new GameBoxReader(ms, this))
                    {
                        var numHeaderChunks = r.ReadInt32();

                        Chunks = new ChunkSet();

                        var chunkList = new Dictionary<uint, (int Size, bool IsHeavy)>();

                        for (var i = 0; i < numHeaderChunks; i++)
                        {
                            var chunkID = r.ReadUInt32();
                            var chunkSize = r.ReadUInt32();

                            var chId = chunkID & 0xFFF;
                            var clId = chunkID & 0xFFFFF000;

                            chunkList[clId + chId] = ((int)(chunkSize & ~0x80000000), (chunkSize & (1 << 31)) != 0);
                        }

                        Log.Write("Header data chunk list:");

                        foreach (var c in chunkList)
                        {
                            if (c.Value.IsHeavy)
                                Log.Write($"| 0x{c.Key:X8} | {c.Value.Size} B (Heavy)");
                            else
                                Log.Write($"| 0x{c.Key:X8} | {c.Value.Size} B");
                        }

                        foreach (var chunkInfo in chunkList)
                        {
                            var chunkId = Chunk.Remap(chunkInfo.Key);
                            var nodeId = chunkId & 0xFFFFF000;

                            if (!NodeCacheManager.AvailableClasses.TryGetValue(nodeId, out Type nodeType))
                                Log.Write($"Node ID 0x{nodeId:X8} is not implemented. This occurs only in the header therefore it's not a fatal problem. ({NodeCacheManager.Names.Where(x => x.Key == nodeId).Select(x => x.Value).FirstOrDefault() ?? "unknown class"})");

                            var chunkTypes = new Dictionary<uint, Type>();

                            if (nodeType != null)
                                NodeCacheManager.AvailableHeaderChunkClasses.TryGetValue(nodeType, out chunkTypes);

                            var d = r.ReadBytes(chunkInfo.Value.Size);
                            Chunk chunk = null;

                            if (chunkTypes.TryGetValue(chunkId, out Type type))
                            {
                                var constructor = type.GetConstructors().First();
                                var constructorParams = constructor.GetParameters();
                                if (constructorParams.Length == 0)
                                {
                                    Chunk headerChunk = (Chunk)constructor.Invoke(new object[0]);
                                    headerChunk.Node = gbx.Node;
                                    headerChunk.GBX = GBX;
                                    ((IHeaderChunk)headerChunk).Data = d;
                                    if (d == null || d.Length == 0)
                                        ((IHeaderChunk)headerChunk).Discovered = true;
                                    chunk = (Chunk)headerChunk;
                                }
                                else if (constructorParams.Length == 2)
                                    chunk = (HeaderChunk<T>)constructor.Invoke(new object[] { gbx.Node, d });
                                else throw new ArgumentException($"{type.FullName} has an invalid amount of parameters.");

                                using (var msChunk = new MemoryStream(d))
                                using (var rChunk = new GameBoxReader(msChunk, this))
                                {
                                    var rw = new GameBoxReaderWriter(rChunk);
                                    chunk.ReadWrite(gbx.Node, rw);
                                    ((ISkippableChunk)chunk).Discovered = true;
                                }

                                ((IHeaderChunk)chunk).IsHeavy = chunkInfo.Value.IsHeavy;
                            }
                            else if (nodeType != null)
                                chunk = (Chunk)Activator.CreateInstance(typeof(HeaderChunk<>).MakeGenericType(nodeType), gbx.Node, chunkId, d);
                            else
                                chunk = new HeaderChunk(chunkId, d) { IsHeavy = chunkInfo.Value.IsHeavy };

                            Chunks.Add(chunk);

                            progress?.Report(new GameBoxReadProgress(
                                GameBoxReadProgressStage.HeaderUserData, 
                                r.BaseStream.Position / (float)r.BaseStream.Length, 
                                gbx, 
                                chunk));
                        }
                    }
                }
            }
        }

        public void Write(GameBoxWriter w, int numNodes, IDRemap remap)
        {
            w.Write(GameBox.Magic, StringLengthPrefix.None);
            w.Write(Version);

            if (Version >= 3)
            {
                w.Write((byte)ByteFormat.GetValueOrDefault());
                w.Write((byte)RefTableCompression.GetValueOrDefault());
                w.Write((byte)BodyCompression.GetValueOrDefault());
                if (Version >= 4) w.Write((byte)UnknownByte.GetValueOrDefault());
                w.Write(GBX.ID.GetValueOrDefault());

                if (Version >= 6)
                {
                    if (Chunks == null)
                    {
                        w.Write(0);
                    }
                    else
                    {
                        using (var userData = new MemoryStream())
                        using (var gbxw = new GameBoxWriter(userData, this))
                        {
                            var gbxrw = new GameBoxReaderWriter(gbxw);

                            Dictionary<uint, int> lengths = new Dictionary<uint, int>();

                            foreach (var chunk in Chunks)
                            {
                                chunk.Unknown.Position = 0;

                                var pos = userData.Position;
                                if (((ISkippableChunk)chunk).Discovered)
                                    chunk.ReadWrite(((GameBox<T>)GBX).Node, gbxrw);
                                else
                                    ((ISkippableChunk)chunk).Write(gbxw);

                                lengths[chunk.ID] = (int)(userData.Position - pos);
                            }

                            // Actual data size plus the class id (4 bytes) and each length (4 bytes) plus the number of chunks integer
                            w.Write((int)userData.Length + Chunks.Count * 8 + 4);

                            // Write number of header chunks integer
                            w.Write(Chunks.Count);

                            foreach (Chunk chunk in Chunks)
                            {
                                w.Write(Chunk.Remap(chunk.ID, remap));
                                var length = lengths[chunk.ID];
                                if (((IHeaderChunk)chunk).IsHeavy)
                                    length |= 1 << 31;
                                w.Write(length);
                            }

                            w.Write(userData.ToArray(), 0, (int)userData.Length);
                        }
                    }
                }

                w.Write(numNodes);
            }
        }

        public void Write(GameBoxWriter w, int numNodes)
        {
            Write(w, numNodes, IDRemap.Latest);
        }

        public TChunk CreateChunk<TChunk>(byte[] data) where TChunk : Chunk
        {
            return Chunks.Create<TChunk>(data);
        }

        public TChunk CreateChunk<TChunk>() where TChunk : Chunk
        {
            return CreateChunk<TChunk>(new byte[0]);
        }

        public void InsertChunk(IHeaderChunk chunk)
        {
            Chunks.Add((Chunk)chunk);
        }

        public void DiscoverChunk<TChunk>() where TChunk : IHeaderChunk
        {
            foreach (var chunk in Chunks)
                if (chunk is TChunk c)
                    c.Discover();
        }

        public void DiscoverChunks<TChunk1, TChunk2>() where TChunk1 : IHeaderChunk where TChunk2 : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk1 c1)
                    c1.Discover();
                if (chunk is TChunk2 c2)
                    c2.Discover();
            }
        }

        public void DiscoverChunks<TChunk1, TChunk2, TChunk3>()
            where TChunk1 : IHeaderChunk
            where TChunk2 : IHeaderChunk
            where TChunk3 : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk1 c1)
                    c1.Discover();
                if (chunk is TChunk2 c2)
                    c2.Discover();
                if (chunk is TChunk3 c3)
                    c3.Discover();
            }
        }

        public void DiscoverChunks<TChunk1, TChunk2, TChunk3, TChunk4>()
            where TChunk1 : IHeaderChunk
            where TChunk2 : IHeaderChunk
            where TChunk3 : IHeaderChunk
            where TChunk4 : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk1 c1)
                    c1.Discover();
                if (chunk is TChunk2 c2)
                    c2.Discover();
                if (chunk is TChunk3 c3)
                    c3.Discover();
                if (chunk is TChunk4 c4)
                    c4.Discover();
            }
        }

        public void DiscoverChunks<TChunk1, TChunk2, TChunk3, TChunk4, TChunk5>()
            where TChunk1 : IHeaderChunk
            where TChunk2 : IHeaderChunk
            where TChunk3 : IHeaderChunk
            where TChunk4 : IHeaderChunk
            where TChunk5 : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk1 c1)
                    c1.Discover();
                if (chunk is TChunk2 c2)
                    c2.Discover();
                if (chunk is TChunk3 c3)
                    c3.Discover();
                if (chunk is TChunk4 c4)
                    c4.Discover();
                if (chunk is TChunk5 c5)
                    c5.Discover();
            }
        }

        public void DiscoverChunks<TChunk1, TChunk2, TChunk3, TChunk4, TChunk5, TChunk6>()
            where TChunk1 : IHeaderChunk
            where TChunk2 : IHeaderChunk
            where TChunk3 : IHeaderChunk
            where TChunk4 : IHeaderChunk
            where TChunk5 : IHeaderChunk
            where TChunk6 : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk1 c1)
                    c1.Discover();
                if (chunk is TChunk2 c2)
                    c2.Discover();
                if (chunk is TChunk3 c3)
                    c3.Discover();
                if (chunk is TChunk4 c4)
                    c4.Discover();
                if (chunk is TChunk5 c5)
                    c5.Discover();
                if (chunk is TChunk6 c6)
                    c6.Discover();
            }
        }

        public void DiscoverAllChunks()
        {
            foreach (var chunk in Chunks)
                if (chunk is IHeaderChunk s)
                    s.Discover();
        }

        public TChunk GetChunk<TChunk>() where TChunk : IHeaderChunk
        {
            foreach (var chunk in Chunks)
            {
                if (chunk is TChunk t)
                {
                    t.Discover();
                    return t;
                }
            }
            return default;
        }

        public bool TryGetChunk<TChunk>(out TChunk chunk) where TChunk : IHeaderChunk
        {
            chunk = GetChunk<TChunk>();
            return chunk != null;
        }

        public void RemoveAllChunks()
        {
            Chunks.Clear();
        }

        public bool RemoveChunk<TChunk>() where TChunk : Chunk
        {
            return Chunks.Remove<TChunk>();
        }
    }
}
