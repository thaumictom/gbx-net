﻿using GBX.NET.Engines.Control;
using System.Collections.Generic;
using System.Linq;

namespace GBX.NET.Engines.Game
{
    [Node(0x030A5000)]
    public class CGameCtnMediaBlockImage : CGameCtnMediaBlock, CGameCtnMediaBlock.IHasKeys
    {
        #region Fields

        private CControlEffectSimi effect = new CControlEffectSimi();
        private FileRef image;

        #endregion

        #region Properties

        IEnumerable<Key> IHasKeys.Keys
        {
            get => Effect.Keys.Cast<Key>();
            set => Effect.Keys = value.Cast<CControlEffectSimi.Key>().ToList();
        }

        [NodeMember]
        public CControlEffectSimi Effect
        {
            get => effect;
            set => effect = value;
        }

        [NodeMember]
        public FileRef Image
        {
            get => image;
            set => image = value;
        }

        #endregion

        #region Chunks

        #region 0x000 chunk

        [Chunk(0x030A5000)]
        public class Chunk030A5000 : Chunk<CGameCtnMediaBlockImage>
        {
            public override void ReadWrite(CGameCtnMediaBlockImage n, GameBoxReaderWriter rw)
            {
                rw.NodeRef<CControlEffectSimi>(ref n.effect);
                rw.FileRef(ref n.image);
            }
        }

        #endregion

        #region 0x001 chunk

        [Chunk(0x030A5001)]
        public class Chunk030A5001 : Chunk<CGameCtnMediaBlockImage>
        {
            public float U01;

            public override void ReadWrite(CGameCtnMediaBlockImage n, GameBoxReaderWriter rw)
            {
                rw.Single(ref U01); // 0.2
            }
        }

        #endregion

        #endregion
    }
}
