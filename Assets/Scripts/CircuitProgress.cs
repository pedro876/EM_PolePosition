using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitProgress 
{
    static int _numCheckpoints = 5;

    public class Checkpoint
    {
        public bool visited { get; set; }
        public float progress { get; set; }
    }

    private List<Checkpoint> checkpoints { get; set; }
    private int numCheckpoints;
    private int currentCheckpoint;
    
    public CircuitProgress()
    {
        numCheckpoints = _numCheckpoints;
        
        currentCheckpoint = numCheckpoints - 2; //penúltimo
        checkpoints = new List<Checkpoint>(numCheckpoints);

        for (int i=0; i < numCheckpoints; i++)
        {
            checkpoints.Add(new Checkpoint());
            checkpoints[i].visited = true;
            checkpoints[i].progress = (1.0f / numCheckpoints) * (i);
        }
    }

    public void Reset()
    {
        Debug.Log("NUEVA VUELTA");
        foreach (Checkpoint cp in checkpoints)
            cp.visited = false;
        checkpoints[0].visited = true;
        this.currentCheckpoint = 0;
    }

    public bool UpdateProgress(float pctCircuit)
    {
        if(checkpoints[numCheckpoints-1].visited)
        {
            //Debug.Log(pctCircuit + " " + checkpoints[1].progress);
            if (pctCircuit < checkpoints[1].progress)
            {
                Reset();
                return true; //se debe aumentar la vuelta
            }
            return false;
        } else
        {
            if (pctCircuit > checkpoints[currentCheckpoint + 1].progress)
            {
                checkpoints[currentCheckpoint + 1].visited = true;
                currentCheckpoint++;
            }
            return false;
        }
        return false;
    }
}
