﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

using GBX.NET.Engines.MwFoundations;

namespace GBX.NET.Engines.Game
{
    [Node(0x0303F000)]
    public class CGameGhost : CMwNod
    {
        #region Fields

        private readonly Action<Task<Data>> dataExceptionHandle;
        private bool isReplaying;
        private Task<Data> sampleData;

        #endregion

        #region Properties

        public bool IsReplaying
        {
            get => isReplaying;
            set => isReplaying = value;
        }

        public Task<Data> SampleData
        {
            get => sampleData;
            set => sampleData = value;
        }

        #endregion

        #region Constructors

        public CGameGhost()
        {
            dataExceptionHandle = task =>
            {
                if (task.IsFaulted)
                {
                    Log.Write($"\nExceptions while reading ghost data: ({task.Exception.InnerExceptions.Count})", ConsoleColor.Yellow);
                    foreach (var ex in task.Exception.InnerExceptions)
                        Log.Write(ex.ToString());
                }
            };
        }

        #endregion

        #region Chunks

        #region 0x003 chunk

        [Chunk(0x0303F003)]
        public class Chunk0303F003 : Chunk<CGameGhost>
        {
            public int U01;
            public bool U02;
            public int U03;

            public byte[] Data { get; set; }
            public int[] Samples { get; set; }
            public int SamplePeriod { get; set; }

            public override void ReadWrite(CGameGhost n, GameBoxReaderWriter rw)
            {
                Data = rw.Bytes(Data);
                Samples = rw.Array(Samples);

                rw.Int32(ref U01);
                rw.Boolean(ref U02);
                SamplePeriod = rw.Int32(SamplePeriod);
                rw.Int32(ref U03);

                n.SampleData = Task.Run(() =>
                {
                    var ghostData = new Data
                    {
                        SamplePeriod = TimeSpan.FromMilliseconds(SamplePeriod)
                    };

                    using (var ms = new MemoryStream(Data))
                    {
                        ghostData.ReadSamples(ms, Samples.Length, 56);
                    }

                    return ghostData;
                });

                n.SampleData.ContinueWith(n.dataExceptionHandle);
            }
        }

        #endregion

        #region 0x004 chunk

        [Chunk(0x0303F004)]
        public class Chunk0303F004 : Chunk<CGameGhost>
        {
            public int U01;

            public override void ReadWrite(CGameGhost n, GameBoxReaderWriter rw)
            {
                rw.Int32(ref U01); // 0x0A103000
            }
        }

        #endregion

        #region 0x005 chunk

        [Chunk(0x0303F005)]
        public class Chunk0303F005 : Chunk<CGameGhost>
        {
            public int UncompressedSize { get; set; }
            public int CompressedSize { get; set; }
            public byte[] Data { get; set; }

            public override void ReadWrite(CGameGhost n, GameBoxReaderWriter rw)
            { 
                UncompressedSize = rw.Int32(UncompressedSize);
                CompressedSize = rw.Int32(CompressedSize);
                Data = rw.Bytes(Data, CompressedSize);

                if (rw.Mode == GameBoxReaderWriterMode.Read)
                {
                    n.SampleData = Task.Run(() =>
                    {
                        var ghostData = new Data();
                        using (var ms = new MemoryStream(Data))
                            ghostData.Read(ms, true);
                        return ghostData;
                    });

                    n.SampleData.ContinueWith(n.dataExceptionHandle);
                }
            }
        }

        #endregion

        #region 0x006 chunk

        [Chunk(0x0303F006)]
        public class Chunk0303F006 : Chunk<CGameGhost>
        {
            public Chunk0303F005 Chunk005 { get; } = new Chunk0303F005();

            public override void ReadWrite(CGameGhost n, GameBoxReaderWriter rw)
            {
                rw.Boolean(ref n.isReplaying);
                Chunk005.ReadWrite(n, rw);
            }
        }

        #endregion

        #endregion

        #region Other classes

        public class Data
        {
            private TimeSpan samplePeriod;

            /// <summary>
            /// How much time is between each sample.
            /// </summary>
            public TimeSpan SamplePeriod
            {
                get => samplePeriod;
                set
                {
                    samplePeriod = value;

                    if (Samples != null)
                        foreach (var sample in Samples)
                            sample.UpdateTimestamp();
                }
            }

