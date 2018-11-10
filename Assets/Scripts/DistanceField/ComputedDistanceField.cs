using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ComputedDistanceField : ADistanceField
{
    private int nbParticules = 1024;
    private int index = 0;
    private Vector2[] savedPoses;

    public ComputedDistanceField(DistanceField_Data data)
    {
        base.data = data;
        savedPoses = new Vector2[nbParticules];
    }

    public override int GetIndex(Vector2 pos)
    {
        savedPoses[index] = pos;
        int savedIndex = index;
        index = (index + 1) % nbParticules;
        return savedIndex;
    }

    public override float GetDistance(int index)
    {
        Vector2 pos = savedPoses[index];
        float topDist = base.data.upBound - pos.y;
        float bottomDist = pos.y - base.data.downBound;
        float rightDist = base.data.rightBound - pos.x;
        float leftDist = pos.x - base.data.leftBound;

        float dist = topDist;
        if (bottomDist < dist)
        {
            dist = bottomDist;
        }
        if (rightDist < dist)
        {
            dist = rightDist;
        }
        if (leftDist < dist)
        {
            dist = leftDist;
        }

        return dist;
    }

    public override Vector2 GetNormal(int index)
    {
        Vector2 pos = savedPoses[index];
        float topDist = base.data.upBound - pos.y;
        float bottomDist = pos.y - base.data.downBound;
        float rightDist = base.data.rightBound - pos.x;
        float leftDist = pos.x - base.data.leftBound;
        float dist = topDist;
        if (bottomDist < dist)
        {
            dist = bottomDist;
        }
        if (rightDist < dist)
        {
            dist = rightDist;
        }
        if (leftDist < dist)
        {
            dist = leftDist;
        }

        if (bottomDist == dist)
        {
            return Vector2.up * Mathf.Sign(-dist);
        }
        else if(rightDist == dist)
        {
            return Vector2.left * Mathf.Sign(-dist);
        }
        else if(leftDist == dist)
        {
            return Vector2.right * Mathf.Sign(-dist);
        }
        else
        {
            return Vector2.down * Mathf.Sign(-dist);
        }
    }
}
