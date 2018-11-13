using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidManager : MonoBehaviour
{
    private float sqrRadius;
    [SerializeField]
    private Vector2 gravity;
    [SerializeField]
    private float maxSpeed;

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
    private List<Particle> particles = new List<Particle>();
    private List<List<int>> neighbors = new List<List<int>>();

    private ASGrid grid;

    [SerializeField]
    private Fluid_Data fluidData;

    [SerializeField]
    private DistanceField_Data distanceFieldData;
    private ADistanceField distanceField;

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

    private MeshFilter meshFilter;
    [SerializeField]
    private float particleRenderSize;

    void Start()
    {
        float spawnDelta = 1f;

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
        distanceField = new BakedDistanceField(distanceFieldData);
        sqrRadius = fluidData.radius * fluidData.radius;

        InitMesh();
    }

    private void InitMesh()
    {
        Mesh mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        meshFilter.mesh.name = "ParticlesMesh";
        meshFilter.mesh.vertices = new Vector3[nbParticles * 4];
        meshFilter.mesh.uv = new Vector2[nbParticles * 4];
        meshFilter.mesh.triangles = new int[nbParticles * 6];
        UpdateMesh();
    }

    private void UpdateMesh()
    {
        Vector3[] vertices = meshFilter.mesh.vertices;
        Vector2[] uv = meshFilter.mesh.uv;
        int[] triangles = meshFilter.mesh.triangles;

        float offset = particleRenderSize / 2f;
        Vector3 offset00 = new Vector3(-offset, -offset, 0f);
        Vector3 offset10 = new Vector3(offset, -offset, 0f);
        Vector3 offset11 = new Vector3(offset, offset, 0f);
        Vector3 offset01 = new Vector3(-offset, offset, 0f);
        Vector2 particle2d;
        Vector3 particle3d = Vector3.zero;
        Vector2 uv00 = new Vector2(0f, 0f);
        Vector2 uv10 = new Vector2(1f, 0f);
        Vector2 uv11 = new Vector2(1f, 1f);
        Vector2 uv01 = new Vector2(0f, 1f);
        for (int i = 0; i < nbParticles; i++)
        {
            particle2d = particles[i].pos;
            particle3d.x = particle2d.x;
            particle3d.y = particle2d.y;

            vertices[i * 4 + 0] = particle3d + offset00;
            vertices[i * 4 + 1] = particle3d + offset10;
            vertices[i * 4 + 2] = particle3d + offset11;
            vertices[i * 4 + 3] = particle3d + offset01;

            uv[i * 4 + 0] = uv00;
            uv[i * 4 + 1] = uv10;
            uv[i * 4 + 2] = uv11;
            uv[i * 4 + 3] = uv01;

            triangles[i * 6 + 0] = i * 4 + 0;
            triangles[i * 6 + 1] = i * 4 + 2;
            triangles[i * 6 + 2] = i * 4 + 1;
            triangles[i * 6 + 3] = i * 4 + 0;
            triangles[i * 6 + 4] = i * 4 + 3;
            triangles[i * 6 + 5] = i * 4 + 2;
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.uv = uv;
        meshFilter.mesh.triangles = triangles;
    }

    void Update()
    {
        UpdateMesh();   
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

        Render();
    }

    private void Render()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Material material = renderer.sharedMaterial;
        Vector4[] sentParticles = new Vector4[] { particles[0].pos, new Vector4(3, 3, 0, 0) };
        material.SetVectorArray("_Particles", sentParticles);
        material.SetInt("_NbParts", sentParticles.Length);
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
            foreach (int indexn in neighbors[i])
            {
                Vector2 vpn = particles[indexn].pos - particles[i].pos;
                float velin = Vector2.Dot((particles[i].vel - particles[indexn].vel), vpn);
                if (velin > 0)
                {
                    float length = vpn.magnitude;
                    // TODO remove this debug log
                    if (length < 0.0001f) Debug.LogWarning("ApplyViscosity : vpn is null vector");
                    velin = velin / length;
                    float q = length / fluidData.radius;
                    Vector2 I = 0.5f * deltaTime * (1f - q) * (fluidData.sigma * velin + fluidData.beta * velin * velin) * vpn;
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
            if (p.vel.magnitude > maxSpeed)
            {
                p.vel = p.vel.normalized * maxSpeed;
            }
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
            foreach (int indexn in grid.PossibleNeighbors(particles[i]))
            {
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
            float p = 0f;
            float pnear = 0f;
            foreach (int indexn in neighbors[i])
            {
                float q = 1f - Vector2.Distance(particles[i].pos, particles[indexn].pos) / fluidData.radius;
                p += q * q;
                pnear += q * q * q;
            }

            float P = fluidData.k * (p - fluidData.p0);
            float Pnear = fluidData.knear * pnear;
            Vector2 delta = Vector2.zero;

            foreach (int indexn in neighbors[i])
            {
                float q = 1f - Vector2.Distance(particles[i].pos, particles[indexn].pos) / fluidData.radius;
                // TODO remove this debug log
                if (Vector2.Distance(particles[i].pos, particles[indexn].pos) < 0.0001f)
                {
                    Debug.LogWarning("DoubleDensityRelaxation : divid by zero");
                }
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
        float friction = 0.02f;
        float collisionSoftness = 0.1f;

        for (int i = 0; i < nbParticles; i++)
        {
            Particle p = particles[i];
            int index = distanceField.GetIndex(p.pos);
            if (index > -1)
            {
                float distance = distanceField.GetDistance(index);
                if (distance < fluidData.collisionRadius)
                {
                    // TODO remove this debug log
                    if (Vector2.Distance(p.pos, p.posprev) < 0.0001f) Debug.LogWarning(p.index + " : ResolveCollisions : divid by zero");
                    Vector2 vpn = (p.pos - p.posprev) / Vector2.Distance(p.pos, p.posprev);
                    Vector2 normal = distanceField.GetNormal(index).normalized;
                    Vector2 tangent = Vector2.Perpendicular(normal);
                    tangent = Time.deltaTime * friction * Vector2.Dot(vpn, tangent) * tangent;
                    p.pos = p.pos - tangent;
                    p.pos = p.pos - collisionSoftness * (distance + fluidData.collisionRadius) * normal;

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
            if (p.vel.magnitude > maxSpeed)
            {
                p.vel = p.vel.normalized * maxSpeed;
            }
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
            Gizmos.DrawSphere(particle.pos, 0.05f);
            //Gizmos.DrawCube(particle.pos, 0.1f * Vector3.one);
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
