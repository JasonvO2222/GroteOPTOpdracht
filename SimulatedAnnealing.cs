using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class SimulatedAnnealing
    {
        private double T; //chance variable
        private readonly int[,,] afstandenMatrix;
        private readonly List<CollectionStop> orderList;
        private Oplossing oplossing;
        private static readonly Random rndDouble = new Random();

        public SimulatedAnnealing(int[,,] matrix, List<CollectionStop> list, float penalty)
        {
            afstandenMatrix = matrix;
            orderList = list;
            T = 1;
            Console.WriteLine($"Score voor startoplossing: {(penalty) / 60}");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            oplossing = new Oplossing(orderList, afstandenMatrix, penalty);
            timer.Stop();

            Console.WriteLine($"Duration: {timer.Elapsed}");
            Console.WriteLine($"Score na startoplossing: {(oplossing.penalty + oplossing.tijd) / 60}");
            oplossing.OutputSolution();
            


        }


    //    private bool ConsiderRemove(List<CollectionStop> stopsAuto)
    //    {
    //        // pick random stop to remove from stops(list)
    //        int? i = oplossing.pickRandomStop(stopsAuto);
    //        if (!i.HasValue) { return false; } // cancel if stops(list) was empty
    //        int index = (int)i;
    //        Stop stop = stopsAuto[index];

    //        // add null protection for when stop.prev/next is null --!!
    //        int currentMId = stop.matrixId;
    //        int prevMId = stop.prev.matrixId;
    //        int nextMId = stop.next.matrixId;

    //        // calculate difference in duration
    //        int currentDuration = afstandenMatrix[prevMId, currentMId, 1] + afstandenMatrix[currentMId, nextMId, 1];
    //        int durationAfterChange = afstandenMatrix[prevMId, nextMId, 1];
    //        int timeDiff = durationAfterChange - currentDuration;

    //        if (timeDiff <= 0) // if the change is an improvement follow through
    //        {
    //            oplossing.RemoveStop(stopsAuto, stop, index);
    //            return true;
    //        }
    //        else if (RollChance(timeDiff)) // if the chance roll returns true, follow through
    //        {
    //            oplossing.RemoveStop(stopsAuto, stop, index);
    //            return true;
    //        }

    //        return false; // else don't follow through

    //    }

    //    private bool RollChance(int diff)
    //    {
    //        double result = Math.Exp(diff / T);
    //        return rndDouble.NextDouble() < result;
    //    }


    }
}
