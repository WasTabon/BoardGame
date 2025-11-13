using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    public int q; // column
    public int r; // row
    public int s; // diagonal (q + r + s = 0)

    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
        this.s = -q - r;
    }

    public static HexCoordinates[] GetNeighbors(HexCoordinates hex)
    {
        return new HexCoordinates[]
        {
            new HexCoordinates(hex.q + 1, hex.r),     // right
            new HexCoordinates(hex.q + 1, hex.r - 1), // top-right
            new HexCoordinates(hex.q, hex.r - 1),     // top-left
            new HexCoordinates(hex.q - 1, hex.r),     // left
            new HexCoordinates(hex.q - 1, hex.r + 1), // bottom-left
            new HexCoordinates(hex.q, hex.r + 1)      // bottom-right
        };
    }

    public static HexCoordinates[] GetDirections()
    {
        return new HexCoordinates[]
        {
            new HexCoordinates(1, 0),
            new HexCoordinates(1, -1),
            new HexCoordinates(0, -1),
            new HexCoordinates(-1, 0),
            new HexCoordinates(-1, 1),
            new HexCoordinates(0, 1)
        };
    }

    public override bool Equals(object obj)
    {
        if (obj is HexCoordinates other)
            return q == other.q && r == other.r;
        return false;
    }

    public override int GetHashCode()
    {
        return (q * 1000 + r).GetHashCode();
    }

    public static bool operator ==(HexCoordinates a, HexCoordinates b)
    {
        return a.q == b.q && a.r == b.r;
    }

    public static bool operator !=(HexCoordinates a, HexCoordinates b)
    {
        return !(a == b);
    }

    public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
    {
        return new HexCoordinates(a.q + b.q, a.r + b.r);
    }
}