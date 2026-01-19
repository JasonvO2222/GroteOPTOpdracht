using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class SimulatedAnnealing
    {
        private double T = 900; //chance variable
        private double T_min = 20; // lowest value for T
        private float a = 0.98f; //chance var factor 
        private int Q = 100000; // iterations before factorizing
        private long zLim = 5000000; // total iterations
        private readonly int[,,] afstandenMatrix;
        private readonly List<CollectionStop> orderList;
        private Oplossing oplossing;
        private static readonly Random rnd = new Random();
        private readonly List<string> days = new List<string>{ "monday", "tuesday", "wednesday", "thursday", "friday" };

        public SimulatedAnnealing(int[,,] matrix, List<CollectionStop> list, float penalty, float chanceVar, float chanceVarMin, float chanceFactor, long totalIterations, int iterationsTConstant)
        {
            if (totalIterations <= iterationsTConstant) { 
                Console.WriteLine("totalIterations cannot be less than (or equal to) iterationsTConstant");
                return;
            }
            afstandenMatrix = matrix;
            orderList = list;
            T = chanceVar;
            T_min = chanceVarMin;
            a = chanceFactor;
            zLim = totalIterations;
            Q = (int)((totalIterations-iterationsTConstant)/(Math.Log(T_min / T, a)));
            Console.WriteLine(Q);


            oplossing = new Oplossing(orderList, afstandenMatrix, penalty);


            // Simulated Annealing
            // Either add/remove/swap action
            // Need one index for remove and 2 for swap

            long z = 1;
            bool TFlag = true;
            while (z <= zLim)
            {

                //Console.WriteLine("m0");
                //Console.WriteLine(oplossing.monday0.Count);
                //Console.WriteLine("m1");
                //Console.WriteLine(oplossing.monday1.Count);
                //Console.WriteLine("t0");
                //Console.WriteLine(oplossing.tuesday0.Count);
                //Console.WriteLine("t1");
                //Console.WriteLine(oplossing.tuesday1.Count);
                //Console.WriteLine("w0");
                //Console.WriteLine(oplossing.wednesday0.Count);
                //Console.WriteLine("w1");
                //Console.WriteLine(oplossing.wednesday1.Count);
                //Console.WriteLine("t0");
                //Console.WriteLine(oplossing.thursday0.Count);
                //Console.WriteLine("t1");
                //Console.WriteLine(oplossing.thursday1.Count);
                //Console.WriteLine("f0");
                //Console.WriteLine(oplossing.friday0.Count);
                //Console.WriteLine("f1");
                //Console.WriteLine(oplossing.friday1.Count);
                //Console.WriteLine("total");
                //Console.WriteLine(oplossing.monday0.Count + oplossing.monday1.Count + oplossing.tuesday0.Count + oplossing.tuesday1.Count +
                //   oplossing.wednesday0.Count + oplossing.wednesday1.Count + oplossing.thursday0.Count + oplossing.thursday1.Count +
                //   oplossing.friday0.Count + oplossing.friday1.Count);
                //Console.WriteLine("ignore");
                //Console.WriteLine(oplossing.ignore.Count);
                //Console.ReadLine();


                if (z % Q == 0 && TFlag) // Decrease T every Q iterations by factorizing with a  (only if T is not already on minimum)
                {
                    T = T * a;
                    if (T < 20) // if T is smaller than minimum: ensure T is not lowered again and set T on minimum
                    {
                        TFlag = false;
                        T = 20;
                    }
                }




                int action = rnd.Next(4);
                int rndInt;
                if (action == 0) // swap
                {
                    //continue;

                    rndInt = rnd.Next(5);
                    string day1 = days[rndInt];
                    int stop1Truck = rnd.Next(2);
                    List<CollectionStop> list1 = oplossing.MappingToList(day1, stop1Truck);
                    int? index1 = oplossing.pickRandomStop(list1);
                    if (index1 == null) { continue; }
                    CollectionStop stop1 = list1[(int)index1];

                    string day2 = "";
                    int stop2Truck = rnd.Next(2);


                    if (stop1.frequency == 3)
                    {
                        day2 = day1;
                    }
                    else if (stop1.frequency == 4)
                    {
                        int dayTotal = 10;
                        foreach (CollectionStop c in stop1.siblings.Concat(new[] { stop1 }))
                        {
                            dayTotal -= MapDayToInt(c.dayStop.day);
                        }
                        day2 = (rnd.Next(2) == 1) ? day1 : days[dayTotal];
                    }
                    else if (stop1.frequency == 2)
                    {
                        //swapping both stops to a different day combination yet to be implemented (if at all)

                        day2 = day1;
                    }


                    List<CollectionStop> list2 = oplossing.MappingToList(day2, stop2Truck);
                    int? index2 = oplossing.pickRandomStop(list2);

                    if (index1 == null || index2 == null || (index1 == index2 && list1 != list2))
                    {
                        continue;
                    }
                    CollectionStop stop2 = list2[(int)index2];
                    if (day1 != day2 && stop2.frequency > 1)
                    {
                        continue;
                    }



                    if (ConsiderSwap(stop1, stop2, out float s1Diff, out float s2Diff, out float timeDiff, out int loadDiff1, out int loadDiff2))
                    {
                        stop1.dayStop.dayTime += s1Diff;
                        stop2.dayStop.dayTime += s2Diff;
                        stop1.ofloadStop.volume += loadDiff1;
                        stop2.ofloadStop.volume += loadDiff2;

                        oplossing.tijd += timeDiff;
                        oplossing.Swap(stop1, stop2);
                        oplossing.SwapStop(stop1, list1, stop2, list2);
                    }

                }

                else if (action == 1) // add
                {
                    //continue;
                    
                    Console.WriteLine("add");
                    int? indexIgnore = oplossing.pickRandomIgnoredStop();

                    if (indexIgnore == null) 
                    {
                        continue;
                    }

                    CollectionStop newStop = oplossing.ignore[(int)indexIgnore];

                                                            //stop to add, stop where isnert, timeDiff, list where to add
                    if (ConsiderAdd(newStop, out (CollectionStop, CollectionStop, float, List<CollectionStop>)[] stopsToAdd, out float penaltyDiff))
                    {
                        foreach (var v in stopsToAdd)
                        {
                            CollectionStop stop = v.Item1;
                            CollectionStop insertLocNode = v.Item2;
                            float timeDiff = v.Item3;
                            insertLocNode.dayStop.dayTime += timeDiff;
                            insertLocNode.ofloadStop.volume += (stop.containerVolume * stop.containerCount);
                            oplossing.tijd += timeDiff;
                            oplossing.Insert(insertLocNode, stop);
                            oplossing.AddStop(stop, v.Item4);

                            Console.WriteLine("add 1");
                        }
                        oplossing.penalty += penaltyDiff;
                    }
                    else
                    {
                        Console.WriteLine("fail");
                    }


                }

                else if (action == 2) // remove
                {

                    //continue;

                    rndInt = rnd.Next(5);
                    string day = days[rndInt];
                    int stopTruck = rnd.Next(2);
                    List<CollectionStop> ls = oplossing.MappingToList(day, stopTruck);
                    int? indexRemove = oplossing.pickRandomStop(ls);

                    if (indexRemove == null)
                    {
                        continue;
                    }

                    CollectionStop removeStop = ls[(int)indexRemove];

                    if (ConsiderRemove(removeStop, out float penaltyDiff, out (CollectionStop, float, List <CollectionStop>)[] stopsToRemove))
                    {
                        foreach (var v in stopsToRemove)
                        {
                            CollectionStop sstop = v.Item1;

                            sstop.dayStop.dayTime += v.Item2;
                            sstop.ofloadStop.volume -= (sstop.containerCount * sstop.containerVolume);
                            oplossing.tijd += v.Item2;
                            oplossing.Remove(sstop);
                            oplossing.RemoveStop(sstop, v.Item3);

                        }

                        oplossing.penalty += penaltyDiff;


                    }
                }

                else if (action == 3) // shift
                {
                    rndInt = rnd.Next(5);
                    string day = days[rndInt];
                    int truck = rnd.Next(2);
                    List<CollectionStop> shiftFromList = oplossing.MappingToList(day, truck);
                    int? index = oplossing.pickRandomStop(shiftFromList);
                    if (index == null) continue;
                    CollectionStop shiftStop = shiftFromList[(int)index];
                    CollectionStop stopShiftAfter;

                    int place = rnd.Next(3);
                    if (place == 0) // Shift within one trip
                    {
                        // Initialize stop we want to shift to
                        int? indexStopShiftAfter = oplossing.pickRandomStop(shiftFromList);
                        if (indexStopShiftAfter == shiftFromList.Count-1 || indexStopShiftAfter == index) continue;
                        stopShiftAfter = shiftFromList[(int)indexStopShiftAfter];

                        // Calculate time difference
                        int differenceTimeRemoveShiftStop = afstandenMatrix[shiftStop.prev.matrixId, shiftStop.next.matrixId, 1]
                            - afstandenMatrix[shiftStop.prev.matrixId, shiftStop.matrixId, 1] - afstandenMatrix[shiftStop.matrixId, shiftStop.next.matrixId, 1];
                        int differenceTimeAddShiftStop = afstandenMatrix[stopShiftAfter.matrixId, shiftStop.matrixId, 1]
                            + afstandenMatrix[shiftStop.matrixId, stopShiftAfter.next.matrixId, 1]
                            - afstandenMatrix[stopShiftAfter.matrixId, stopShiftAfter.next.matrixId, 1];
                        int timeDiff = differenceTimeRemoveShiftStop + differenceTimeAddShiftStop;

                        // Check if time difference is non-positive or roll chance returns true and follow through
                        if (timeDiff <= 0 || (RollChance(timeDiff)) && shiftStop.dayStop.dayTime+timeDiff <= oplossing.maxDayTime)
                        {
                            shiftStop.prev.next = shiftStop.next;
                            shiftStop.next.prev = shiftStop.prev;
                            shiftStop.prev = stopShiftAfter;
                            shiftStop.next = stopShiftAfter.next;
                            // recalculate daytime
                        }

                        // else don't follow through
                    }
                    else if (place == 1) // Shift within one day
                    {
                        // Initialize stop we want to shift to
                        int shiftToTruck = rnd.Next(2);
                        List<CollectionStop> shiftToList = oplossing.MappingToList(day, shiftToTruck);
                        int? indexStopShiftAfter = oplossing.pickRandomStop(shiftToList);
                        if (indexStopShiftAfter == shiftToList.Count - 1 || indexStopShiftAfter == index || indexStopShiftAfter == null) continue; // It will probably never be null because the dayStop is always in there
                        stopShiftAfter = shiftToList[(int)indexStopShiftAfter];

                        (bool, float, float, float) consideredShift = considerShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList);
                        if (consideredShift.Item1)
                            ActualShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList, consideredShift.Item2, consideredShift.Item3, consideredShift.Item4);
                        break;
                    }
                    else if (place == 2) // Shift to anywhere
                    {
                        // Initialize stop we want to shift to
                        int shiftToTruck;
                        string shiftToDay;
                        List<CollectionStop> shiftToList;
                        int? indexStopShiftAfter;

                        switch (shiftStop.frequency)
                        {
                            case 1:
                                // Initialize stop we want to shift to
                                shiftToTruck = rnd.Next(2);
                                shiftToDay = days[rnd.Next(5)];
                                shiftToList = oplossing.MappingToList(shiftToDay, shiftToTruck);
                                indexStopShiftAfter = oplossing.pickRandomStop(shiftToList);
                                if (indexStopShiftAfter == shiftToList.Count - 1 || indexStopShiftAfter == index || indexStopShiftAfter == null) continue;
                                stopShiftAfter = shiftToList[(int)indexStopShiftAfter];

                                (bool, float, float, float) consideredShift1 = considerShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList);
                                if (consideredShift1.Item1)
                                    ActualShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList, consideredShift1.Item2, consideredShift1.Item3, consideredShift1.Item4);
                                break;
                            case 2: 
                                if (shiftStop.dayStop.day == "monday" || shiftStop.dayStop.day == "thursday")
                                {
                                    (int[], string[], List<CollectionStop>[], int?[], CollectionStop[], CollectionStop[]) stopShiftAfters = InitializeFreq2(["tuesday", "friday"], shiftStop);
                                }
                                else if (shiftStop.dayStop.day == "tuesday" || shiftStop.dayStop.day == "friday")
                                {
                                    (int[], string[], List<CollectionStop>[], int?[], CollectionStop[], CollectionStop[]) stopShiftAfters = InitializeFreq2(["monday", "thursday"], shiftStop);
                                    if (stopShiftAfters == (null, null, null, null, null, null)) break;
                                    (bool, float, float, float) consideredShift21 = considerShift(stopShiftAfters.Item6[0].index, stopShiftAfters.Item6[0], stopShiftAfters.Item5[0], oplossing.MappingToList(stopShiftAfters.Item6[0].dayStop.day, stopShiftAfters.Item6[0].dayStop.truckId), stopShiftAfters.Item3[0]);
                                    (bool, float, float, float) consideredShift22 = considerShift(stopShiftAfters.Item6[1].index, stopShiftAfters.Item6[1], stopShiftAfters.Item5[1], oplossing.MappingToList(stopShiftAfters.Item6[1].dayStop.day, stopShiftAfters.Item6[1].dayStop.truckId), stopShiftAfters.Item3[1]);
                                    if (consideredShift21.Item1 && consideredShift22.Item1) // if the shifting of both stops is okay then we implement the actual shift
                                    {
                                        ActualShift(stopShiftAfters.Item6[0].index, stopShiftAfters.Item6[0], stopShiftAfters.Item5[0], oplossing.MappingToList(stopShiftAfters.Item6[0].dayStop.day, stopShiftAfters.Item6[0].dayStop.truckId), stopShiftAfters.Item3[0], consideredShift21.Item2, consideredShift21.Item3, consideredShift21.Item4);
                                        ActualShift(stopShiftAfters.Item6[1].index, stopShiftAfters.Item6[1], stopShiftAfters.Item5[1], oplossing.MappingToList(stopShiftAfters.Item6[1].dayStop.day, stopShiftAfters.Item6[1].dayStop.truckId), stopShiftAfters.Item3[1], consideredShift22.Item2, consideredShift22.Item3, consideredShift22.Item4);
                                    }
                                    break;
                                }
                                break;
                            case 3: break;
                            case 4: // We have to force the stop to shift to the 'missing' day
                                // Initialize stop to one in unused day
                                shiftToTruck = rnd.Next(2);
                                string unusedDay = "";
                                List<string> siblingDays = new List<string>();
                                foreach (CollectionStop sibling in shiftStop.siblings)
                                {
                                    siblingDays.Add(sibling.dayStop.day);
                                }
                                foreach (string weekday in days) // Find day on which no sibling stop is
                                {
                                    if (!siblingDays.Contains(day) && day != shiftStop.dayStop.day)
                                        unusedDay = weekday;
                                }

                                shiftToDay = unusedDay;
                                shiftToList = oplossing.MappingToList(shiftToDay, shiftToTruck);
                                indexStopShiftAfter = oplossing.pickRandomStop(shiftToList);
                                if (indexStopShiftAfter == shiftToList.Count - 1 || indexStopShiftAfter == index || indexStopShiftAfter == null) continue;
                                stopShiftAfter = shiftToList[(int)indexStopShiftAfter];

                                (bool, float, float, float) consideredShift4 = considerShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList);
                                if (consideredShift4.Item1)
                                    ActualShift((int)index, shiftStop, stopShiftAfter, shiftFromList, shiftToList, consideredShift4.Item2, consideredShift4.Item3, consideredShift4.Item4);
                                break;
                        }
                    }
                }

                    z++;
            }


        }

        private (int[], string[], List<CollectionStop>[], int?[], CollectionStop[], CollectionStop[]) InitializeFreq2(List<string> dayShiftToOptions, CollectionStop shiftStopOriginal)
        {
            int shiftToTruck1;
            int shiftToTruck2;
            string shiftToDay1;
            string shiftToDay2;
            List<CollectionStop> shiftToList1;
            List<CollectionStop> shiftToList2;
            int? indexStopShiftAfter1;
            int? indexStopShiftAfter2;
            CollectionStop stopShiftAfter1;
            CollectionStop stopShiftAfter2;
            CollectionStop shiftStop1 = shiftStopOriginal;
            CollectionStop shiftStop2 = shiftStopOriginal.siblings[0];

            shiftToTruck1 = rnd.Next(2);
            shiftToTruck2 = rnd.Next(2);
            int firstDayIndex = rnd.Next(2);
            shiftToDay1 = dayShiftToOptions[firstDayIndex];
            if (firstDayIndex + 1 == 1)
                shiftToDay2 = dayShiftToOptions[1];
            else
                shiftToDay2 = dayShiftToOptions[0];
            shiftToList1 = oplossing.MappingToList(shiftToDay1, shiftToTruck1);
            shiftToList2 = oplossing.MappingToList(shiftToDay2, shiftToTruck2);
            indexStopShiftAfter1 = oplossing.pickRandomStop(shiftToList1);
            indexStopShiftAfter2 = oplossing.pickRandomStop(shiftToList2);
            if (indexStopShiftAfter1 == shiftToList1.Count - 1 || indexStopShiftAfter1 == null) return (null, null, null, null, null, null);
            if (indexStopShiftAfter2 == shiftToList2.Count - 1 || indexStopShiftAfter2 == null) return (null, null, null, null, null, null);
            stopShiftAfter1 = shiftToList1[(int)indexStopShiftAfter1];
            stopShiftAfter2 = shiftToList2[(int)indexStopShiftAfter2];

            int[] shiftToTruck = { shiftToTruck1, shiftToTruck2 };
            string[] shiftToDay = { shiftToDay1, shiftToDay2 };
            List<CollectionStop>[] shiftToList = { shiftToList1, shiftToList2 };
            int?[] indexStopShiftAfter = { indexStopShiftAfter1, indexStopShiftAfter2 };
            CollectionStop[] stopShiftAfter = { stopShiftAfter1, stopShiftAfter2 };
            CollectionStop[] shiftStop = { shiftStop1, shiftStop2 };
            return (shiftToTruck, shiftToDay, shiftToList, indexStopShiftAfter, stopShiftAfter, shiftStop);
        }

        private (bool, float, float, float) considerShift(int indexShiftStop, CollectionStop shiftStop, CollectionStop stopShiftAfter, List<CollectionStop> shiftFromList, List<CollectionStop> shiftToList)
        {
            // Check if cargo doesn't get too heavy with shift
            if (stopShiftAfter.ofloadStop.volume + shiftStop.containerVolume * shiftStop.containerCount > oplossing.cargoSpace) return (false, 0, 0, 0);

            // Calculate time difference
            float differenceTimeRemoveShiftStop = afstandenMatrix[shiftStop.prev.matrixId, shiftStop.next.matrixId, 1]
                - afstandenMatrix[shiftStop.prev.matrixId, shiftStop.matrixId, 1] - afstandenMatrix[shiftStop.matrixId, shiftStop.next.matrixId, 1]
                - shiftStop.loadingTime;
            float differenceTimeAddShiftStop = afstandenMatrix[stopShiftAfter.matrixId, shiftStop.matrixId, 1]
                + afstandenMatrix[shiftStop.matrixId, stopShiftAfter.next.matrixId, 1]
                - afstandenMatrix[stopShiftAfter.matrixId, stopShiftAfter.next.matrixId, 1]
                + shiftStop.loadingTime;
            float timeDiff = differenceTimeRemoveShiftStop + differenceTimeAddShiftStop;

            // Check if time difference is non-positive or (roll chance returns true and max daytime is not exceeded) and follow through
            if (timeDiff <= 0 || (RollChance(timeDiff)) && stopShiftAfter.dayStop.dayTime + differenceTimeAddShiftStop <= oplossing.maxDayTime)
                return (true, differenceTimeRemoveShiftStop, differenceTimeAddShiftStop, timeDiff);

            // else don't follow through
            return (false, differenceTimeRemoveShiftStop, differenceTimeAddShiftStop, timeDiff);
        }

        private void ActualShift(int indexShiftStop, CollectionStop shiftStop, CollectionStop stopShiftAfter, List<CollectionStop> shiftFromList, List<CollectionStop> shiftToList, float differenceTimeRemoveShiftStop, float differenceTimeAddShiftStop, float timeDiff)
        {
            shiftStop.prev.next = shiftStop.next;
            shiftStop.next.prev = shiftStop.prev;
            shiftStop.prev = stopShiftAfter;
            shiftStop.next = stopShiftAfter.next;

            shiftStop.dayStop.dayTime += differenceTimeRemoveShiftStop;
            shiftStop.ofloadStop.volume -= shiftStop.containerVolume * shiftStop.containerCount;

            shiftStop.dayStop = stopShiftAfter.dayStop;
            shiftStop.ofloadStop = stopShiftAfter.ofloadStop;

            shiftStop.dayStop.dayTime += differenceTimeAddShiftStop;
            shiftStop.ofloadStop.volume += shiftStop.containerVolume * shiftStop.containerCount;

            oplossing.tijd += timeDiff;

            shiftFromList[indexShiftStop] = shiftFromList[shiftFromList.Count - 1];
            shiftFromList.RemoveAt(shiftFromList.Count - 1);
            shiftToList.Add(shiftStop);
        }

        private bool ConsiderRemove(CollectionStop removeNode, out float penaltyDiff, out (CollectionStop, float, List<CollectionStop>)[] stopsToRemove)
        {
            // calculate difference in duration
            stopsToRemove = new (CollectionStop, float, List<CollectionStop>)[removeNode.frequency]; //stop, timeDiff, penaltyDiff

            penaltyDiff = 3 * removeNode.frequency * removeNode.loadingTime;

            float totalTimeDiff = 0;

            int arrayCounter = 0;

            foreach (CollectionStop s in removeNode.siblings.Concat(new[] { removeNode }))
            {
                float timeDiff = -(s.loadingTime + afstandenMatrix[s.prev.matrixId, s.matrixId, 1]
                                                  + afstandenMatrix[s.matrixId, s.next.matrixId, 1]
                                                  - afstandenMatrix[s.prev.matrixId, s.next.matrixId, 1]);
                stopsToRemove[arrayCounter] = ((s, timeDiff, oplossing.MappingToList(s.dayStop.day, s.dayStop.truckId)));
                arrayCounter++;
                totalTimeDiff += timeDiff;
            }


            float scoreDiff = totalTimeDiff + penaltyDiff;

            if (scoreDiff <= 0) // if the change is an improvement follow through
            {
                return true;
            }
            else if (RollChance(scoreDiff)) // if the chance roll returns true, follow through
            {
                return true;
            }

            return false; // else don't follow through

        }


        private bool ConsiderAdd(CollectionStop newStop, out (CollectionStop, CollectionStop, float, List<CollectionStop>)[] stopsToAdd, out float penaltyDiff)
        {
            stopsToAdd = new (CollectionStop, CollectionStop, float, List<CollectionStop>)[newStop.frequency];
            int arrayCounter = 0;
            float totalTimeDiff = 0;

            penaltyDiff = -(3 * newStop.frequency * newStop.loadingTime);



            int rndInt;

            if (newStop.frequency == 1)
            {
                rndInt = rnd.Next(5);
                string day = days[rndInt];
                int stopTruck = rnd.Next(2);
                List<CollectionStop> ls = oplossing.MappingToList(day, stopTruck);
                int? insertIndex = oplossing.pickRandomStop(ls);
                if (insertIndex == null)
                {
                    return false;
                }

                CollectionStop insertLocStop = ls[(int)insertIndex];

                stopsToAdd[arrayCounter] = (newStop, insertLocStop, 0f, ls);
                arrayCounter++;

            }
            else if (newStop.frequency == 2)
            {
                rndInt = rnd.Next(2);
                string day1 = days[rndInt];
                int stopTruck1 = rnd.Next(2);
                List<CollectionStop> ls1 = oplossing.MappingToList(day1, stopTruck1);
                int? insertIndex1 = oplossing.pickRandomStop(ls1);
                if (insertIndex1 == null)
                {
                    return false;
                }

                CollectionStop insertLocStop1 = ls1[(int)insertIndex1];

                stopsToAdd[arrayCounter] = (newStop, insertLocStop1, 0f, ls1);
                arrayCounter++;


                CollectionStop sibling = newStop.siblings[0];
                string day2 = days[rndInt + 3];
                int stopTruck2 = rnd.Next(2);
                List<CollectionStop> ls2 = oplossing.MappingToList(day2, stopTruck2);
                int? insertIndex2 = oplossing.pickRandomStop(ls2);
                if (insertIndex2 == null)
                {
                    return false;
                }

                CollectionStop insertLocStop2 = ls2[(int)insertIndex2];

                stopsToAdd[arrayCounter] = (sibling, insertLocStop2, 0f, ls2);
                arrayCounter++;



            }
            else if (newStop.frequency == 3)
            {
                int c = 0;
                foreach (CollectionStop cStop in newStop.siblings.Concat(new[] { newStop }))
                {

                    string day = days[c];
                    int stopTruck = rnd.Next(2);
                    List<CollectionStop> ls = oplossing.MappingToList(day, stopTruck);
                    int? insertIndex = oplossing.pickRandomStop(ls);
                    if (insertIndex == null)
                    {
                        return false;
                    }

                    CollectionStop insertLocStop = ls[(int)insertIndex];

                    stopsToAdd[arrayCounter] = (cStop, insertLocStop, 0f, ls);
                    arrayCounter++;


                    c += 2;
                }
            }
            else if (newStop.frequency == 4)
            {
                rndInt = rnd.Next(5);
                int c = 0;

                for (int i = 0; i < 5; i++)
                {
                    if (rndInt == i) continue;
                    CollectionStop cStop = (c < 3) ? newStop.siblings[c] : newStop;

                    string day = days[i];
                    int stopTruck = rnd.Next(2);
                    List<CollectionStop> ls = oplossing.MappingToList(day, stopTruck);
                    int? insertIndex = oplossing.pickRandomStop(ls);
                    if (insertIndex == null)
                    {
                        return false;
                    }

                    CollectionStop insertLocStop = ls[(int)insertIndex];

                    stopsToAdd[arrayCounter] = (cStop, insertLocStop, 0f, ls);
                    arrayCounter++;


                    c++;
                }
            }

            for (int i = 0; i < stopsToAdd.Length; i++)
            {
                var v = stopsToAdd[i];
                CollectionStop nStop = v.Item1;
                CollectionStop iStop = v.Item2;
                
                // check if adding this node would exceed the cargoSpace 
                if ((iStop.ofloadStop.volume + nStop.containerCount * nStop.containerVolume) > oplossing.cargoSpace)
                {
                    return false;
                }


                float timeDiff = nStop.loadingTime + afstandenMatrix[iStop.matrixId, nStop.matrixId, 1]
                                + afstandenMatrix[nStop.matrixId, iStop.next.matrixId, 1]
                                - afstandenMatrix[iStop.matrixId, iStop.next.matrixId, 1];
                totalTimeDiff += timeDiff;
                

                if (timeDiff + iStop.dayStop.dayTime > oplossing.maxDayTime) //check if adding the node would exceed the dayTimeLimit
                {
                    return false;
                }

                stopsToAdd[i] = (nStop, iStop, timeDiff, v.Item4);
            }



            float scoreDiff = penaltyDiff + totalTimeDiff;

            if (scoreDiff <= 0) return true; // if the add is an improvement in score
            else if (RollChance(scoreDiff)) // else roll chance
            {
                return true;
            }
            else return false;

        }


        private bool ConsiderSwap(CollectionStop s1, CollectionStop s2, out float s1Diff, out float s2Diff, out float timeDiff, out int loadDiff1, out int loadDiff2)
        {
            s1Diff = 0;
            s2Diff = 0;
            timeDiff = 0;

            if (s1.ofloadStop != s2.ofloadStop) //if both stops are not on the same day before the same ofloadStop
            {
                loadDiff1 = s2.containerCount * s2.containerVolume - s1.containerVolume * s1.containerCount;
                loadDiff2 = -loadDiff1;
                // reject if doesnt fit in cargospace
                if (s1.ofloadStop.volume + loadDiff1 > oplossing.cargoSpace ||
                    s2.ofloadStop.volume + loadDiff2 > oplossing.cargoSpace) return false;
            }
            else
            {
                loadDiff1 = 0;
                loadDiff2 = 0;
            }


            // get values from objects :: stop.p <-> stop1 <-> stop1.n ... stop2.p <-> stop2 <-> stop2.n
            int oudNaarS1 = afstandenMatrix[s1.prev.matrixId, s1.matrixId, 1]; // stop1.p -> stop1
            int oudVanS1 = afstandenMatrix[s1.matrixId, s1.next.matrixId, 1];  // stop1 -> stop1.n
            int oudNaarS2 = afstandenMatrix[s2.prev.matrixId, s2.matrixId, 1]; // stop2.p -> stop2
            int oudVanS2 = afstandenMatrix[s2.matrixId, s2.next.matrixId, 1];  // stop2 -> stop2.n
            float s1Tijd = s1.loadingTime;

            int nieuwNaarS1 = afstandenMatrix[s2.prev.matrixId, s1.matrixId, 1]; // stop2.p -> stop1
            int nieuwVanS1 = afstandenMatrix[s1.matrixId, s2.next.matrixId, 1];  // stop1 -> stop2.n
            int nieuwNaarS2 = afstandenMatrix[s1.prev.matrixId, s2.matrixId, 1]; // stop1.p -> stop2
            int nieuwVanS2 = afstandenMatrix[s2.matrixId, s1.next.matrixId, 1];  // stop2 -> stop1.n
            float s2Tijd = s2.loadingTime;

            

            if (s1.next == s2) // if adjacent stop1.p -> stop1 -> stop2 -> stop2.n
            {
                // Nieuw - Oud
                s1Diff = (nieuwNaarS2 - oudNaarS1) + (nieuwVanS1 - oudVanS2) + (afstandenMatrix[s2.matrixId, s2.prev.matrixId, 1] - oudVanS1);
                s2Diff = 0; // (stop1.p -> stop2) + (stop1 -> stop2.n) + (stop2 -> stop1) - (stop1.p -> stop1) - (stop2 -> stop2.n) - (stop1 -> stop1.n)
                timeDiff = s1Diff;

            }
            else if (s2.next == s1) // if adjacent stop2.p -> stop2 -> stop1 -> stop1.n
            {
                s1Diff = 0;
                s2Diff = (nieuwNaarS1 - oudNaarS2) + (nieuwVanS2 - oudVanS1) + (afstandenMatrix[s1.matrixId, s1.prev.matrixId, 1] - oudVanS2);
                timeDiff = s2Diff; // (stop2.p -> stop1) + (stop2 -> stop1.n) + (stop1 -> stop2) - (stop2.p -> stop2) - (stop1 -> stop1.n) - (stop2 -> stop2.n)
            }
            else // otherwise
            {
                s1Diff = (s2Tijd - s1Tijd) + (nieuwNaarS2 - oudNaarS1) + (nieuwVanS2 - oudVanS1); // (stop1.p -> stop2) + (stop2 -> stop1.n) - (stop1.p -> stop1) - (stop1 -> stop1.n)
                s2Diff = (s1Tijd - s2Tijd) + (nieuwNaarS1 - oudNaarS2) + (nieuwVanS1 - oudVanS2); // (stop2.p -> stop1) + (stop1 -> stop2.n) - (stop2.p -> stop2) - (stop2 -> stop2.n)
                timeDiff = s1Diff + s2Diff;
            }

            // reject if doesnt fit in dayTime
            if (s1.dayStop.dayTime + s1Diff > oplossing.maxDayTime ||
                s2.dayStop.dayTime + s2Diff > oplossing.maxDayTime) return false;


            float scoreDiff = timeDiff;

            if (scoreDiff <= 0) return true; //accept if better and roll chance if not
            else if (RollChance(scoreDiff))
            {
                return true;
            }
            else return false;

        }

        private bool RollChance(float diff)
        {
            double result = Math.Exp((-diff) / T);
            return rnd.NextDouble() < result;
        }

        public double GetScore()
        {
            return (oplossing.tijd + oplossing.penalty) / 60;
        }
        public void OutputSolution()
        {
            oplossing.OutputSolution();
        }

        public int MapDayToInt(string day)
        {
            switch (day) {
                case "monday":
                    return 0;
                case "tuesday":
                    return 1;
                case "wednesday":
                    return 2;
                case "thursday":
                    return 3;
                case "friday":
                    return 4;
            }
            Console.WriteLine("dikke error");
            return -1; //shouldnt happen

        }
    }
}
