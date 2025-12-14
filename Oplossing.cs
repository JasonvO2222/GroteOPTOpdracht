using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Oplossing
    {
        public List<CollectionStop> stops;  //stops we visit
        public List<CollectionStop> ignore; //stops we dont visit
        public double tijd; 
        public double penalty;
        private static readonly Random rnd = new Random();
        public int ofloadingTime = 1800; //time it takes to ofload
        public int cargoSpace = 100000; //liters of space (before compression) a truck can fit
        public int maxDayTime = 43200; //max minutes in a day
        public DayStop leftMostDayStop; //track the startnode in the linkedList

        public Oplossing(List<CollectionStop> orderList, int[,,] afstandenMatrix, float penalty)
        {

            //initially ignore all stops
            ignore = new List<CollectionStop>(orderList);
            //then shuffle list
            int n = ignore.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                (ignore[k], ignore[n]) = (ignore[n], ignore[k]);
            }

            // setup other vars
            stops = new List<CollectionStop>();
            this.penalty = penalty;
            tijd = 0;


            // Create day divider nodes and connect them (each DayStop is a node which divides the linkedlist into days twice for both trucks)
            DayStop[] dayStops = new DayStop[11];
            String[] days = new String[11] { "start", "monday", "tuesday", "wednesday", "thursday", "friday", "monday", "tuesday", "wednesday", "thursday", "friday" };
            for (int i = 0; i < 11; i ++)
            {
                DayStop dStop = new DayStop(days[i], 0);
                dayStops[i] = dStop;
            }
            leftMostDayStop = dayStops[0];
            for (int i = 0; i < dayStops.Length; i++) // link all of the dayStop nodes
            {
                
                if (i == 0) { dayStops[i].next = dayStops[i + 1]; }

                else if (i == dayStops.Length - 1) { dayStops[i].prev = dayStops[i - 1]; }

                else
                {
                    dayStops[i].next = dayStops[i + 1];
                    dayStops[i].prev = dayStops[i - 1];
                }
            }


            // fill each day to the max with stops as a starting solution
            bool ignoreEmpty = false;
            foreach (DayStop dStop in dayStops)
            {
                if (dStop.day == "start") continue; 
                bool maxTimeReached = false;
                if (ignoreEmpty) { continue; }

                while (!maxTimeReached && !ignoreEmpty)
                {
                    bool maxLoadReached = false;

                    // first insert an ofload stop where the truck stops to empty its cargo
                    OfloadStop oStop = new OfloadStop(0);
                    if (dStop.dayTime + ofloadingTime > maxDayTime) { maxTimeReached = true; continue; }
                    dStop.dayTime += ofloadingTime;
                    tijd += ofloadingTime;

                    if (dStop.prev is DayStop) // in case insert ofload between dStop1 dStop2
                    {
                        oStop.prev = dStop.prev; // ofload.prev = dStop1
                        dStop.prev.next = oStop; // dStop1.next = ofload
                        dStop.prev = oStop;      // dStop2.prev = ofload
                        oStop.next = dStop;      // ofload.next = dStop2
                    }
                    else if (dStop.prev is OfloadStop) //in case insert ofload between prevOStop dStop
                    {
                        OfloadStop prevOStop = (OfloadStop)dStop.prev;
                        prevOStop.next = oStop;             // prevOStop.next = ofload
                        prevOStop.nextOfloadStop = oStop;   // prevOStop.nextOfloadStop
                        oStop.prev = prevOStop;             // ofload.prev = prevOStop
                        oStop.next = dStop;                 // ofload.next = dStop
                        dStop.prev = oStop;                 // dStop.prev = ofload
                    }

                    while (!maxTimeReached && !maxLoadReached && !ignoreEmpty) //now fill with collection stops until cargo is full
                    {
                        if (ignore.Count == 0) { ignoreEmpty = true; continue; }
                        CollectionStop stop = ignore[0];

                        // check if it fits in the leftover cargospace before the dropoff
                        // also check if the extra time for the stop fits in the time left in the day
                        
                        int volumeCheck = oStop.volume + (stop.containerCount * stop.containerVolume);
                        if (volumeCheck > cargoSpace) { maxLoadReached = true; continue; }

                        float timeCheck = dStop.dayTime + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                                        + afstandenMatrix[oStop.prev.matrixId, stop.matrixId, 1]
                                                        - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                                        + stop.loadingTime;
                        if (timeCheck > maxDayTime) { maxTimeReached = true; continue; }

                        // if it is possible update the ofload and day stop node with the new values
                        oStop.volume = volumeCheck;
                        dStop.dayTime = timeCheck;
                        tijd = tijd + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                    + afstandenMatrix[oStop.prev.matrixId, stop.matrixId, 1]
                                    - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                    + stop.loadingTime;

                        // and reddirect the pointers to the correct nodes
                        oStop.prev.next = stop;
                        stop.prev = oStop.prev;
                        oStop.prev = stop;
                        stop.next = oStop;
                        stop.ofloadStop = oStop;
                        stop.dayStop = dStop;
                        


                        stop.included = true;
                        
                        // check penalty and remove if correct
                        if (CorrectPickup(stop)) penalty -= (3 * stop.loadingTime * stop.frequency);

                        //remove from ignore and add to stops
                        AddStop(0);

                    }
                }
            }


        }

        string[] allowedThreeDays = { "monday", "wednesday", "friday" };
        string[] allowedTwoDays1 = { "monday", "thursday" };
        string[] allowedTwoDays2 = { "tuesday", "friday" };
        int WeekdayIndex(string day)
        {   return day switch
            {
                "monday" => 1, "tuesday" => 2, "wednesday" => 3, "thursday" => 4, "friday" => 5
            };  }

        // this function takes a stop and checks if the order is divided over the correct days
        // to check a hypothetical when considering a swap or add the day and included vars can be overridden
        public bool CorrectPickup(CollectionStop stop, string day = null, bool included = false)
        {
            // if it is an order with a single pickup it is true unless the order isnt included in stops
            if (stop.frequency == 1 && (stop.included || included)) { return true; }
            else if (stop.frequency == 1) { return false; }


            else
            {
                if (!(stop.included || included)) return false;

                // set up a list with all the days that the order is picked up
                string[] weekdays = new string[stop.frequency];
                if (day != null) { weekdays[0] = day; }
                else weekdays[0] = stop.dayStop.day;

                for (int i = 1; i < stop.frequency; i++)
                {
                    CollectionStop s = stop.siblings[i - 1];
                    if (!s.included) return false; // if there is a order pickup that is ignored return false
                    weekdays[i] = s.dayStop.day;
                }


                var days = weekdays.OrderBy(d => WeekdayIndex(d)).ToArray();

                if (stop.frequency == 4) // if freq = 4 check if all the days are distinct days
                {
                    return days.Distinct().Count() == 4;
                }

                else if (stop.frequency == 3) // if freq = 3 check if the right days are included
                {

                    return allowedThreeDays.SequenceEqual(days);
                }

                else if (stop.frequency == 2) // if freq = 2 check if it is one of two right combinations 
                {
                    return (allowedTwoDays1.SequenceEqual(days) || allowedTwoDays2.SequenceEqual(days));
                }
            }
            return false;

        }

        public void AddStop(int index)
        {
            // switch object with last object in ignore(list) and add it to stops(list) and remove it from ignore(list)
            CollectionStop stop = ignore[index];
            stops.Add(stop);
            int c = ignore.Count - 1;
            (ignore[index], ignore[c]) = (ignore[c], ignore[index]);
            ignore.RemoveAt(c);
        }

        public void RemoveStop(int index)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            CollectionStop stop = stops[index];
            ignore.Add(stop);
            int c = stops.Count - 1;
            (stops[index], stops[c]) = (stops[c], stops[index]);
            stops.RemoveAt(c);
        }

        public void OutputSolution()
        {
            StreamWriter sW = new StreamWriter("Resultaat.txt");
            Stop s = leftMostDayStop.next; // get first node

            int counter = 1;
            int truck = 1;
            int dagId = 1;

            while (s != null) //iterate over linkedlist
            {
                string line = "";
                if (s is DayStop)
                {
                    DayStop r = (DayStop)s;
                    dagId++;
                    counter = 1;
                    if(r.day == "friday") {; truck = 2; dagId = 1; } //once the friday DayStop node has passed switch to truck 2
                }

                else if(s is CollectionStop)
                {
                    CollectionStop r = (CollectionStop)s;
                    line = $"{truck}; {dagId}; {counter}; {r.orderId}";
                    sW.WriteLine(line);
                    counter++;
                }

                else if (s is OfloadStop)
                {
                    OfloadStop r = (OfloadStop)s;
                    line = $"{truck}; {dagId}; {counter}; {0}";
                    sW.WriteLine(line);
                    counter++;
                }
                s = s.next;
                Console.WriteLine(line);
            }

            sW.Close();

        }

        // gets random index from stops
        public int? pickRandomStop()
        {
            int? index;
            if (!stops.Any()) index = null;
            else index = rnd.Next(stops.Count);

            return index;
        }

        // gets random index from ignore
        public int? pickRandomIgnoredStop()
        {
            int? index;
            if (!ignore.Any()) index = null;
            else index = rnd.Next(ignore.Count);

            return index;
        }


        // execute swap in linkedlist
        public void Swap(CollectionStop s1, CollectionStop s2)
        {
            // if you swap adjacent nodes
            if (s1.next == s2)
            {
                s1.next = s2.next;
                s2.next.prev = s1;
                s2.prev = s1.prev;
                s1.prev.next = s2;
                s2.next = s1;
                s1.prev = s2;
            }
            else if (s2.next == s1)
            {
                s2.next = s1.next;
                s1.next.prev = s2;
                s1.prev = s2.prev;
                s2.prev.next = s1;
                s1.next = s2;
                s2.prev = s1;
            }
            else // if the swap nodes are not adjacent
            {
                s1.prev.next = s2;
                s1.next.prev = s2;
                s2.prev.next = s1;
                s2.next.prev = s1;
                (s1.next, s2.next) = (s2.next, s1.next);
                (s1.prev, s2.prev) = (s2.prev, s1.prev);
            }

            // update other pointers
            (s1.dayStop, s2.dayStop) = (s2.dayStop, s1.dayStop);
            (s1.ofloadStop, s2.ofloadStop) = (s2.ofloadStop, s1.ofloadStop);

        }

        // inserts a node into the linkedlist at specific node
        public void Insert(CollectionStop insertNode, CollectionStop newStop)
        {
            newStop.next = insertNode.next;
            insertNode.next.prev = newStop;
            insertNode.next = newStop;
            newStop.prev = insertNode;
            newStop.ofloadStop = insertNode.ofloadStop;
            newStop.dayStop = insertNode.dayStop;
            newStop.included = true;
        }

        // removes a node from linkedlist
        public void Remove(CollectionStop stop)
        {
            stop.prev.next = stop.next;
            stop.next.prev = stop.prev;
            stop.next = null;
            stop.prev = null;
            stop.ofloadStop = null;
            stop.dayStop = null;
            stop.included = false;
        }

    }
}
