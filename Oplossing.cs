using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Oplossing
    {
        public List<CollectionStop> monday0;  //stops we visit on monday with truck 0
        public List<CollectionStop> monday1;  //stops we visit on monday with truck 1
        public List<CollectionStop> tuesday0;  //stops we visit on tuesday with truck 0
        public List<CollectionStop> tuesday1;  //stops we visit on tuesday with truck 1
        public List<CollectionStop> wednesday0;  //stops we visit on wednesday with truck 0
        public List<CollectionStop> wednesday1;  //stops we visit on wednesday with truck 1
        public List<CollectionStop> thursday0;  //stops we visit on thursday with truck 0
        public List<CollectionStop> thursday1;  //stops we visit on thursday with truck 1
        public List<CollectionStop> friday0;  //stops we visit on friday with truck 0
        public List<CollectionStop> friday1;  //stops we visit on friday with truck 1
        public List<CollectionStop> ignore; //stops we dont visit
        public double tijd; 
        public double penalty;
        private static readonly Random rnd = new Random();
        public int ofloadingTime = 1800; //time it takes to ofload
        public int cargoSpace = 100000; //liters of space (before compression) a truck can fit
        public int maxDayTime = 43200; //max minutes in a day
        public DayStop leftMostDayStop; //track the startnode in the linkedList
        public DayStop[] dayStops; // Contains all the daystops
        public int[,,] afstandenMatrix;

        public Oplossing(List<CollectionStop> orderList, int[,,] aMatrix, float pen)
        {
            this.afstandenMatrix = aMatrix;
            //initially ignore all stops
            ignore = new List<CollectionStop>();
            //then shuffle list
            int n = ignore.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                (ignore[k], ignore[n]) = (ignore[n], ignore[k]);
            }

            // setup other vars
            monday0 = new List<CollectionStop>();
            monday1 = new List<CollectionStop>();
            tuesday0 = new List<CollectionStop>();
            tuesday1 = new List<CollectionStop>();
            wednesday0 = new List<CollectionStop>();
            wednesday1 = new List<CollectionStop>();
            thursday0 = new List<CollectionStop>();
            thursday1 = new List<CollectionStop>();
            friday0 = new List<CollectionStop>();
            friday1 = new List<CollectionStop>();
            penalty = pen;
            tijd = 0;


            // Create day divider nodes and connect them (each DayStop is a node which divides the linkedlist into days twice for both trucks)
            dayStops = new DayStop[11];
            int[] days = new int[10] {0, 1, 2, 3, 4, 0, 1, 2, 3, 4 };
            dayStops[0] = new DayStop(-1, 0, -1);
            leftMostDayStop = dayStops[0];
            for (int j = 0; j < 2; j++)
            {
                int x;
                for (int i = 0; i < 5; i++)
                {
                    x = j * 5 + i;
                    DayStop d = new DayStop(days[x], 0, j);
                    dayStops[x + 1] = d;
                }
            }

            // link all of the dayStop nodes and add OfloadStops in between
            for (int i = 1; i < dayStops.Length; i++)
            {
                int l = i - 1; //last daystop index

                OfloadStop o = new OfloadStop(0);

                dayStops[l].next = o;
                o.prev = dayStops[l];
                o.next = dayStops[i];
                dayStops[i].prev = o;

                dayStops[l].dayStop = dayStops[i];
                dayStops[l].ofloadStop = o;

                dayStops[i].dayTime += 1800;
                tijd += 1800;
            }


            // prepare some variables for making starting solution
            (CollectionStop, int, int)[] toCheck = new (CollectionStop, int, int)[4];
            (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)[] checkedStops = new (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)[4];
            int day;
            bool fits;
            List<CollectionStop> destination;
            CollectionStop s;

            // variables for casting into from the checkedStops list
            CollectionStop stop;
            DayStop dStop;
            OfloadStop oStop;
            int volumeCheck;
            float timeCheck;

            // make starting solution by iterating over stops and handling it according to frequency
            while (orderList.Count > 0)
            {
                s = orderList[0];
                int freq = s.frequency;
                if (freq == 1)
                {
                    (fits, checkedStops[0]) = CheckSingle(s, rnd.Next(5), rnd.Next(2));
                }
                else if (freq == 2)
                {
                    day = rnd.Next(2);
                    toCheck[0] = (s, day, rnd.Next(2));
                    day += 3;
                    toCheck[1] = (s.siblings[0], day, rnd.Next(2));

                    (fits, checkedStops) = CheckAll(toCheck, freq);
                }
                else if (freq == 3)
                {
                    toCheck[0] = (s, 0, rnd.Next(2));
                    toCheck[1] = (s.siblings[0], 2, rnd.Next(2));
                    toCheck[2] = (s.siblings[1], 4, rnd.Next(2));

                    (fits, checkedStops) = CheckAll(toCheck, freq);
                }
                else
                {
                    int r = rnd.Next(5);
                    int c = 0;
                    day = 0;
                    foreach (CollectionStop cStop in s.siblings.Append(s))
                    {
                        if (day == r) day++;
                        toCheck[c] = (cStop, day, rnd.Next(2));
                        day++;
                        c++;
                    }

                    (fits, checkedStops) = CheckAll(toCheck, freq);
                }



                if (fits) // true if all stops in order can be added
                {
                    for (int i = 0; i < freq; i ++)
                    {
                        (stop, dStop, oStop, destination, volumeCheck, timeCheck) = checkedStops[i];
                        

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

                        //remove from orderlist and add to right stoplist
                        int lastIndex = orderList.Count - 1;
                        int id = orderList.IndexOf(stop); //sadly O(n) but cant be helped
                        (orderList[id], orderList[lastIndex]) = (orderList[lastIndex], orderList[id]);
                        stop.index = destination.Count;
                        destination.Add(orderList[lastIndex]);
                        orderList.RemoveAt(lastIndex);

                    }

                    penalty -= (3 * s.loadingTime * s.frequency);
                }
                else //else remove and put in ignore
                {
                    for (int i = 0; i < freq; i++)
                    {
                        (stop, dStop, oStop, destination, volumeCheck, timeCheck) = checkedStops[i];
                        //remove from orderlist and add to ignore
                        int lastIndex = orderList.Count - 1;
                        int id = orderList.IndexOf(stop); //sadly O(n) but cant be helped
                        (orderList[id], orderList[lastIndex]) = (orderList[lastIndex], orderList[id]);
                        stop.index = ignore.Count;
                        ignore.Add(orderList[lastIndex]);
                        orderList.RemoveAt(lastIndex);
                    }

                }

            }

        }

        // Checks if all stops in the input (from one order) will fit in the randomly chosen days/trucks
        private (bool, (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)[]) CheckAll((CollectionStop, int, int)[] ls, int freq )
        {
            (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)[] res = new (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)[4];
            int count = 0;
            bool bAccumulator = true;

            bool b;
            CollectionStop stop;
            DayStop dStop;
            OfloadStop oStop;
            int volumeCheck;
            float timeCheck;

            for (int i = 0; i < freq; i++)
            {
                (CollectionStop s, int day, int truck) = ls[i];
                (b, res[count]) = CheckSingle(s, day, truck);
                bAccumulator &= b;
                count++;
            }

            return (bAccumulator, res);
        }

        // Checks if a stop will fit in the randomly chosen day/truck
        private (bool, (CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float)) CheckSingle(CollectionStop s, int day, int truck)
        {
            List<CollectionStop> sList = MappingToList(day, truck);
            DayStop dStop = MappingToDayStop(day, truck);
            OfloadStop oStop = (OfloadStop)dStop.prev;
            bool b = true;

            int volumeCheck = oStop.volume + (s.containerCount * s.containerVolume);
            if (volumeCheck > cargoSpace) { b = false; }

            float timeCheck = dStop.dayTime + afstandenMatrix[s.matrixId, oStop.matrixId, 1]
                                            + afstandenMatrix[oStop.prev.matrixId, s.matrixId, 1]
                                            - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                            + s.loadingTime;
            if (timeCheck > maxDayTime) { b = false; }

            return (b, (s, dStop, oStop, sList, volumeCheck,  timeCheck));

        }

        public void AddStop(int id, List<CollectionStop> dayTruckList)
        {
            // switch object with last object in ignore(list) and add it to stops(list) and remove it from ignore(list)
            CollectionStop stop = ignore[id];
            int c = ignore.Count - 1;
            CollectionStop lastStop = ignore[c];
            lastStop.index = id;
            (ignore[id], ignore[c]) = (ignore[c], ignore[id]);

            ignore.RemoveAt(c);
            dayTruckList.Add(stop);
            stop.index = dayTruckList.Count - 1;
        }

        public void AddStop(CollectionStop stop, List<CollectionStop> dayTruckList)
        {
            int id = stop.index;
            int lastIndex = ignore.Count - 1;

            if (id != lastIndex)  // Only swap if not already last
            {
                CollectionStop lastStop = ignore[lastIndex];
                lastStop.index = id;
                (ignore[id], ignore[lastIndex]) = (ignore[lastIndex], ignore[id]);
            }

            ignore.RemoveAt(lastIndex);
            stop.index = dayTruckList.Count;
            dayTruckList.Add(stop);
        }

        public void RemoveStop(int index, List<CollectionStop> dayTruckList)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            CollectionStop stop = dayTruckList[index];
            stop.index = ignore.Count;
            ignore.Add(stop);
            int c = dayTruckList.Count - 1;
            CollectionStop lastStop = dayTruckList[c];
            lastStop.index = index;
            (dayTruckList[index], dayTruckList[c]) = (dayTruckList[c], dayTruckList[index]);
            dayTruckList.RemoveAt(c);
        }

        public void RemoveStop(CollectionStop stop, List<CollectionStop> dayTruckList)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            int index = stop.index;
            stop.index = ignore.Count;
            ignore.Add(stop);
            int c = dayTruckList.Count - 1;

            if (index != c)
            {
                CollectionStop lastStop = dayTruckList[c];
                lastStop.index = index;
                (dayTruckList[index], dayTruckList[c]) = (dayTruckList[c], dayTruckList[index]);
            }

            dayTruckList.RemoveAt(c);
        }

        public void SwapStop(CollectionStop stop1, List<CollectionStop> l1, CollectionStop stop2, List<CollectionStop> l2)
        {
            if (l1 == l2)
            {
                return;
            }

            int index1 = stop1.index;
            int index2 = stop2.index;

            int l1index = (l1.Count - 1);
            int l2index = (l2.Count - 1);


            if (l1index < 0) l1index = 0;
            if (l2index < 0) l2index = 0;

            l1[l1index].index = index1;
            l2[l2index].index = index2;

            (l1[index1], l1[l1index]) = (l1[l1index], l1[index1]);
            (l2[index2], l2[l2index]) = (l2[l2index], l2[index2]);

            stop1.index = l2index;
            stop2.index = l1index;

            l1.RemoveAt(l1index);
            l2.RemoveAt(l2index);

            l1.Add(stop2);
            l2.Add(stop1);

        }


        public void ShiftStop(CollectionStop source, List<CollectionStop> sourceList, List<CollectionStop> targetList)
        {
            if (sourceList == targetList)
            {
                return;
            }

            int sourceIndex = source.index;
            int lastIndex = sourceList.Count - 1;
            int newIndex = targetList.Count;
            source.index = newIndex;

            if (sourceIndex != lastIndex)
            {
                CollectionStop lastStop = sourceList[lastIndex];
                lastStop.index = sourceIndex;
                (sourceList[sourceIndex], sourceList[lastIndex]) = (sourceList[lastIndex], sourceList[sourceIndex]);
            }

            sourceList.RemoveAt(lastIndex);
            targetList.Add(source);
        }

        public void OutputSolution(string path = "Resultaat.txt")
        {
            StreamWriter sW = new StreamWriter(path);
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
                    if(r.day == 4) {; truck = 2; dagId = 1; } //once the friday DayStop node has passed switch to truck 2
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
        public int? pickRandomStop(List<CollectionStop> dayTruckList)
        {
            int? index;
            if (!dayTruckList.Any()) index = null;
            else index = rnd.Next(dayTruckList.Count);

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
        public void Insert(Stop insertNode, CollectionStop newStop)
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

        // shift two nodes in the linkedList
        public void Shift(CollectionStop source,  Stop target)
        {
            if (target.next == source)
            {
                return;
            }

            source.prev.next = source.next;
            source.next.prev = source.prev;
            source.prev = target;
            source.next = target.next;
            target.next.prev = source;
            target.next = source;

            source.dayStop = target.dayStop;
            source.ofloadStop = target.ofloadStop;
        }


        public List<CollectionStop> MappingToList(int day, int truck)
        {
            switch (day, truck)
            {
                case (0, 0):
                    return monday0;
                case (0, 1):
                    return monday1;
                case (1, 0):
                    return tuesday0;
                case (1, 1):
                    return tuesday1;
                case (2, 0):
                    return wednesday0;
                case (2, 1):
                    return wednesday1;
                case (3, 0):
                    return thursday0;
                case (3, 1):
                    return thursday1;
                case (4, 0):
                    return friday0;
                case (4, 1):
                    return friday1;
            }
            return new List<CollectionStop>(); //this shouldnt happen
        }

        public (int, int) MappingFromList(List<CollectionStop> ls )
        {
            if (ls.Equals(monday0))
            {
                return (0, 0);
            }
            if (ls.Equals(monday1))
            {
                return (0, 1);
            }
            if (ls.Equals(tuesday0))
            {
                return (1, 0);
            }
            if (ls.Equals(tuesday1))
            {
                return (1, 1);
            }
            if (ls.Equals(wednesday0))
            {
                return (2, 0);
            }
            if (ls.Equals(wednesday1))
            {
                return (2, 1);
            }
            if (ls.Equals(thursday0))
            {
                return (3, 0);
            }
            if (ls.Equals(thursday1))
            {
                return (3, 1);
            }
            if (ls.Equals(friday0))
            {
                return (4, 0);
            }
            if (ls.Equals(friday1))
            {
                return (4, 1);
            }


            return (-1, -1); //this shouldnt happen
        }

        public DayStop MappingToDayStop(int day, int truck)
        {
            return dayStops[((day + 1) + (truck * 5))];
        }

    }
}
