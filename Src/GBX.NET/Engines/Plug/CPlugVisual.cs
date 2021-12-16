﻿namespace GBX.NET.Engines.Plug;

[Node(0x09006000), WritingNotSupported]
public class CPlugVisual : CPlug
{
    protected int flags;
    protected int count;
    protected Vec2[]? texCoords;

    public int Flags
    {
        get => flags;
        set => flags = value;
    }

    public int Count
    {
        get => count;
        set => count = value;
    }

    public Vec2[]? TexCoords
    {
        get => texCoords;
        set => texCoords = value;
    }

    protected CPlugVisual()
    {

    }

    [Chunk(0x09006001)]
    public class Chunk09006001 : Chunk<CPlugVisual>
    {
        public string U01;

        public Chunk09006001()
        {
            U01 = "";
        }

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Id(ref U01!);
        }
    }

    [Chunk(0x09006004)]
    public class Chunk09006004 : Chunk<CPlugVisual>
    {
        public CMwNod? U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.NodeRef(ref U01); // sometimes not present
        }
    }

    [Chunk(0x09006005)]
    public class Chunk09006005 : Chunk<CPlugVisual>
    {
        public int U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Int32(ref U01); // ArchiveCountAndElems, could have binary data with U01 length
        }
    }

    [Chunk(0x09006006)]
    public class Chunk09006006 : Chunk<CPlugVisual>
    {
        public bool U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Boolean(ref U01);
        }
    }

    [Chunk(0x09006007)]
    public class Chunk09006007 : Chunk<CPlugVisual>
    {
        public bool U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Boolean(ref U01);
        }
    }

    [Chunk(0x09006009)]
    public class Chunk09006009 : Chunk<CPlugVisual>
    {
        public float U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Single(ref U01);
        }
    }

    [Chunk(0x0900600A)]
    public class Chunk0900600A : Chunk<CPlugVisual>
    {
        public int U01;
        public int U02;
        public int U03;
        public int U04;
        public int U05;
        public int U06;
        public int U07;
        public int U08;

        public override void Read(CPlugVisual n, GameBoxReader r)
        {
            U01 = r.ReadInt32(); // IsGeometryStatic?
            U02 = r.ReadInt32(); // IsIndexationStatic?
            U03 = r.ReadInt32(); //
            U04 = r.ReadInt32();
            n.count = r.ReadInt32();
            U05 = r.ReadInt32();
            U06 = r.ReadInt32();

            if (U03 == 1)
            {
                n.texCoords = r.ReadArray(n.count, r => r.ReadVec2());
                U07 = r.ReadInt32(); // not correct but works
            }

            U08 = r.ReadInt32();
        }
    }

    [Chunk(0x0900600B)]
    public class Chunk0900600B : Chunk<CPlugVisual>
    {
        public object[]? U01;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            U01 = rw.Array<object>(null, (i, r) => new
            {
                x = r.ReadInt32(),
                y = r.ReadInt32(),
                
                vec1 = r.ReadVec3(), // GmBoxAligned::ArchiveABox
                vec2 = r.ReadVec3()
            }, (x, w) => { });
        }
    }

    [Chunk(0x0900600D)]
    public class Chunk0900600D : Chunk<CPlugVisual>
    {
        private int flags;

        public int Flags
        {
            get => flags;
            set => flags = value;
        }

        public int U01;
        public int U02;

        public float U03;
        public float U04;
        public float U05;
        public float U06;
        public float U07;
        public float U08;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Int32(ref flags);
            // CFastBuffer::GetCount(); - could get from 0x00B

            rw.Int32(ref U01);
            rw.Int32(ref U02);

            var count = rw.Int32(); // could be vertex count

            // Array of node references using 'count'

            // Another array using 'count'

            // if((param_1_00 + 7) & 7) != 0 ----> CPlugVisual::ArchiveSkinData

            U03 = rw.Single(); // ArchiveABox
            U04 = rw.Single();
            U05 = rw.Single();
            U06 = rw.Single();
            U07 = rw.Single();
            U08 = rw.Single();

            // Count + byte array probably
        }
    }

    [Chunk(0x0900600E)]
    public class Chunk0900600E : Chunk<CPlugVisual>
    {
        public int U01;
        public int U02;
        public int U03;

        public override void ReadWrite(CPlugVisual n, GameBoxReaderWriter rw)
        {
            rw.Int32(ref n.flags);
            rw.Int32(ref U01); // 1 works fine, 2 or 3 doesnt

            if (U01 != 1)
            {

            }

            n.count = rw.Int32();

            U02 = rw.Int32(); // array of node refs

            for (var i = 0; i < U01; i++)
            {
                U03 = rw.Int32(); // something flag related

                var textureCoords = rw.Reader!.ReadArray(n.count, r => r.ReadVec2());
            }

            var floats = rw.Array<float>(count: 6); // GmBoxAligned::ArchiveABox
            var someCount = rw.Int32(); // CFastArray::ArchiveCountAndElems
        }
    }
}