            public ObservableCollection<Sample> Samples { get; private set; }
            public CompressionLevel Compression { get; set; }
            public uint NodeID { get; set; }

            public Data()
            {

            }

            private void Samples_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.OldItems != null)
                    foreach (Sample sample in e.OldItems)
                        sample.AssignTo(null);

                if (e.NewStartingIndex != Samples.Count - 1)
                    foreach (var sample in Samples.Skip(e.NewStartingIndex))
                        sample.AssignTo(this);
            }

            /// <summary>
            /// Read compressed or uncompressed ghost data from <see cref="Stream"/>.
            /// </summary>
            /// <param name="stream">Stream to read from.</param>
            /// <param name="compressed">If stream contains compressed data.</param>
            public void Read(Stream stream, bool compressed)
            {
                if (compressed)
                {
                    using (var zlib = new CompressedStream(stream, CompressionMode.Decompress))
                    {
                        Compression = zlib.Compression;
                        using (var r = new GameBoxReader(zlib))
                            Read(r);
                    }
                }
                else
                {
                    Compression = CompressionLevel.NoCompression;
                    Read(stream);
                }
            }

            /// <summary>
            /// Read uncompressed ghost data from <see cref="Stream"/>.
            /// </summary>
            /// <param name="stream">Stream to read from.</param>
            public void Read(Stream stream)
            {
                using (var r = new GameBoxReader(stream))
                    Read(r);
            }

            /// <summary>
            /// Read uncompressed ghost data with <see cref="GameBoxReader"/>.
            /// </summary>
            /// <param name="r">Reader.</param>
            public void Read(GameBoxReader r)
            {
                NodeID = r.ReadUInt32(); // CSceneVehicleCar or CSceneMobilCharVis
                if (NodeID != uint.MaxValue)
                {
                    var bSkipList2 = r.ReadBoolean();
                    var u01 = r.ReadInt32();
                    SamplePeriod = TimeSpan.FromMilliseconds(r.ReadInt32());
                    var u02 = r.ReadInt32();

                    var sampleData = r.ReadBytes();

                    int sizePerSample = -1;
                    int[] sampleSizes = null;

                    var numSamples = r.ReadInt32();
                    if (numSamples > 0)
                    {
                        var firstSampleOffset = r.ReadInt32();
                        if (numSamples > 1)
                        {
                            sizePerSample = r.ReadInt32();
                            if (sizePerSample == -1)
                            {
                                sampleSizes = r.ReadArray<int>(numSamples - 1);
                            }
                        }
                    }

                    int[] sampleTimes = null;

                    if (!bSkipList2)
                    {
                        sampleTimes = r.ReadArray<int>();
                    }

                    if (numSamples > 0)
                    {
                        using (var mssd = new MemoryStream(sampleData))
                        {
                            ReadSamples(mssd, numSamples, sizePerSample, sampleSizes, sampleTimes);
                        }
                    }
                    else
                    {
                        Samples = new ObservableCollection<Sample>();
                        Samples.CollectionChanged += Samples_CollectionChanged;
                    }
                }
            }

