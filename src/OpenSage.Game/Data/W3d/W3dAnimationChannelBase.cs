﻿using System.IO;

namespace OpenSage.Data.W3d
{
    public abstract class W3dAnimationChannelBase
    {
        internal abstract W3dChunkType ChunkType { get; }

        internal abstract void WriteTo(BinaryWriter writer);
    }
}