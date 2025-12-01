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
        private static int[,,] afstandenMatrix;
        private static List<Order> orderList;
        private Oplossing oplossing;

        public SimulatedAnnealing(int[,,] matrix, List<Order> list)
        {
            afstandenMatrix = matrix;
            orderList = list;
            T = 1;
            oplossing = new Oplossing(orderList, afstandenMatrix);
        }


        private void considerRemove(Stop stop, int index)
        {
            // add null protection for when stop.prev/next is null --!!
            int currentMId = stop.order.matrixId;
            int prevMId = stop.prev.order.matrixId;
            int nextMId = stop.next.order.matrixId;

            // calculate difference in duration
            int currentDuration = afstandenMatrix[prevMId, currentMId, 1] + afstandenMatrix[currentMId, nextMId, 1];
            int durationAfterChange = afstandenMatrix[prevMId, nextMId, 1];
            int timeDiff = durationAfterChange - currentDuration;

            if (timeDiff >= 0)
            {
                oplossing.RemoveStop(stop, index);
            }


        }

    }
}