            public void ReadSamples(MemoryStream ms, int numSamples, int sizePerSample, int[] sizesPerSample = null, int[] sampleTimes = null)
            {
                Samples = new ObservableCollection<Sample>();
                Samples.CollectionChanged += Samples_CollectionChanged;

                using (var r = new GameBoxReader(ms))
                {
                    for (var i = 0; i < numSamples; i++)
                    {
                        var sample = new Sample();

                        var sampleProgress = (int)ms.Position;

                        byte[] unknownData;
                        if (sizePerSample != -1)
                            unknownData = new byte[ms.Length / sizePerSample];
                        else if (sizesPerSample != null)
                        {
                            if (i == numSamples - 1) // Last sample size not included
                                unknownData = new byte[(int)(ms.Length - ms.Position)];
                            else
                                unknownData = new byte[sizesPerSample[i]];
                        }
                        else throw new Exception();

                        int? time = null;

                        if (sampleTimes != null)
                            time = sampleTimes[i];

                        switch (NodeID)
                        {
                            case 0x0A02B000: // CSceneVehicleCar
                                var (position, rotation, speed, velocity) = r.ReadTransform();

                                sample.Position = position;
                                sample.Rotation = rotation;
                                sample.Speed = speed * 3.6f;
                                sample.Velocity = velocity;

                                break;
                            case 0x0A401000: // CSceneMobilCharVis
                                var bufferType = r.ReadByte();

                                switch (bufferType)
                                {
                                    case 0:
                                        var unknownData401 = r.ReadBytes(14);
                                        Buffer.BlockCopy(unknownData401, 0, unknownData, 0, unknownData401.Length);

                                        var transform401 = r.ReadTransform();

                                        sample.Position = transform401.position;
                                        sample.Rotation = transform401.rotation;
                                        sample.Speed = transform401.speed * 3.6f;
                                        sample.Velocity = transform401.velocity;

                                        break;
                                    case 1:
                                        break;
                                    default:
                                        break;
                                }

                                sample.BufferType = bufferType;

                                break;
                            default:
                                sample.BufferType = null;
                                break;
                        }

                        sampleProgress = (int)(ms.Position - sampleProgress);

                        if (sizePerSample != -1) // If the sample size is constant
                        {
                            var moreUnknownData = r.ReadBytes(sizePerSample - sampleProgress);
                            Buffer.BlockCopy(moreUnknownData, 0, unknownData, sampleProgress, moreUnknownData.Length);
                        }
                        else if (sizesPerSample != null) // If sample sizes are different
                        {
                            var moreUnknownData = r.ReadBytes(unknownData.Length - sampleProgress);
                            Buffer.BlockCopy(moreUnknownData, 0, unknownData, sampleProgress, moreUnknownData.Length);
                        }
                        else throw new Exception();

                        sample.Unknown = unknownData;

                        Samples.Add(sample);

                        sample.AssignTo(this);
                    }
                }
            }

            /// <summary>
            /// Linearly interpolates <see cref="NET.Sample.Position"/>, <see cref="NET.Sample.Rotation"/> (<see cref="NET.Sample.PitchYawRoll"/>),
            /// <see cref="NET.Sample.Speed"/> and <see cref="NET.Sample.Velocity"/> between two samples. Unknown data is taken from sample A.
            /// </summary>
            /// <param name="timestamp">Any timestamp between the range of samples.</param>
            /// <returns>A new instance of <see cref="Sample"/> that has been linearly interpolated (<see cref="NET.Sample.Timestamp"/> will be null)
            /// or a reference to an existing sample if <paramref name="timestamp"/> matches an existing sample timestamp.
            /// Also returns null if there are no samples, or if <paramref name="timestamp"/> is outside of the sample range,
            /// or <see cref="SamplePeriod"/> is lower or equal to 0.</returns>
            public Sample GetSampleLerp(TimeSpan timestamp)
            {
                if (Samples?.Count > 0 && samplePeriod.Ticks > 0)
                {
                    var sampleKey = timestamp.TotalMilliseconds / samplePeriod.TotalMilliseconds;
                    var a = Samples.ElementAtOrDefault((int)Math.Floor(sampleKey)); // Sample A
                    var b = Samples.ElementAtOrDefault((int)Math.Ceiling(sampleKey)); // Sample B

                    if (a == null) // Timestamp is outside of the range
                        return null;

                    if (b == null || a == b) // There's no second sample to interpolate with
                        return a;

                    var t = (float)(sampleKey - Math.Floor(sampleKey));

                    return new Sample()
                    {
                        Position = AdditionalMath.Lerp(a.Position, b.Position, t),
                        Rotation = AdditionalMath.Lerp(a.Rotation, b.Rotation, t),
                        Speed = AdditionalMath.Lerp(a.Speed, b.Speed, t),
                        Velocity = AdditionalMath.Lerp(a.Velocity, b.Velocity, t),
                        Unknown = a.Unknown
                    };
                }

                return null;
            }

            public class Sample : NET.Sample
            {
                private Data owner;

                internal void AssignTo(Data ghostData)
                {
                    owner = ghostData;

                    if (owner == null || owner.samplePeriod == null || owner.samplePeriod.TotalMilliseconds <= 0)
                    {
                        Timestamp = null;
                        return;
                    }

                    UpdateTimestamp();
                }

                internal void UpdateTimestamp()
                {
                    Timestamp = TimeSpan.FromMilliseconds(owner.samplePeriod.TotalMilliseconds * owner.Samples.IndexOf(this));
                }
            }
        }

        #endregion
    }
}
