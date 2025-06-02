[System.Serializable]
public struct GridPosition
{
    public int x;
    public int y;

    public GridPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static GridPosition operator +(GridPosition a, GridPosition b)
        => new GridPosition(a.x + b.x, a.y + b.y);

    public override bool Equals(object obj)
    {
        if (!(obj is GridPosition)) return false;
        GridPosition other = (GridPosition)obj;
        return x == other.x && y == other.y;
    }

    public override int GetHashCode() => x * 1000 + y;
    public static bool operator ==(GridPosition a, GridPosition b) => a.Equals(b);
    public static bool operator !=(GridPosition a, GridPosition b) => !a.Equals(b);
}
