using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASGrid
{
    private float range = 0.2f;
    private List<int>[] hashTable;
    private int hashTableLength = 1023;

    public ASGrid()
    {
        hashTable = new List<int>[hashTableLength];
        for (int i = 0; i < hashTableLength; i++)
        {
            hashTable[i] = new List<int>();
        }
    }

    public void MoveParticle(ref FluidManager.Particle p)
    {
        int x = (int)((p.pos.x / range));
        int y = (int)((p.pos.y / range));
        int key = GetKey(x, y);

        //If p still on same cell, do nothing
        if (key == p.hashKey) return;

        //Else move p to the fine cell
        hashTable[key].Add(p.index);
        if (p.hashKey != -1)
            hashTable[p.hashKey].Remove(p.index);
        p.hashKey = key;
    }

    public List<int> PossibleNeighbors(FluidManager.Particle p)
    {
        List<int> possibleNeighbors = new List<int>();

        int x = (int)(p.pos.x / range);
        int y = (int)(p.pos.y / range);
        int key = GetKey(x, y);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x, y + 1);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x, y - 1);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x + 1, y);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x + 1, y + 1);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x + 1, y - 1);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x - 1, y);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x - 1, y + 1);
        possibleNeighbors.AddRange(hashTable[key]);
        key = GetKey(x - 1, y - 1);
        possibleNeighbors.AddRange(hashTable[key]);

        return possibleNeighbors;
    }

    private int GetKey(int x, int y)
    {
        int lowX = x & 0xFFFF;
        int lowY = y & 0xFFFF;
        int hash = (lowX << 16) | lowY;
        int intKey = hash % hashTableLength;
        if (intKey < 0) intKey += hashTableLength;
        return intKey;
    }
}
