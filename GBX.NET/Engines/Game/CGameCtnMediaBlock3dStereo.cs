﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GBX.NET.Engines.Game
{
    [Node(0x03024000)]
    public class CGameCtnMediaBlock3dStereo : CGameCtnMediaBlock
    {
        public Key[] Keys { get; set; }

        public CGameCtnMediaBlock3dStereo(ILookbackable lookbackable, uint classID) : base(lookbackable, classID)
        {

        }

        [Chunk(0x03024000)]
        public class Chunk03024000 : Chunk<CGameCtnMediaBlock3dStereo>
        {
            public override void ReadWrite(CGameCtnMediaBlock3dStereo n, GameBoxReaderWriter rw)
            {
                n.Keys = rw.Array(n.Keys, i => new Key()
                {
                    Time = rw.Reader.ReadSingle(),
                    UpToMax = rw.Reader.ReadSingle(),
                    ScreenDist = rw.Reader.ReadSingle()
                },
                x =>
                {
                    rw.Writer.Write(x.Time);
                    rw.Writer.Write(x.UpToMax);
                    rw.Writer.Write(x.ScreenDist);
                });
            }
        }

        public class Key : MediaBlockKey
        {
            public float UpToMax { get; set; }
            public float ScreenDist { get; set; }
        }
    }
}