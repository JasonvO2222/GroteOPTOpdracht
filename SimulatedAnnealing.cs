using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class SimulatedAnnealing
    {
        private double T; //chance variable
        private readonly int[,,] afstandenMatrix;
        private readonly List<Stop> orderList;
        private Oplossing oplossing;
        private static readonly Random rndDouble = new Random();

        public SimulatedAnnealing(int[,,] matrix, List<Stop> list)
        {
            afstandenMatrix = matrix;
            orderList = list;
            T = 1;
            oplossing = new Oplossing(orderList, afstandenMatrix);

            // test
            ConsiderRemove();

            Console.WriteLine($"ignored orders: {oplossing.ignore.Count}");
            Console.WriteLine($"total time: {oplossing.tijd}");
        }


        private bool ConsiderRemove()
        {
            // pick random stop to remove from stops(list)
            int? i = oplossing.pickRandomStop();
            if (!i.HasValue) { return false; } // cancel if stops(list) was empty
            int index = (int)i;
            Stop stop = oplossing.stops[index];

            // add null protection for when stop.prev/next is null --!!
            int currentMId = stop.matrixId;
            int prevMId = stop.prev.matrixId;
            int nextMId = stop.next.matrixId;

            // calculate difference in duration
            int currentDuration = afstandenMatrix[prevMId, currentMId, 1] + afstandenMatrix[currentMId, nextMId, 1];
            int durationAfterChange = afstandenMatrix[prevMId, nextMId, 1];
            int timeDiff = durationAfterChange - currentDuration;

            if (timeDiff >= 0) // if the change is an improvement follow through
            {
                oplossing.RemoveStop(stop, index);
                return true;
            }
            else if (RollChance(timeDiff)) // if the chance roll returns true, follow through
            {
                oplossing.RemoveStop(stop, index);
                return true;
            }

            return false; // else don't follow through

        }

        private bool RollChance(int diff)
        {
            double result = Math.Exp(diff / T);
            return rndDouble.NextDouble() < result;
        }


    }
}
