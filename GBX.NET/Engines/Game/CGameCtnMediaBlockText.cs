﻿using GBX.NET.Engines.Control;
using System.Collections.Generic;
using System.Linq;

namespace GBX.NET.Engines.Game
{
    [Node(0x030A8000)]
    public class CGameCtnMediaBlockText : CGameCtnMediaBlock, CGameCtnMediaBlock.IHasKeys
    {
        private string text;
        private CControlEffectSimi effect = new CControlEffectSimi();
        private Vec3 color;

        #region Properties

        IEnumerable<Key> IHasKeys.Keys
        {
            get => Effect.Keys.Cast<Key>();
            set => Effect.Keys = value.Cast<CControlEffectSimi.Key>().ToList();
        }

        [NodeMember]
        public string Text
        {
            get => text;
            set => text = value;
        }

        [NodeMember]
        public CControlEffectSimi Effect
        {
            get => effect;
            set => effect = value;
        }

        [NodeMember]
        public Vec3 Color
        {
            get => color;
            set => color = value;
        }

        #endregion

        #region Chunks

        #region 0x001 chunk (text)

        [Chunk(0x030A8001, "text")]
        public class Chunk030A8001 : Chunk<CGameCtnMediaBlockText>
        {
            public override void ReadWrite(CGameCtnMediaBlockText n, GameBoxReaderWriter rw)
            {
                rw.String(ref n.text);
                rw.NodeRef<CControlEffectSimi>(ref n.effect);
            }
        }

        #endregion

        #region 0x002 chunk (color)

        [Chunk(0x030A8002, "color")]
        public class Chunk030A8002 : Chunk<CGameCtnMediaBlockText>
        {
            public override void ReadWrite(CGameCtnMediaBlockText n, GameBoxReaderWriter rw)
            {
                rw.Vec3(ref n.color);
            }
        }

        #endregion

        #region 0x003 chunk

        [Chunk(0x030A8003)]
        public class Chunk030A8003 : Chunk<CGameCtnMediaBlockText>
        {
            public float U01;

            public override void ReadWrite(CGameCtnMediaBlockText n, GameBoxReaderWriter rw)
            {
                rw.Single(ref U01); // 0.2
            }
        }

        #endregion

        #endregion
    }
}