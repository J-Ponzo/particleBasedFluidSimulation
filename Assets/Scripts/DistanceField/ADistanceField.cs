using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ADistanceField
{
    protected DistanceField_Data data;

    public abstract int GetIndex(Vector2 pos);

    public abstract float GetDistance(int index);

    public abstract Vector2 GetNormal(int index);
}
