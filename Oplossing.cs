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
        public float tijd;
        public float penalty;
        private static readonly Random rnd = new Random();
        public int ofloadingTime = 1800; //time it takes to ofload
        public int cargoSpace = 100000; //liters of space (before compression) a truck can fit
        public int maxDayTime = 43200;
        public DayStop leftMostDayStop;

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
            DayStop[] dayStops = new DayStop[10];
            String[] days = new String[10] { "monday", "tuesday", "wednesday", "thursday", "friday", "monday", "tuesday", "wednesday", "thursday", "friday" };
            for (int i = 0; i < 10; i ++)
            {
                DayStop dStop = new DayStop(days[i], 0);
                dayStops[i] = dStop;
            }
            leftMostDayStop = dayStops[0];
            for (int i = 0; i < dayStops.Length; i++)
            {
                
                if (i == 0) { dayStops[i].next = dayStops[i + 1]; }

                else if (i == dayStops.Length - 1) { dayStops[i].prev = dayStops[i - 1]; }

                else
                {
                    dayStops[i].next = dayStops[i + 1];
                    dayStops[i].prev = dayStops[i - 1];
                }
            }


            // fill each day
            bool ignoreEmpty = false;
            foreach (DayStop dStop in dayStops)
            {
                bool maxTimeReached = false;

                if (ignoreEmpty) { continue; }

                while (!maxTimeReached && !ignoreEmpty)
                {
                    bool maxLoadReached = false;

                    // first insert an ofload stop
                    OfloadStop oStop = new OfloadStop(0);
                    if (dStop.dayTime + ofloadingTime > maxDayTime) { maxLoadReached = true; continue; }
                    dStop.dayTime += ofloadingTime;
                    tijd += ofloadingTime;

                    if (dStop.prev == null) //in the case insert ofload before dStop
                    {
                        dStop.prev = oStop;     // dStop.prev = ofload
                        oStop.next = dStop;     // ofload.next = dStop
                    }
                    else if (dStop.prev is DayStop) // in case insert ofload between dStop1 dStop2
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

                    while (!maxTimeReached && !maxLoadReached && !ignoreEmpty)
                    {
                        if (ignore.Count == 0) { ignoreEmpty = true; continue; }
                        CollectionStop stop = ignore[0];

                        if(oStop.prev == null)
                        {
                            oStop.prev = stop;
                            stop.next = oStop;
                            stop.ofloadStop = oStop;
                            stop.dayStop = dStop;
                            oStop.volume += (stop.containerCount * stop.containerVolume);
                            dStop.dayTime = dStop.dayTime + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                                          + stop.loadingTime;
                            tijd = tijd + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                        + stop.loadingTime;
                        }
                        else if (oStop.prev is DayStop)
                        {
                            int volumeCheck = oStop.volume + (stop.containerCount * stop.containerVolume);
                            if (volumeCheck > cargoSpace) { maxLoadReached = true; continue; }

                            float timeCheck = dStop.dayTime + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                                          + stop.loadingTime;
                            if (timeCheck > maxDayTime) { maxTimeReached = true; continue; }

                            oStop.volume = volumeCheck;
                            dStop.dayTime = timeCheck;
                            tijd = tijd + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                        + stop.loadingTime;
                            oStop.prev.next = stop;
                            stop.prev = oStop.prev;
                            oStop.prev = stop;
                            stop.next = oStop;
                            stop.ofloadStop = oStop;
                            stop.dayStop = dStop;
                        }
                        else
                        {
                            int volumeCheck = oStop.volume + (stop.containerCount * stop.containerVolume);
                            if (volumeCheck > cargoSpace) { maxLoadReached = true; continue; }

                            float timeCheck = dStop.dayTime + afstandenMatrix[oStop.prev.matrixId, stop.matrixId, 1]
                                                          + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                                          - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                                          + stop.loadingTime;

                            if (timeCheck > maxDayTime) { maxTimeReached = true; continue; }

                            oStop.volume = volumeCheck;
                            dStop.dayTime = timeCheck;
                            tijd = tijd +afstandenMatrix[oStop.prev.matrixId, stop.matrixId, 1]
                                        + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                        - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                        + stop.loadingTime;
                            oStop.prev.next = stop;
                            stop.prev = oStop.prev;
                            oStop.prev = stop;
                            stop.next = oStop;
                            stop.ofloadStop = oStop;
                            stop.dayStop = dStop;
                        }

                        stop.included = true;
                        
                        // check penalty and remove if correct
                        if (CorrectPickup(stop)) penalty -= (3 * stop.loadingTime);

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

        public bool CorrectPickup(CollectionStop stop)
        {
            if (stop.frequency == 1 && stop.included) { return true; }
            else if (stop.frequency == 1) { return false; }


            else
            {
                if (!stop.included) return false;

                string[] weekdays = new string[stop.frequency];
                weekdays[0] = stop.dayStop.day;

                for (int i = 1; i < stop.frequency; i++)
                {
                    CollectionStop s = stop.siblings[i - 1];
                    if (!s.included) return false;
                    weekdays[i] = s.dayStop.day;
                }


                var days = weekdays.OrderBy(d => WeekdayIndex(d)).ToArray();

                if (stop.frequency == 4)
                {
                    return days.Distinct().Count() == 4;
                }

                else if (stop.frequency == 3)
                {

                    return allowedThreeDays.SequenceEqual(days);
                }

                else if (stop.frequency == 2)
                {
                    return (allowedTwoDays1.SequenceEqual(days) || allowedTwoDays2.SequenceEqual(days));
                }
            }
            return false;

        }

        public void AddStop(int index)
        {
            CollectionStop stop = ignore[index];
            stops.Add(stop);
            int c = ignore.Count - 1;
            (ignore[index], ignore[c]) = (ignore[c], ignore[index]);
            ignore.RemoveAt(c);
        }

        public void OutputSolution()
        {
            StreamWriter sW = new StreamWriter("Resultaat.txt");
            Stop s = leftMostDayStop;
            while(true)
            {
                if (s.prev != null)
                {
                    s = s.prev;
                }
                else break;
            }
            int counter = 1;
            int truck = 1;
            int dagId = 1;

            while (s != null)
            {
                string line = "";
                if (s is DayStop)
                {
                    DayStop r = (DayStop)s;
                    dagId++;
                    counter = 1;
                    if(r.day == "friday") {; truck = 2; dagId = 1; }
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

        //public int? pickRandomStop(List<Stop> stopsAuto)
        //{
        //    int? stopsIndex;
        //    if (!stopsAuto.Any()) stopsIndex = null;
        //    else stopsIndex = rndIndex.Next(stopsAuto.Count);

        //    return stopsIndex;
        //}


        //public int? pickRandomIgnoredStop()
        //{
        //    int? ignoreIndex;
        //    if (!ignore.Any()) ignoreIndex = null;
        //    else ignoreIndex = rndIndex.Next(ignore.Count);

        //    return ignoreIndex;
        //}

        //public void RemoveStop(List<Stop> stopsAuto, Stop stop, int index)
        //{
        //    // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
        //    int lastIndex = stopsAuto.Count - 1;
        //    (stopsAuto[lastIndex], stopsAuto[index]) = (stopsAuto[index], stopsAuto[lastIndex]);
        //    ignore.Add(stopsAuto[lastIndex]);
        //    stopsAuto.RemoveAt(lastIndex);

        //    // add null handling --!!
        //    (stop.prev.next, stop.next.prev) = (stop.next.prev, stop.prev.next);
        //}

    }
}
