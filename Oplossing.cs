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
        private static readonly Random rndIndex = new Random();

        public Oplossing(List<Stop> orderList, int[,,] afstandenMatrix)
        {
            stops = new List<Stop>();
            ignore = new List<Stop>();
            tijd = 0;


            // !! update for better start oplossing !!

            // create stops objects of every order and put them in stops(list)
            for (int i = 0; i < orderList.Count; i++) {
                stops.Add(orderList[i]);
            }

            // calculate total time and add prev/references to stops in stops(list)
            stops[0].next = stops[1];
            for (int i = 1; i < (stops.Count - 1); i++) {
                stops[i].prev = stops[i - 1];
                stops[i].next = stops[i + 1];
                tijd += afstandenMatrix[stops[i - 1].matrixId, stops[i].matrixId, 1];

            }
            stops[stops.Count].prev = stops[stops.Count - 1];
            tijd += afstandenMatrix[stops[stops.Count - 1].matrixId, stops[stops.Count].matrixId, 1];

        }

        public int? pickRandomStop()
        {
            int? stopsIndex;
            if (!stops.Any()) stopsIndex = null;
            else stopsIndex = rndIndex.Next(stops.Count);

            return stopsIndex;
        }


        public int? pickRandomIgnoredStop()
        {
            int? ignoreIndex;
            if (!ignore.Any()) ignoreIndex = null;
            else ignoreIndex = rndIndex.Next(ignore.Count);

            return ignoreIndex;
        }

        public void RemoveStop(Stop stop, int index)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            int lastIndex = stops.Count - 1;
            (stops[lastIndex], stops[index]) = (stops[index], stops[lastIndex]);
            ignore.Add(stops[lastIndex]);
            stops.RemoveAt(lastIndex);

            // add null handling --!!
            (stop.prev.next, stop.next.prev) = (stop.next.prev, stop.prev.next);

        }

    }
}
