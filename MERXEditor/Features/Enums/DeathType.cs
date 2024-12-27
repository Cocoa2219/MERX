using System;

namespace MERX.Features.Enums
{
    [Flags]
    public enum DeathType
    {
        None = 0,
        Destroy = 1 << 0,
        Broadcast = 1 << 1,
        SpawnItem = 1 << 2,
        Explode = 1 << 3,
        PlaySound = 1 << 4,
        Dynamic = 1 << 5,
    }
}