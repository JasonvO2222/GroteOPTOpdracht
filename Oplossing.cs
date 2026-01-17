using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            String[] days = new String[10] {"monday", "tuesday", "wednesday", "thursday", "friday", "monday", "tuesday", "wednesday", "thursday", "friday" };
            dayStops[0] = new DayStop("start", 0, -1);
            leftMostDayStop = dayStops[0];
            for (int j = 0; j < 2; j++)
            {
                int x;
                for (int i = 0; i < 5; i++)
                {
                    x = j * 5 + i;
                    DayStop dStop = new DayStop(days[x], 0, j);
                    dayStops[x + 1] = dStop;
                }
            }

            // link all of the dayStop nodes and add OfloadStops in between
            for (int i = 1; i < dayStops.Length; i++)
            {
                int l = i - 1; //last daystop index

                OfloadStop oStop = new OfloadStop(0);

                dayStops[l].next = oStop;
                oStop.prev = dayStops[l];
                oStop.next = dayStops[i];
                dayStops[i].prev = oStop;
            }

            // fill each day to the max with stops as a starting solution
            foreach (DayStop dStop in dayStops)
            {
                if (dStop.day == "start") continue;

                bool maxTimeReached = false;
                bool maxLoadReached = false;

                while (!maxTimeReached && !maxLoadReached && orderList.Count > 0) //now fill with collection stops until cargo is full or stops empty
                {
                    CollectionStop stop = orderList[0];

                    int freq = stop.frequency;
                    string day = dStop.day;

                    int truck = dStop.truckId; 
                    bool correctionFlag = false;

                    // skip if a multiple stop order cant be correctly inserted anymore because the days before
                    if ((freq == 3 && dStop.day != "monday") || ((freq == 2 || freq == 4) && (dStop.day != "monday" && dStop.day != "tuesday")))
                    {
                        if (truck == 0 && freq == 3) //if we are only on truck 0 past tuesday we can add it to truck 1 instead
                        {
                            truck = 1;
                            day = "monday";
                            if (freq != 3 && 1 == rnd.Next(2)) day = "tuesday";
                            correctionFlag = true; //this flag lets us know we are now looking at a different DayStop than orginally
                        }
                        else {          //otherwise an order with freq>2 can not be added anymore so we remove it and its siblings

                            int lastIndex = orderList.Count - 1;
                            (orderList[0], orderList[lastIndex]) = (orderList[lastIndex], orderList[0]);
                            stop.index = ignore.Count;
                            ignore.Add(orderList[lastIndex]);
                            orderList.RemoveAt(lastIndex);


                            foreach (CollectionStop cStop in stop.siblings)
                            {
                                lastIndex--;
                                int id = orderList.IndexOf(cStop); //sadly O(n) but cant be helped
                                (orderList[id], orderList[lastIndex]) = (orderList[lastIndex], orderList[id]);
                                cStop.index = ignore.Count;
                                ignore.Add(orderList[lastIndex]);
                                orderList.RemoveAt(lastIndex);
                            }

                            continue;
                        }
                    }



                    (bool, CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float, bool, bool)[] ordersToAdd = new (bool, CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float, bool, bool)[freq];


                    ordersToAdd[0] = (CheckIfFits(stop, day, truck)); //check if the stop can be added
                    if ((ordersToAdd[0].Item8 == true || ordersToAdd[0].Item9) && !correctionFlag) // if the volume of the ofloadstop/time in day is full/up and we are still on the same DayStop, we move on to next DayStop
                    {
                        maxLoadReached = ordersToAdd[0].Item8;
                        maxTimeReached = ordersToAdd[0].Item9;

                        continue;
                    }

                    int tId = (truck == 0) ? rnd.Next(2) : 1; // choose which truck it goes in, if we are on truck 1 (either because truck 0 is full or we aren't on the original daystop anymore), we try to add siblings to truck 1
                    //what truck the sibling(s) go(es) in is random, if CheckIfFits returns false, the other truck isnt checked (except if we are not on the original daystop(we switched to truck 1), in which case truck 0 is also tested)
                    if (freq == 2) //checking siblings freq=2
                    {
                        if (day == "monday")
                        {
                            ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "thursday", tId));
                            if (ordersToAdd[1].Item1 == false && correctionFlag)
                            {
                                ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "thursday", 0));
                            }
                        }
                        else
                        {
                            ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "friday", tId));
                            if (ordersToAdd[1].Item1 == false && correctionFlag)
                            {
                                ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "friday", 0));
                            }
                        }
                    }
                    if (freq == 3)//checking siblings freq=3
                    {
                        ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "wednesday", tId));
                        if (ordersToAdd[1].Item1 == false && correctionFlag)
                        {
                            ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "wednesday", 0));
                        }
                        tId = (truck == 0) ? rnd.Next(2) : 1;
                        ordersToAdd[2] = (CheckIfFits(stop.siblings[1], "friday", tId));
                        if (ordersToAdd[2].Item1 == false && correctionFlag)
                        {
                            ordersToAdd[2] = (CheckIfFits(stop.siblings[1], "friday", 0));
                        }
                    }
                    if (freq == 4) //checking siblings freq=4
                    {
                        if (day == "monday")
                        {
                            string[] s = { "tuesday", "wednesday", "thursday", "friday"};
                            int r = rnd.Next(4);
                            int c = 1;
                            for (int i = 0; i < 4; i++)
                            {
                                if (r == i) continue;
                                ordersToAdd[c] = (CheckIfFits(stop.siblings[c - 1], s[i], tId));
                                if (ordersToAdd[c].Item1 == false && correctionFlag)
                                {
                                    ordersToAdd[c] = (CheckIfFits(stop.siblings[c - 1], s[i], 0));
                                }
                                tId = (truck == 0) ? rnd.Next(2) : 1;
                                c++;
                            }
                        }
                        if (day == "tuesday")
                        {
                            ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "wednesday", tId));
                            if (ordersToAdd[1].Item1 == false && correctionFlag)
                            {
                                ordersToAdd[1] = (CheckIfFits(stop.siblings[0], "wednesday", 0));
                            }

                            tId = (truck == 0) ? rnd.Next(2) : 1;
                            ordersToAdd[2] = (CheckIfFits(stop.siblings[1], "thursday", tId));
                            if (ordersToAdd[2].Item1 == false && correctionFlag)
                            {
                                ordersToAdd[2] = (CheckIfFits(stop.siblings[1], "thursday", 0));
                            }

                            tId = (truck == 0) ? rnd.Next(2) : 1;
                            ordersToAdd[3] = (CheckIfFits(stop.siblings[2], "friday", tId));
                            if (ordersToAdd[3].Item1 == false && correctionFlag)
                            {
                                ordersToAdd[3] = (CheckIfFits(stop.siblings[2], "friday", 0));
                            }
                        }
                    }

                    bool acc = true; 
                    foreach (var v in ordersToAdd) // check if all siblings and original stop can be added
                    {
                        acc = acc && v.Item1;
                    }

                    if (acc) // if true add
                    {
                        foreach (var v in ordersToAdd)
                        {
                            CollectionStop CStop = v.Item2;
                            DayStop DStop = v.Item3;
                            OfloadStop OStop = v.Item4;
                            List<CollectionStop> list = v.Item5;
                            int volumeCheck = v.Item6;
                            float timeCheck = v.Item7;

                            // if it is possible update the ofload and day stop node with the new values
                            OStop.volume = volumeCheck;
                            DStop.dayTime = timeCheck;
                            tijd = tijd + afstandenMatrix[CStop.matrixId, OStop.matrixId, 1]
                                        + afstandenMatrix[OStop.prev.matrixId, CStop.matrixId, 1]
                                        - afstandenMatrix[OStop.prev.matrixId, OStop.matrixId, 1]
                                        + CStop.loadingTime;

                            // and reddirect the pointers to the correct nodes
                            OStop.prev.next = CStop;
                            CStop.prev = OStop.prev;
                            OStop.prev = CStop;
                            CStop.next = OStop;
                            CStop.ofloadStop = OStop;
                            CStop.dayStop = DStop;



                            CStop.included = true;

                            //remove from orderlist and add to right stoplist
                            int lastIndex = orderList.Count - 1;
                            int id = orderList.IndexOf(CStop); //sadly O(n) but cant be helped
                            (orderList[id], orderList[lastIndex]) = (orderList[lastIndex], orderList[id]);
                            CStop.index = list.Count;
                            list.Add(orderList[lastIndex]);
                            orderList.RemoveAt(lastIndex);

                        }

                        penalty -= (3 * stop.loadingTime * stop.frequency);
                    }
                    else //else remove and put in ignore
                    {
                        foreach (var v in ordersToAdd)
                        {
                            CollectionStop CStop = v.Item2;
                            //remove from orderlist and add to ignore
                            int lastIndex = orderList.Count - 1;
                            int id = orderList.IndexOf(CStop); //sadly O(n) but cant be helped
                            (orderList[id], orderList[lastIndex]) = (orderList[lastIndex], orderList[id]);
                            CStop.index = ignore.Count;
                            ignore.Add(orderList[lastIndex]);
                            orderList.RemoveAt(lastIndex);
                        }

                    }
                }
            }

            int co = ignore.Count;
            foreach (CollectionStop s in orderList)
            {
                s.index = co;
                ignore.Add(s);
                co++;
            }
            

            foreach (var v in monday0.Concat(monday1.Concat(tuesday0.Concat(tuesday1.Concat(wednesday0.Concat(wednesday1.Concat(thursday0.Concat(thursday1.Concat(friday0.Concat(friday1)))))))))) {

                Console.WriteLine(v.index);
            }

            foreach (var v in ignore) {
                Console.WriteLine(v.index);
            }
        }


        private (bool, CollectionStop, DayStop, OfloadStop, List<CollectionStop>, int, float, bool, bool) CheckIfFits(CollectionStop stop, string day, int truck)
        {
            // check if it fits in the leftover cargospace before the dropoff
            // also check if the extra time for the stop fits in the time left in the day
            DayStop dStop = MappingToDayStop(day, truck);
            OfloadStop oStop = (OfloadStop)dStop.prev;
            List<CollectionStop> list = MappingToList(day, truck);
            bool maxLoadReached = false;
            bool maxTimeReached = false;
            bool elligible = true;

            int volumeCheck = oStop.volume + (stop.containerCount * stop.containerVolume);
            if (volumeCheck > cargoSpace) { maxLoadReached = true; elligible = false; }

            float timeCheck = dStop.dayTime + afstandenMatrix[stop.matrixId, oStop.matrixId, 1]
                                            + afstandenMatrix[oStop.prev.matrixId, stop.matrixId, 1]
                                            - afstandenMatrix[oStop.prev.matrixId, oStop.matrixId, 1]
                                            + stop.loadingTime;
            if (timeCheck > maxDayTime) { maxTimeReached = true; elligible = false; }

            return (elligible, stop, dStop, oStop, list, volumeCheck, timeCheck, maxLoadReached, maxTimeReached);
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
                (l1[stop1.index], l1[stop2.index]) = (l1[stop2.index], l1[stop1.index]);
                (stop1.index, stop2.index) = (stop2.index, stop1.index);
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


        public List<CollectionStop> MappingToList(string day, int truck)
        {
            switch (day, truck)
            {
                case ("monday", 0):
                    return monday0;
                case ("monday", 1):
                    return monday1;
                case ("tuesday", 0):
                    return tuesday0;
                case ("tuesday", 1):
                    return tuesday1;
                case ("wednesday", 0):
                    return wednesday0;
                case ("wednesday", 1):
                    return wednesday1;
                case ("thursday", 0):
                    return thursday0;
                case ("thursday", 1):
                    return thursday1;
                case ("friday", 0):
                    return friday0;
                case ("friday", 1):
                    return friday1;
            }
            return new List<CollectionStop>(); //this shouldnt happen
        }

        public (string, int) MappingFromList(List<CollectionStop> ls )
        {
            if (ls.Equals(monday0))
            {
                return ("monday", 0);
            }
            if (ls.Equals(monday1))
            {
                return ("monday", 1);
            }
            if (ls.Equals(tuesday0))
            {
                return ("tuesday", 0);
            }
            if (ls.Equals(tuesday1))
            {
                return ("tuesday", 1);
            }
            if (ls.Equals(wednesday0))
            {
                return ("wednesday", 0);
            }
            if (ls.Equals(wednesday1))
            {
                return ("wednesday", 1);
            }
            if (ls.Equals(thursday0))
            {
                return ("tursday", 0);
            }
            if (ls.Equals(thursday1))
            {
                return ("tursday", 1);
            }
            if (ls.Equals(friday0))
            {
                return ("friday", 0);
            }
            if (ls.Equals(friday1))
            {
                return ("friday", 1);
            }


            return ("false", -1); //this shouldnt happen
        }

        public DayStop MappingToDayStop(string day, int truck)
        {
            List<string> s = new List<string>{ "monday", "tuesday", "wednesday", "thursday", "friday" };
            int index = s.IndexOf(day);
            index = index + 5 * truck + 1;
            return dayStops[index];
        }

    }
}
