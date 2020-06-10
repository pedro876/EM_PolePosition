using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//La función de esta clase es establecer distintos checkpoints a lo largo del circuito y cerciorarnos de que al llegar a la meta el jugador ha pasado por todos
//ellos, evitando que se cuente como una vuelta pasar por la meta varias veces sin haber realizado correctamente el circuito.

public class CircuitProgress 
{
    #region checkpointClass
    //Esta clase ha sido creada para tener un objeto Checkpoint que tuviese como parámetros si ya ha sido visitado y la posición en la que se encuentra respecto
    //al circuito. En el circuito se podrán encontrar n checkpoints que se pueden establecer en la variable _numCheckpoints.
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
    //Este es el constructor de la clase en el que se crea una lista de tantos checkpoints como deseamos que tenga el circuito y se inicializan, tanto su
    //posición en el circuito como que estén todos visitados en una primera instancia
    public CircuitProgress()
    {
        numCheckpoints = _numCheckpoints;
        //currentCheckpoint = numCheckpoints - 2; //penúltimo
        checkpoints = new List<Checkpoint>(numCheckpoints);

        for (int i=0; i < numCheckpoints; i++)
        {
            checkpoints.Add(new Checkpoint());
            checkpoints[i].visited = true;
            checkpoints[i].progress = (1.0f / numCheckpoints) * (i);
        }
    }

    #region progressFuncs
    //Reseteamos la variable de visitado de todos los checkpoints a false, a esta función se llama una vez se ha completado la vuelta, por lo que el primer 
    //checkpoint, que es la meta, se pone a true y el currentCheckpoint se actualiza a 0 para saber que es el último por el que se ha pasado
    public void Reset()
    {
        foreach (Checkpoint cp in checkpoints)
            cp.visited = false;
        checkpoints[0].visited = true;
        this.currentCheckpoint = 0;
    }

    //Existen dos posibilidades: que el ultimo checkpoint del circuito haya sido visitado, en cuyo caso, si ademas se encuentra en una posicion menor a la del 
    //primer checkpoint, se llama a Reset y se realiza lo que se ha explicado anteriormente, además devuelve true para que se aumente una vuelta del jugador.
    //En cualquier otro caso, es decir, siempre que el checkpoint por el que ha pasado el jugador no sea la meta, se comprueba si la posicion del jugador en
    //cuanto al circuito es mayor que la del siguiente checkpoint y ademas es menor que la del dos checkpoints más allá,por lo que se encontraría en ese
    //intervalo. En ese caso se actualiza el valor de visitado del siguiente checkpoint
    //a true y se aumenta el currentCheckpoint
    public bool UpdateProgress(float pctCircuit)
    {
        if(checkpoints[numCheckpoints-1].visited)
        {
            if (pctCircuit < checkpoints[1].progress)
            {
                Reset();
                return true; 
            }
            return false;
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
