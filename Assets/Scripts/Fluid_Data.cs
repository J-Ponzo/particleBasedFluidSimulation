using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Fluid_Data", menuName = "FluidSim/Fluid", order = 2)]
public class Fluid_Data : ScriptableObject
{
    public float radius;
    public float collisionRadius;
    public float p0;
    public float sigma;
    public float beta;
    public float k;
    public float knear;
}
