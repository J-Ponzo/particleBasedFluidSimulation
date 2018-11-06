using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceField
{
    private float minx = -10f;
    private float maxx = 10f;
    private float miny = -5f;
    private float maxy = 5f;

    private int nbParticules = 10000;
    private int index = 0;
    private Vector2[] savedPoses;

    public DistanceField()
    {
        savedPoses = new Vector2[nbParticules];
    }

    public int GetIndex(Vector2 pos)
    {
        savedPoses[index] = pos;
        int savedIndex = index;
        index = (index + 1) % nbParticules;
        return savedIndex;
    }

    public float GetDistance(int index)
    {
        Vector2 pos = savedPoses[index];
        float topDist = maxy - pos.y;
        float bottomDist = pos.y - miny;
        float rightDist = maxx - pos.x;
        float leftDist = pos.x - minx;

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

    public Vector2 GetNormal(int index)
    {
        Vector2 pos = savedPoses[index];
        float topDist = maxy - pos.y;
        float bottomDist = pos.y - miny;
        float rightDist = maxx - pos.x;
        float leftDist = pos.x - minx;
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
