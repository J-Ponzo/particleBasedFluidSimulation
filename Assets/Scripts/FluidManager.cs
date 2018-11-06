using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidManager : MonoBehaviour
{
    [SerializeField]
    private float radius;
    [SerializeField]
    private float collisionRadius;
    [SerializeField]
    private float p0;
    [SerializeField]
    private float sigma;
    [SerializeField]
    private float beta;
    [SerializeField]
    private float k;
    [SerializeField]
    private float knear;
    [SerializeField]
    private Vector2 gravity;

    public class Particle
    {
        public Vector2 pos;
        public Vector2 posprev;
        public Vector2 vel;
        public int index;
    }

    [SerializeField]
    private int nbParticles;
    private List<Particle> particles;
    private List<List<Particle>> neighbors;
    private ASGrid grid;
    private DistanceField distanceField;

    [SerializeField]
    private float minx;
    [SerializeField]
    private float maxx;
    [SerializeField]
    private float miny;
    [SerializeField]
    private float maxy;

    void Start()
    {
        float spawnDelta = 1f;

        particles = new List<Particle>();
        neighbors = new List<List<Particle>>();
        for (int i = 0; i < nbParticles; i++)
        {
            neighbors.Add(new List<Particle>());
            Particle particle = new Particle();
            float x = UnityEngine.Random.Range(minx + spawnDelta, maxx - spawnDelta);
            float y = UnityEngine.Random.Range(miny + spawnDelta, maxy - spawnDelta);
            particle.index = -1;
            particle.pos.x = x;
            particle.pos.y = y;
            particle.posprev.x = x;
            particle.posprev.y = y;
            particles.Add(particle);
        }
        grid = new ASGrid();
        distanceField = new DistanceField();
    }

    void FixedUpdate()
    {
        ApplyExternalForces(Time.deltaTime);
        ApplyViscosity(Time.deltaTime);
        AdvanceParticles(Time.deltaTime);
        UpdateNeighbors();
        DoubleDensityRelaxation(Time.deltaTime);
        ResolveCollisions();
        UpdateVelocity(Time.deltaTime);
    }

    private void ApplyExternalForces(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            p.vel += gravity;
            p.vel += ForcesFromEnv(p);
            particles[i] = p;
        }
    }

    private Vector2 ForcesFromEnv(Particle p)
    {
        return Vector2.zero;
    }

    private void ApplyViscosity(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            foreach (Particle n in neighbors[i])
            {
                Vector2 vpn = n.pos - p.pos;
                float velin = Vector2.Dot((p.vel - n.vel), vpn);
                if (velin > 0)
                {
                    float length = vpn.magnitude;
                    velin = velin / length;
                    float q = length / radius;
                    Vector2 I = 0.5f * deltaTime * (1f - q) * (sigma * velin + beta * velin * velin) * vpn;
                    p.vel = p.vel - I;
                }
            }
        }
    }

    private void AdvanceParticles(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            p.posprev = p.pos;
            p.pos += deltaTime * p.vel;
            grid.MoveParticle(p);
            particles[i] = p;
        }
    }

    private void UpdateNeighbors()
    {
       for (int i = 0; i < nbParticles; i++)
       {
            neighbors[i].Clear();
            Particle p = particles[i];
            foreach (Particle n in grid.PossibleNeighbors(p))
            {
                if (n != p && Vector2.Distance(p.pos, n.pos) < radius)
                {
                    neighbors[i].Add(n);
                }
            }
       }
    }

    private void DoubleDensityRelaxation(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            Particle part = particles[i];
            float p = 0f;
            float pnear = 0f;
            foreach (Particle n in neighbors[i])
            {
                float q = 1f - Vector2.Distance(part.pos, n.pos) / radius;
                p += q * q;
                pnear += q * q * q;
            }

            float P = k * (p - p0);
            float Pnear = knear * pnear;
            Vector2 delta = Vector2.zero;

            foreach (Particle n in neighbors[i])
            {
                float q = 1f - Vector2.Distance(part.pos, n.pos) / radius;
                Vector2 vpn = (part.pos - n.pos) / Vector2.Distance(part.pos, n.pos);
                Vector2 D = 0.5f * deltaTime * deltaTime * (P * q + Pnear * q * q) * vpn;
                n.pos = n.pos + D;
                delta = delta - D;
            }
            part.pos = part.pos + delta;
        }
    }

    private void ResolveCollisions()
    {
        float friction = 0.03f;
        float collisionSoftness = 0.02f;

        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            int index = distanceField.GetIndex(p.pos);
            if (index != -1)
            {
                float distance = distanceField.GetDistance(index);
                if (distance < collisionRadius)
                {
                    Vector2 vpn = (p.pos - p.posprev) / Vector2.Distance(p.pos, p.posprev);
                    //Vector2 vpn = p.vel; //asumption
                    Vector2 normal = distanceField.GetNormal(index);
                    Vector2 tangent = Vector2.Perpendicular(normal);
                    tangent = Time.deltaTime * friction * Vector2.Dot(vpn, tangent) * tangent;
                    p.pos = p.pos - tangent;
                    p.pos = p.pos - collisionSoftness * (distance + radius) * normal;

                    particles[i] = p;
                }
            }
        }
    }

    private void UpdateVelocity(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            p.vel = (p.pos - p.posprev) / deltaTime;
            particles[i] = p;
        }
    }

    void OnDrawGizmos()
    {
        Vector2 p1 = new Vector2(minx, miny);
        Vector2 p2 = new Vector2(minx, maxy);
        Vector2 p3 = new Vector2(maxx, maxy);
        Vector2 p4 = new Vector2(maxx, miny);
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);

        foreach (Particle particle in particles)
        {
            Gizmos.DrawSphere(particle.pos, 0.1f);
        }
    }

}
