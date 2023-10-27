namespace BmsToOsu.Utils;

public static class ListExt
{
    public static T RandomTake<T>(this List<T> list)
    {
        var x = Random.Shared.NextInt64(0, list.Count);
        return list[(int)x];
    }
}