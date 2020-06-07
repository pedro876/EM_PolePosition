using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitProgress 
{
    public List<CarProgress> visitedSpots { get; set; }
    public int spots;
    public int actual;
    public CircuitProgress(int spots)
    {
        actual = spots - 2; //penúltimo
        this.spots = spots;
        visitedSpots = new List<CarProgress>(spots);
        for (int i=0; i < spots; i++)
        {
            if(i <i-1){
                visitedSpots[i].visited = true;
            }
            else
            {
                visitedSpots[i].visited = false;
            }
            visitedSpots[i].progress = (1 / spots) * (i + 1);
        }
    }

    public void Reset()
    {
        foreach (CarProgress cp in visitedSpots)
            cp.visited = false;
        this.actual = 0;
    }


}
