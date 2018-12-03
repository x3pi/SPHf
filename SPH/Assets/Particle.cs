using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle
{
    public int Id;
    public Vector2 Pos;
    public Vector2 Vel;
    public Vector2 NewPos;
    public Vector2 DeltaP;
    public List<Particle> Neighbours;
    public float Lambda;

    public float Density;


    public Particle(int id, Vector2 pos, Vector2 vel, Vector2 newPos, Vector2 deltaP, List<Particle> neighbours, float lambda, float density)
    {
        Id = id;
        Pos = pos;
        Vel = vel;
        NewPos = newPos;
        DeltaP = deltaP;
        Neighbours = neighbours;
        Lambda = lambda;
        Density = density;
    }




}
