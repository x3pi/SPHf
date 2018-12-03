using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PBF : MonoBehaviour
{

    public List<Particle> particles = new List<Particle>();
    public List<Particle> grid = new List<Particle>();
    public float[] gridOffsets;
    public float horizontalCells = 0;
    public int iteration = 0;
    public float timestep = 1 / 60;
    public int solverIterations = 3;
    public float restDensity = 6000;

    public float particleMass = 1;
    public float kernelRadius = 0.1f;
    public float epsilon = 300;
    public float numParticles = 400;

    public Vector2 gravity = new Vector2(0, 1);

    // Use this for initialization
    void Start()
    {
        initializeParticles();



    }

    // Update is called once per frame
    void Update()
    {

    }

    // Khởi tạo các hạt
    void initializeParticles()
    {
        //var Particle p = 
        for (var i = 0; i < numParticles; i++)
        {
            particles.Add(new Particle(i, new Vector2(Random.value, Random.value / 2), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new List<Particle>(), 0, 0));
        }
    }
    // Tạo lưới
    void createSpatialHashingGrid()
    {
        horizontalCells = Mathf.Ceil(1.0f / kernelRadius);
        int size = (int)(horizontalCells * horizontalCells);
        float[] gridSums = new float[size];
        grid = new List<Particle>(size);
        gridOffsets = new float[size];


        for (int i = 0; i < numParticles; i++)
        {
            int x = (int)Mathf.Max(Mathf.Min(Mathf.Floor(particles[i].Pos[0] / kernelRadius), horizontalCells - 1), 0);
            var y = (int)Mathf.Max(Mathf.Min(Mathf.Floor(particles[i].Pos[1] / kernelRadius), horizontalCells - 1), 0);
            int bin = (int)(x + horizontalCells * y);
            gridSums[(int)bin]++;
        }

        for (int i = 1; i < gridSums.Length; i++)
        {
            gridOffsets[i] = gridOffsets[i - 1] + gridSums[i - 1];
        }

        for (int i = 0; i < numParticles; i++)
        {
            int x = (int)Mathf.Max(Mathf.Min(Mathf.Floor(particles[i].Pos[0] / kernelRadius), horizontalCells - 1), 0);
            int y = (int)Mathf.Max(Mathf.Min(Mathf.Floor(particles[i].Pos[1] / kernelRadius), horizontalCells - 1), 0);
            int bin = (int)(x + horizontalCells * y);
            gridSums[bin]--;
            grid[(int)((gridSums[bin]) + gridOffsets[bin])] = particles[i];
        }
    }

    // poly6
    float poly6(Vector2 p1, Vector2 p2)
    {
        Vector2 r = p1 - p2;

        float result = (float)(315.0 / (64.0 * Mathf.PI * Mathf.Pow(kernelRadius, 9)) * Mathf.Pow(kernelRadius * kernelRadius - r.magnitude * r.magnitude, 3));
        return result;
    }

    // Trả về độ dốc của hạt nhân gai

    Vector2 spiky(Vector2 p1, Vector2 p2)
    {
        Vector2 r = p1 - p2;

        if (r.magnitude > kernelRadius || r.magnitude == 0)
        {
            return new Vector2(0, 0);
        }

        float result = (float)(-45.0 / (Mathf.PI * Mathf.Pow(kernelRadius, 6)) * Mathf.Pow(kernelRadius * kernelRadius - r.magnitude * r.magnitude, 2) * 1 / r.magnitude);
        Vector2.Scale(r, new Vector2(result, result));
        return r;
    }

    /* Khởi tạo các vị trí tạm thời của các hạt.*/
    void predictPositions()
    {
        particles.ForEach((p1) =>
        {
            p1.Vel = p1.Vel + Vector2.Scale(gravity, new Vector2(timestep, timestep));
            p1.NewPos = p1.NewPos + Vector2.Scale(p1.Vel, new Vector2(timestep, timestep));
        });
    }

    /*Tìm hạt hàng xóm */


    void updateNeighbours()
    {
        particles.ForEach((p1) =>
        {
            List<Particle> neighbours = new List<Particle>();
            int x = (int)Mathf.Max(Mathf.Min(Mathf.Floor(p1.Pos[0] / kernelRadius), horizontalCells - 1), 0);
            int y = (int)Mathf.Max(Mathf.Min(Mathf.Floor(p1.Pos[1] / kernelRadius), horizontalCells - 1), 0);

            float kernelRadius2 = kernelRadius * kernelRadius;



            int[,] masks;
            masks = new int[,] { { 0, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 }
            , { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 } };





            for (int i = 0; i < masks.Length; i++)
            {
                int newX = masks[i, 0] + x;
                int newY = masks[i, 1] + y;

                if (newX >= 0 && newY >= 0 && newX < horizontalCells && newY < horizontalCells)
                {
                    int bin = (int)(newX + newY * horizontalCells);
                    float limit = bin < gridOffsets.Length - 1 ? gridOffsets[bin + 1] : numParticles;

                    for (int q = (int)gridOffsets[bin]; q < limit; q++)
                    {
                        Particle p2 = grid[q];
                        Vector2 diff = p1.Pos - p2.Pos;
                        float r2 = diff.sqrMagnitude;
                        if (r2 < kernelRadius2)
                        {
                            neighbours.Add(p2);
                        }
                    }
                }
            }


            p1.Neighbours = neighbours;
        });
    }



    /* Tính mật độ nhẵn */
    void calculateDensities()
    {
        particles.ForEach((p1) =>
        {
            float rhoSum = 0;
            p1.Neighbours.ForEach((p2) =>
            {
                rhoSum += poly6(p1.NewPos, p2.NewPos);
            });
            p1.Density = rhoSum;
            //console.log(rhoSum);
        });
    }


    /* Tính lambda*/
    void calculateLambda()
    {
        particles.ForEach((p1) =>
        {
            float constraint = p1.Density / restDensity - 1;

            float gradientSum = 0;
            Vector2 gradientKI = new Vector2(0, 0);

            /* Sum up gradient norms for the denominator */
            p1.Neighbours.ForEach((p2) =>
            {
                Vector2 gradient = spiky(p1.NewPos, p2.NewPos);
                Vector2.Scale(gradient, new Vector2(1 / restDensity, 1 / restDensity));

                gradientSum += gradient.magnitude * gradient.magnitude;

                gradientKI = gradientKI + gradient;
            });

            // console.log(constraint);

            gradientSum += gradientKI.magnitude * gradientKI.magnitude;
            p1.Lambda = -constraint / (gradientSum + epsilon);
        });
    }

    /* Tính toán khoảng cách cần thiết để di chuyển hạt */
    void calculateDeltaP()
    {
        particles.ForEach((p1) =>
        {
            Vector2 lambdaSum = new Vector2(0, 0);
            p1.Neighbours.ForEach((p2) =>
            {
                Vector2 gradient = spiky(p1.NewPos, p2.NewPos);
                lambdaSum = lambdaSum + gradient * (p1.Lambda + p2.Lambda)
            });
            p1.DeltaP = Vector2.Scale(lambdaSum, new Vector2(1 / restDensity, 1 / restDensity));
            p1.NewPos = p1.NewPos + p1.DeltaP;
        });
    }

    /* Updates the particle's position for rendering and uses verlet integration to update the velocity */
    void updatePosition()
    {
        particles.ForEach((p1) =>
        {
            p1.Vel = p1.NewPos + p1.Pos * -1;
            p1.Vel = p1.Vel * 1 / timestep;
            p1.Pos = p1.NewPos;
        });
    }

    /* Xử lý va chạm tường đơn giản */
    void constrainParticles()
    {
        particles.ForEach((p1) =>
        {
            //console.log(p1.pos);
            if (p1.NewPos[0] > 1)
            {
                p1.NewPos[0] = 1;
            }
            if (p1.NewPos[0] < 0)
            {
                p1.NewPos[0] = 0.00f;
            }
            if (p1.NewPos[1] < 0)
            {
                p1.NewPos[1] = 0.00f;
            }
            if (p1.NewPos[1] > 1)
            {
                p1.NewPos[1] = 1;
            }
        });
    }

    
    void simulate()
    {
        predictPositions();
        createSpatialHashingGrid();
        updateNeighbours();
        for (int i = 0; i < solverIterations; i++)
        {
            calculateDensities();
            calculateLambda();
            calculateDeltaP();
            constrainParticles();
        }
        updatePosition();
        iteration++;
    }





}