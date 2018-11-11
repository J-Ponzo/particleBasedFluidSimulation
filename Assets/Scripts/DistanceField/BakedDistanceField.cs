using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakedDistanceField : ADistanceField
{
    private float sampleSize;
    private float halfSampleSize;
    private int nbSamplesX;
    private int nbSamplesY;

    public BakedDistanceField(DistanceField_Data data)
    {
        base.data = data;
        sampleSize = 1f / data.samplesPerUnit;
        halfSampleSize = sampleSize / 2f;
        nbSamplesX = (int)((data.rightBound - data.leftBound) * data.samplesPerUnit);
        nbSamplesY = (int)((data.upBound - data.downBound) * data.samplesPerUnit);
    }

    public override int GetIndex(Vector2 pos)
    {
        if (pos.x < base.data.leftBound + halfSampleSize + 0.0001f
            || pos.x > base.data.rightBound - halfSampleSize - 0.0001f
            || pos.y < base.data.downBound - halfSampleSize - 0.0001f
            || pos.y > base.data.upBound + halfSampleSize + 0.0001f)
        {
            return -1;
        }

        int x = (int) ((pos.x - base.data.leftBound + halfSampleSize) / sampleSize);
        int y = (int) ((pos.y - base.data.downBound + halfSampleSize) / sampleSize);

        //return x * nbSamplesY + y;
        // TODO remove this debug log
        int index = x * nbSamplesY + y;
        if (index >= data.samples.Length || index < 0)
        {
            Debug.LogError("index not existing");
        }
        return index;
    }

    public override float GetDistance(int index)
    {
        // TODO remove this debug log
        if (index >= data.samples.Length || index < 0)
        {
            Debug.LogError("index not existing");
        }
        return data.samples[index].z;
    }

    public override Vector2 GetNormal(int index)
    {
        return data.samples[index];
    }
}
