using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ASGrid
{
    private FluidManager.Particle[] particles = new FluidManager.Particle[1000];

    public void MoveParticle(FluidManager.Particle p)
    {
        if (particles[p.index] == null) particles[p.index] = p;
    }

    public List<FluidManager.Particle> PossibleNeighbors(FluidManager.Particle p)
    {
        return new List<FluidManager.Particle>(particles); ;
    }
}
