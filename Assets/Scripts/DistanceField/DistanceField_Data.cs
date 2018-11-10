using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "DistanceField_Data", menuName = "FluidSim/DistanceField", order = 1)]
public class DistanceField_Data : ScriptableObject
{
    public float leftBound = -16f;
    public float rightBound = 16f;
    public float downBound = -16f;
    public float upBound = 16f;
    public float samplesPerUnit = 32f;
    /// <summary>
    /// x, y values encodes the normal. z is unsed for the distance.
    /// </summary>
    public Vector3[] samples;

    public void Bake()
    {
        //Cache some values
        float sampleSize = 1f / samplesPerUnit;
        float halfSampleSize = sampleSize / 2f;
        int nbSamplesX = (int) ((rightBound - leftBound) * samplesPerUnit);
        int nbSamplesY = (int)((upBound - downBound) * samplesPerUnit);

        //Retrieve coliders
        BoxCollider2D[] colliders = FindObjectsOfType<BoxCollider2D>();

        //Compute samples
        samples = new Vector3[nbSamplesX * nbSamplesY];
        int index = 0;
        Vector2 samplePos = new Vector2(leftBound, downBound);
        for (int i = 0; i < nbSamplesX; i++)
        {
            samplePos.x = leftBound + i * sampleSize + halfSampleSize;
            for (int j = 0; j < nbSamplesY; j++)
            {
                samplePos.y = downBound + j * sampleSize + halfSampleSize;
                samples[index++] = ComputeSample(samplePos, colliders);
            }
        }

        //Save samples
        AssetDatabase.Refresh();
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log("Baking Complete !");
    }

    private Vector3 ComputeSample(Vector2 pos, BoxCollider2D[] boxes)
    {
        Vector3 sample = new Vector3(0f, 0f, float.PositiveInfinity);
        List<Vector2> conflictingNormals = new List<Vector2>();

        Vector2 boxCenter, posFromBox, normal;
        foreach (Collider2D box in boxes)
        {
            boxCenter = (box.bounds.max + box.bounds.min) / 2f;
            posFromBox = boxCenter - pos;
            float sdf = BoxSDF(posFromBox, box.bounds.size);

            if (Mathf.Abs(sample.z - sdf) < 0.0001f)
            {
                normal = BoxNormal(pos, box.bounds.min, box.bounds.max);
                conflictingNormals.Add(normal);
            }
            else if (sdf < sample.z)
            {
                sample.z = sdf;

                // TODO Optimize putting that out of the loop
                normal = BoxNormal(pos, box.bounds.min, box.bounds.max);
                conflictingNormals.Clear();
                conflictingNormals.Add(normal);
                sample.x = normal.x;
                sample.y = normal.y;
            }
        }

        normal = conflictingNormals[0];
        for (int i = 1; i < conflictingNormals.Count; i++)
        {
            normal += conflictingNormals[i];
        }
        normal.Normalize();
        sample.x = -normal.x;
        sample.y = -normal.y;
        return sample;
    }

    private float BoxSDF(Vector2 pos, Vector2 size)
    {
        return Mathf.Max(Mathf.Abs(pos.x) - size.x / 2f, Mathf.Abs(pos.y) - size.y / 2f);
    }

    private Vector2 BoxNormal(Vector2 pos, Vector3 min, Vector3 max)
    {
        //Get bounds
        float leftBound = min.x;
        float rightBound = max.x;
        float upBound = max.y;
        float downBound = min.y;

        //case : p is inside de box
        if (pos.x > leftBound && pos.x < rightBound 
            && pos.y > downBound && pos.y < upBound)
        {
            return BoxNormalFromInside(pos, leftBound, rightBound, upBound, downBound);
        }

        //case : In a corner
        if (pos.x < min.x && pos.y < min.y
            || pos.x > max.x && pos.y < min.y
            || pos.x > max.x && pos.y > max.y
            || pos.x < min.x && pos.y > max.y)
        {
            return BOXNormalFromCorner(pos, min, max);
        }

        //Cas : In an edge
        if (pos.x < min.x)
        {
            return Vector2.left;
        }
        if (pos.y < min.y)
        {
            return Vector2.down;
        }
        if (pos.x > max.x)
        {
            return Vector2.right;
        }
        if (pos.y > max.y)
        {
            return Vector2.up;
        }

        return Vector2.zero;
    }

    private Vector2 BOXNormalFromCorner(Vector2 pos, Vector3 min, Vector3 max)
    {
        // Get box corners point CCW from min
        Vector2 p1 = min;
        Vector2 p2 = new Vector2(max.x, min.y);
        Vector2 p3 = max;
        Vector2 p4 = new Vector2(min.x, max.y);

        //TODO Optimise using sqrDist
        // Find min distances with corners
        float distP1 = Vector2.Distance(pos, p1);
        float distP2 = Vector2.Distance(pos, p2);
        float distP3 = Vector2.Distance(pos, p3);
        float distP4 = Vector2.Distance(pos, p4);
        float minDist = distP1;
        if (distP2 < minDist)
        {
            minDist = distP2;
        }
        if (distP3 < minDist)
        {
            minDist = distP3;
        }
        if (distP4 < minDist)
        {
            minDist = distP4;
        }

        // Find closest corner
        Vector2 closest = p1;
        if (distP2 == minDist)
        {
            closest = p2;
        }
        else if (distP3 == minDist)
        {
            closest = p3;
        }
        else if (distP4 == minDist)
        {
            closest = p4;
        }

        //return normal
        return (pos - closest).normalized;
    }

    private static Vector2 BoxNormalFromInside(Vector2 pos, float leftBound, float rightBound, float upBound, float downBound)
    {
        // Find min dist to bound
        float upDist = upBound - pos.y;
        float downDist = pos.y - downBound;
        float rightDist = rightBound - pos.x;
        float leftDist = pos.x - leftBound;
        float minDist = upDist;
        if (downDist < minDist)
        {
            minDist = downDist;
        }
        if (rightDist < minDist)
        {
            minDist = rightDist;
        }
        if (leftDist < minDist)
        {
            minDist = leftDist;
        }

        // return normal according to min dist
        if (downDist == minDist)
        {
            return Vector2.up;
        }
        else if (rightDist == minDist)
        {
            return Vector2.left;
        }
        else if (leftDist == minDist)
        {
            return Vector2.right;
        }
        else
        {
            return Vector2.down;
        }
    }
}
