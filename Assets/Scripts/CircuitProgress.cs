using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitProgress 
{
    #region checkpointClass
    public class Checkpoint
    {
        public bool visited { get; set; }
        public float progress { get; set; }
    }
    #endregion checkpointClass

    #region variables

    static int _numCheckpoints = 5;

    private List<Checkpoint> checkpoints { get; set; }
    private int numCheckpoints;
    private int currentCheckpoint;

    #endregion variables

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

    #region progressFuncs

    public void Reset()
    {
        //Debug.Log("NUEVA VUELTA");
        foreach (Checkpoint cp in checkpoints)
            cp.visited = false;
        checkpoints[0].visited = true;
        this.currentCheckpoint = 0;
    }

    public bool UpdateProgress(float pctCircuit)
    {
        if(checkpoints[numCheckpoints-1].visited)
        {
            if (pctCircuit < checkpoints[1].progress)
            {
                Reset();
                return true; //se debe aumentar la vuelta
            }
            return false;
        //si estoy entre dos checkpoint y no es el último
        } else if (pctCircuit > checkpoints[currentCheckpoint + 1].progress
            && (currentCheckpoint + 2 == numCheckpoints || pctCircuit < checkpoints[currentCheckpoint + 2].progress))
        {
            checkpoints[currentCheckpoint + 1].visited = true;
            currentCheckpoint++;
            return false;
        }
        return false;
    }

    #endregion progressFuncs
}
