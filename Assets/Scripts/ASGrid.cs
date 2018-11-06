using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASGrid
{
    //private FluidManager.Particle[] particles = new FluidManager.Particle[1000];

    private float range = 0.2f;
    private List<FluidManager.Particle>[] hashTable;
    private int hashTableLength = 10000;

    public ASGrid()
    {
        hashTable = new List<FluidManager.Particle>[hashTableLength];
        for (int i = 0; i < hashTableLength; i++)
        {
            hashTable[i] = new List<FluidManager.Particle>();
        }
    }

    public void MoveParticle(FluidManager.Particle p)
    {
        int x = (int)((p.pos.x / range));
        int y = (int)((p.pos.y / range));
        int key = GetKey(x, y);

        //If p still on same cell, do nothing
        if (key == p.index) return;

        //Else move p to the fine cell
        hashTable[key].Add(p);
        if (p.index != -1) hashTable[p.index].Remove(p);
        p.index = key;
    }

    public List<FluidManager.Particle> PossibleNeighbors(FluidManager.Particle p)
    {
        List<FluidManager.Particle> possibleNeighbors = new List<FluidManager.Particle>();

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
