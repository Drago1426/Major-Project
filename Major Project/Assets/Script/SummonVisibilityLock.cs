using UnityEngine;

public static class SummonVisibilityLock
{
    static Object activeOwner;

    public static bool TryClaim(Object owner)
    {
        if (owner == null)
            return false;

        if (activeOwner == null || activeOwner == owner)
        {
            activeOwner = owner;
            return true;
        }

        return false;
    }

    public static void Release(Object owner)
    {
        if (activeOwner == owner)
            activeOwner = null;
    }

    public static void ReleaseAll()
    {
        activeOwner = null;
    }
}
