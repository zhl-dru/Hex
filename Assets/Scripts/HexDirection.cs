/// <summary>
/// 规定方向
/// </summary>
public enum HexDirection
{
    /// <summary>
    /// 东北
    /// </summary>
    NE,
    /// <summary>
    /// 东
    /// </summary>
    E,
    /// <summary>
    /// 东南
    /// </summary>
    SE,
    /// <summary>
    /// 西南
    /// </summary>
    SW,
    /// <summary>
    /// 西
    /// </summary>
    W,
    /// <summary>
    /// 西北
    /// </summary>
    NW
}

/// <summary>
/// 与方向有关的扩展方法
/// </summary>
public static class HexDirectionExtensions
{
    /// <summary>
    /// 相反方向
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns>返回一个与输入方向相反的方向</returns>
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    /// <summary>
    /// 顺时针上一个方向
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns>返回输入方向在顺时针的上一个方向</returns>
    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    /// <summary>
    /// 顺时针下一个方向
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns>返回输入方向在顺时针的下一个方向</returns>
    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }
    /// <summary>
    /// 顺时针上个方向的上个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }
    /// <summary>
    /// 顺时针下个方向的下个方向
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}