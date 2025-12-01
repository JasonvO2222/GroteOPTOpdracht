using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Oplossing
    {
        public List<Stop> stops;
        public List<Stop> ignore;
        public int tijd;

        public Oplossing(List<Order> orderList, int[,,] afstandenMatrix)
        {
            stops = new List<Stop>();
            ignore = new List<Stop>();
            tijd = 0;


            // !! update for better start oplossing !!

            // create stops objects of every order and put them in stops
            for (int i = 0; i < orderList.Count; i++) {
                stops.Add(new Stop(orderList[i]));
            }

            // calculate total time and add prev/references to stops in stops
            stops[0].next = stops[1];
            for (int i = 1; i < (stops.Count - 1); i++) {
                stops[i].prev = stops[i - 1];
                stops[i].next = stops[i + 1];
                tijd += afstandenMatrix[stops[i - 1].order.matrixId, stops[i].order.matrixId, 1];

            }
            stops[stops.Count].prev = stops[stops.Count - 1];
            tijd += afstandenMatrix[stops[stops.Count - 1].order.matrixId, stops[stops.Count].order.matrixId, 1];

        }

        public void RemoveStop(Stop stop, int index)
        {
            int lastIndex = stops.Count - 1;
            (stops[lastIndex], stops[index]) = (stops[index], stops[lastIndex]);
            ignore.Add(stops[lastIndex]);
            stops.RemoveAt(lastIndex);

            // add null handling --!!
            (stop.prev.next, stop.next.prev) = (stop.next.prev, stop.prev.next);

        }

    }

    public class Stop
    {
        public Stop? next;
        public Stop? prev;
        public Order order;

        public Stop(Order o)
        {
            order = o;
        }
    }
}
