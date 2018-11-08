using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidManager : MonoBehaviour
{
    [SerializeField]
    private float radius;
    private float sqrRadius;
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

    public struct Particle
    {
        public Vector2 pos;
        public Vector2 posprev;
        public Vector2 vel;
        public int hashKey;
        public int index;
    }

    [SerializeField]
    private int nbParticles;
    private List<Particle> particles;
    private List<List<int>> neighbors;
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

    private long extForcesTime;
    private long viscosityTime;
    private long advanceTime;
    private long neighborsTime;
    private long relaxTime;
    private long collisionsTime;
    private long updateVelTime;

    void Start()
    {
        float spawnDelta = 1f;

        particles = new List<Particle>();
        neighbors = new List<List<int>>();
        for (int i = 0; i < nbParticles; i++)
        {
            neighbors.Add(new List<int>());
            Particle particle = new Particle();
            float x = UnityEngine.Random.Range(minx + spawnDelta, maxx - spawnDelta);
            float y = UnityEngine.Random.Range(miny + spawnDelta, maxy - spawnDelta);
            particle.index = i;
            particle.hashKey = -1;
            particle.pos.x = x;
            particle.pos.y = y;
            particle.posprev.x = x;
            particle.posprev.y = y;
            particles.Add(particle);
        }
        grid = new ASGrid();
        distanceField = new DistanceField();
        sqrRadius = radius * radius;
    }

    void FixedUpdate()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        ApplyExternalForces(Time.deltaTime);
        stopwatch.Stop();
        extForcesTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        ApplyViscosity(Time.deltaTime);
        stopwatch.Stop();
        viscosityTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        AdvanceParticles(Time.deltaTime);
        stopwatch.Stop();
        advanceTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        UpdateNeighbors();
        stopwatch.Stop();
        neighborsTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        DoubleDensityRelaxation(Time.deltaTime);
        stopwatch.Stop();
        relaxTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        ResolveCollisions();
        stopwatch.Stop();
        collisionsTime = stopwatch.ElapsedMilliseconds;

        stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        UpdateVelocity(Time.deltaTime);
        stopwatch.Stop();
        updateVelTime = stopwatch.ElapsedMilliseconds;
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
            //Particle p = particles[i];
            foreach (int indexn in neighbors[i])
            {
                Vector2 vpn = particles[indexn].pos - particles[i].pos;
                float velin = Vector2.Dot((particles[i].vel - particles[indexn].vel), vpn);
                if (velin > 0)
                {
                    float length = vpn.magnitude;
                    velin = velin / length;
                    float q = length / radius;
                    Vector2 I = 0.5f * deltaTime * (1f - q) * (sigma * velin + beta * velin * velin) * vpn;
                    Particle p = particles[i];
                    p.vel = particles[i].vel - I;
                    particles[i] = p;
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
            grid.MoveParticle(ref p);
            particles[i] = p;
        }
    }

    private void UpdateNeighbors()
    {
       for (int i = 0; i < nbParticles; i++)
       {
            neighbors[i].Clear();
            //Particle p = particles[i];
            foreach (int indexn in grid.PossibleNeighbors(particles[i]))
            {
                //Particle n = particles[indexn];
                //if (particles[indexn].index != particles[i].index && Vector2.Distance(particles[i].pos, particles[indexn].pos) < radius)
                if (particles[indexn].index != particles[i].index && (particles[indexn].pos - particles[i].pos).sqrMagnitude < sqrRadius)
                    {
                    neighbors[i].Add(indexn);
                }
            }
       }
    }

    private void DoubleDensityRelaxation(float deltaTime)
    {
        for (int i = 0; i < nbParticles; i++)
        {
            //Particle part = particles[i];
            float p = 0f;
            float pnear = 0f;
            foreach (int indexn in neighbors[i])
            {
                float q = 1f - Vector2.Distance(particles[i].pos, particles[indexn].pos) / radius;
                p += q * q;
                pnear += q * q * q;
            }

            float P = k * (p - p0);
            float Pnear = knear * pnear;
            Vector2 delta = Vector2.zero;

            foreach (int indexn in neighbors[i])
            {
                float q = 1f - Vector2.Distance(particles[i].pos, particles[indexn].pos) / radius;
                Vector2 vpn = (particles[i].pos - particles[indexn].pos) / Vector2.Distance(particles[i].pos, particles[indexn].pos);
                Vector2 D = 0.5f * deltaTime * deltaTime * (P * q + Pnear * q * q) * vpn;
                Particle n = particles[indexn];
                n.pos = n.pos + D;
                particles[indexn] = n;
                delta = delta - D;
            }
            Particle part = particles[i];
            part.pos = part.pos + delta;
            particles[i] = part;
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

    private void OnGUI()
    {
        GUIStyle statStyle = new GUIStyle();
        statStyle.fontSize = 30;
        statStyle.normal.textColor = Color.white;
        
        float frameTime = extForcesTime + viscosityTime + advanceTime + neighborsTime + relaxTime + collisionsTime + updateVelTime;
        float time = (float)(frameTime) / 1000f;
        float fps = (float)Math.Round(1f / time, 2);
        GUI.Label(new Rect(10, 10, 100, 50), "fps : " + fps, statStyle);
        GUI.Label(new Rect(170, 10, 100, 50), "(" + time * 1000f + " ms)", statStyle);

        time = (float)(extForcesTime) / 1000f;
        GUI.Label(new Rect(10, 40, 100, 50), "apply forces (" + time * 1000f + " ms)", statStyle);

        time = (float)(viscosityTime) / 1000f;
        GUI.Label(new Rect(10, 70, 100, 50), "apply viscosity (" + time * 1000f + " ms)", statStyle);

        time = (float)(advanceTime) / 1000f;
        GUI.Label(new Rect(10, 100, 100, 50), "advance particles (" + time * 1000f + " ms)", statStyle);

        time = (float)(neighborsTime) / 1000f;
        GUI.Label(new Rect(10, 130, 100, 50), "update neighbors (" + time * 1000f + " ms)", statStyle);

        time = (float)(relaxTime) / 1000f;
        GUI.Label(new Rect(10, 160, 100, 50), "DD relax  (" + time * 1000f + " ms)", statStyle);

        time = (float)(collisionsTime) / 1000f;
        GUI.Label(new Rect(10, 190, 100, 50), "resolve collisions (" + time * 1000f + " ms)", statStyle);

        time = (float)(updateVelTime) / 1000f;
        GUI.Label(new Rect(10, 220, 100, 50), "update velocity (" + time * 1000f + " ms)", statStyle);    
    }
}
