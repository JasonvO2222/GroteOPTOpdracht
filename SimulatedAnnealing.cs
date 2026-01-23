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
        private readonly List<CollectionStop> orderList;
        private Oplossing oplossing;
        private readonly int[,,] afstandenMatrix;

        private static readonly Random rnd = new Random();

        private double T; //chance variable
        private double T_min; // lowest value for T
        private float T_factor; //chance var factor 
        private long interval; // iterations before factorizing
        private long iterations; // total iterations
        private long iterationsTConstant;
        private int[] weights;
        private int weightsLength;
        private int[] shiftWeights;
        private int shiftWeightsLength;
        public float volumePenalty;
        public float timePenalty;

        public SimulatedAnnealing(int[,,] matrix, List<CollectionStop> list, float penalty, Parameters p)
        {
            afstandenMatrix = matrix;
            orderList = list;
            T = p.T;
            T_min = p.T_min;
            T_factor = p.T_factor;
            iterations = p.iterations;
            iterationsTConstant = p.iterationsTConstant;
            weights = p.weights;
            weightsLength = weights.Length;
            shiftWeights = p.shiftWeights;
            shiftWeightsLength = shiftWeights.Length;
            volumePenalty = p.volumePenalty;
            timePenalty = p.timePenalty;


            if (iterations <= iterationsTConstant)
            {
                Console.WriteLine("totalIterations cannot be less than (or equal to) iterationsTConstant");
                return;
            }

            interval = (int)((iterations - iterationsTConstant) / (Math.Log(T_min / T, T_factor)));


            oplossing = new Oplossing(orderList, afstandenMatrix, penalty);


            // Simulated Annealing
            // Either add/remove/swap/shift action
            // Need one index for remove and 2 for swap

            long iteration_counter = 1;
            bool TFlag = true;
            int action;

            CollectionStop source;
            List<CollectionStop> sourceList;
            int? sourceIndex;
            int sourceDay;
            int sourceTruck;
            float sourceTimeDiff;
            int sourceVolumeDiff;
            Stop target;
            CollectionStop cTarget;
            List<CollectionStop> targetList;
            int? targetIndex;
            int targetDay;
            float targetTimeDiff;
            int targetVolumeDiff;

            float timeDiff;
            float penaltyDiff;
            float realPenaltyDiff;

            int shiftMode;
            int freq;

            //debug trackers
            float timeDiffAdd = 0;
            float penaltyDiffAdd = 0;
            float timeDiffRemove = 0;
            float penaltyDiffRemove = 0;
            float timeDiffSwap = 0;
            float timeDiffShift = 0;






            while (iteration_counter <= iterations)
            {

                if (TFlag && iteration_counter % interval == 0) // Decrease T every Q iterations by factorizing with a  (only if T is not already on minimum)
                {
                    T = T * T_factor;
                    volumePenalty *= 1.3f;
                    timePenalty *= 1.3f;
                    if (T < T_min) // if T is smaller than minimum: ensure T is not lowered again and set T on minimum
                    {
                        TFlag = false;
                        T = T_min;
                    }
                }




                action = rnd.Next(weightsLength);
                action = weights[action];
                if (action == 0) // swap
                {
                    //continue;
                    sourceDay = rnd.Next(5);

                    sourceList = oplossing.MappingToList(sourceDay, rnd.Next(2));
                    sourceIndex = oplossing.pickRandomStop(sourceList);
                    if (sourceIndex == null) { continue; }
                    source = sourceList[(int)sourceIndex];


                    if (source.frequency == 4) // stop can switch to skipped day
                    {
                        if (rnd.Next(2) == 1)
                        {
                            targetDay = sourceDay;
                        }
                        else
                        {
                            targetDay = 10;
                            foreach (CollectionStop c in source.siblings.Append(source))
                            {
                                targetDay -= c.dayStop.day;
                            }
                        }

                    }
                    else if (source.frequency == 2 || source.frequency == 3) // stop must stay on same day
                    {
                        targetDay = sourceDay;
                    }
                    else
                    {
                        targetDay = rnd.Next(5);
                    }


                    targetList = oplossing.MappingToList(targetDay, rnd.Next(2));
                    targetIndex = oplossing.pickRandomStop(targetList);

                    if (targetIndex == null || (sourceIndex == targetIndex && targetList == sourceList))
                    {
                        continue;
                    }

                    cTarget = targetList[(int)targetIndex];

                    if (targetDay != sourceDay && cTarget.frequency > 1)
                    {
                        continue;
                    }



                    if (ConsiderSwap(source, cTarget, out sourceTimeDiff, out targetTimeDiff, out timeDiff, out sourceVolumeDiff, out targetVolumeDiff, out penaltyDiff))
                    {
                        source.dayStop.dayTime += sourceTimeDiff;
                        cTarget.dayStop.dayTime += targetTimeDiff;
                        source.ofloadStop.volume += sourceVolumeDiff;
                        cTarget.ofloadStop.volume += targetVolumeDiff;

                        oplossing.tijd += timeDiff;
                        oplossing.penalty += penaltyDiff;
                        oplossing.Swap(source, cTarget);
                        oplossing.SwapStop(source, sourceList, cTarget, targetList);

                        timeDiffSwap += timeDiff;
                    }

                }

                else if (action == 1) // add
                {
                    //continue;
                    sourceIndex = oplossing.pickRandomIgnoredStop();
                    sourceList = oplossing.ignore;

                    if (sourceIndex == null)
                    {
                        continue;
                    }

                    source = sourceList[(int)sourceIndex];

                    //stop to add, stop where isnert, timeDiff, list where to add
                    if (ConsiderAdd(source, out (CollectionStop, Stop, float, List<CollectionStop>)[] stopsToAdd, out penaltyDiff, out realPenaltyDiff))
                    {
                        foreach (var v in stopsToAdd)
                        {
                            (source, target, timeDiff, targetList) = v;
                            target.dayStop.dayTime += timeDiff;
                            target.ofloadStop.volume += (source.containerVolume * source.containerCount);
                            oplossing.tijd += timeDiff;
                            oplossing.Insert(target, source);
                            oplossing.AddStop(source, targetList);
                            timeDiffAdd += timeDiff;
                        }
                        oplossing.penalty += penaltyDiff;
                        oplossing.realPenalty += realPenaltyDiff;
                        penaltyDiffAdd += penaltyDiff;
                    }

                }

                else if (action == 2) // remove
                {
                    //continue;
                    sourceList = oplossing.MappingToList(rnd.Next(5), rnd.Next(2));
                    sourceIndex = oplossing.pickRandomStop(sourceList);

                    if (sourceIndex == null)
                    {
                        continue;
                    }

                    source = sourceList[(int)sourceIndex];

                    if (ConsiderRemove(source, out penaltyDiff, out realPenaltyDiff, out (CollectionStop, float, List<CollectionStop>)[] stopsToRemove))
                    {
                        foreach (var v in stopsToRemove)
                        {
                            (source, timeDiff, sourceList) = v;

                            source.dayStop.dayTime += timeDiff;
                            source.ofloadStop.volume -= (source.containerCount * source.containerVolume);
                            oplossing.tijd += timeDiff;
                            oplossing.Remove(source);
                            oplossing.RemoveStop(source, sourceList);


                            timeDiffRemove += timeDiff;
                        }
                        oplossing.penalty += penaltyDiff;
                        oplossing.realPenalty += realPenaltyDiff;
                        penaltyDiffRemove += penaltyDiff;
                    }
                }

                else if (action == 3) // shift
                {
                    shiftMode = rnd.Next(shiftWeightsLength); // 0 means within ride, 1 means within day, 2 means within week
                    shiftMode = shiftWeights[shiftMode];
                    sourceDay = rnd.Next(5);
                    sourceTruck = rnd.Next(2);
                    sourceList = oplossing.MappingToList(sourceDay, sourceTruck);
                    sourceIndex = oplossing.pickRandomStop(sourceList);
                    if (sourceIndex == null)
                    {
                        continue;
                    }

                    source = sourceList[(int)sourceIndex];
                    freq = (shiftMode == 2 && source.frequency == 2) ? 2 : 1;



                    if (ConsiderShift(source, sourceList, shiftMode, out penaltyDiff, out (CollectionStop, List<CollectionStop>, Stop, List<CollectionStop>, float, float)[] stopsToShift))
                    {
                        for (int i = 0; i < freq; i++)
                        {
                            (source, sourceList, target, targetList, sourceTimeDiff, targetTimeDiff) = stopsToShift[i];

                            if (target.next == source) continue;

                            source.dayStop.dayTime += sourceTimeDiff;
                            target.dayStop.dayTime += targetTimeDiff;
                            source.ofloadStop.volume -= (source.containerCount * source.containerVolume);
                            target.ofloadStop.volume += (source.containerCount * source.containerVolume);

                            oplossing.tijd += sourceTimeDiff + targetTimeDiff;
                            oplossing.Shift(source, target);
                            oplossing.ShiftStop(source, sourceList, targetList);

                            timeDiffShift += sourceTimeDiff + targetTimeDiff;

                        }

                        oplossing.penalty += penaltyDiff;

                    }
                }

                iteration_counter++;
            }




            Console.WriteLine($"timeDiffAdd: {timeDiffAdd}");
            Console.WriteLine($"penaltyDiffAdd: {penaltyDiffAdd}");
            Console.WriteLine($"timeDiffRemove: {timeDiffRemove}");
            Console.WriteLine($"penaltyDiffRemove: {penaltyDiffRemove}");
            Console.WriteLine($"timeDiffSwap: {timeDiffSwap}");
            Console.WriteLine($"timeDiffShift: {timeDiffShift}");



        }


        private bool ConsiderShift(CollectionStop source, List<CollectionStop> sourceList, int shiftMode, out float penaltyDiff, out (CollectionStop, List<CollectionStop>, Stop, List<CollectionStop>, float, float)[] stopsToShift)
        {
            Stop target;
            List<CollectionStop> targetList;
            int? targetIndex;
            int sourceDay;
            int targetDay;
            int targetTruck;
            float timeDiff;
            float sourceTimeDiff;
            float targetTimeDiff;
            penaltyDiff = 0;

            stopsToShift = new (CollectionStop, List<CollectionStop>, Stop, List<CollectionStop>, float, float)[source.frequency];


            if (shiftMode < 2)
            {
                targetDay = source.dayStop.day;
                targetTruck = rnd.Next(2);
                targetList = (shiftMode == 0) ? sourceList : oplossing.MappingToList(source.dayStop.day, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay-1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                    if (target == source || target.next == source) return false;
                }


                sourceTimeDiff = afstandenMatrix[source.prev.matrixId, source.next.matrixId, 1]
                    - afstandenMatrix[source.prev.matrixId, source.matrixId, 1]
                    - afstandenMatrix[source.matrixId, source.next.matrixId, 1]
                    - source.loadingTime;
                targetTimeDiff = afstandenMatrix[target.matrixId, source.matrixId, 1]
                    + afstandenMatrix[source.matrixId, target.next.matrixId, 1]
                    - afstandenMatrix[target.matrixId, target.next.matrixId, 1]
                    + source.loadingTime;
                timeDiff = sourceTimeDiff + targetTimeDiff;

                //check if adding/removing the node would (exceed)/(take away excess) the dayTimeLimit/cargSpace 
                penaltyDiff += oplossing.calcTimePenalty(source.dayStop.dayTime, source.dayStop.dayTime + sourceTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcTimePenalty(target.dayStop.dayTime, target.dayStop.dayTime + targetTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(source.ofloadStop.volume, (source.ofloadStop.volume - source.containerCount * source.containerVolume), volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(target.ofloadStop.volume, (target.ofloadStop.volume + source.containerCount * source.containerVolume), volumePenalty);

                stopsToShift[0] = (source, sourceList, target, targetList, sourceTimeDiff, targetTimeDiff);
            }
            else if (source.frequency == 1)
            {
                targetDay = rnd.Next(5);
                targetTruck = rnd.Next(2);
                targetList = oplossing.MappingToList(targetDay, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay-1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                    if (target == source) return false;
                }

                sourceTimeDiff = afstandenMatrix[source.prev.matrixId, source.next.matrixId, 1]
                    - afstandenMatrix[source.prev.matrixId, source.matrixId, 1]
                    - afstandenMatrix[source.matrixId, source.next.matrixId, 1]
                    - source.loadingTime;
                targetTimeDiff = afstandenMatrix[target.matrixId, source.matrixId, 1]
                    + afstandenMatrix[source.matrixId, target.next.matrixId, 1]
                    - afstandenMatrix[target.matrixId, target.next.matrixId, 1]
                    + source.loadingTime;
                timeDiff = sourceTimeDiff + targetTimeDiff;

                
                //check if adding/removing the node would (exceed)/(take away excess) the dayTimeLimit/cargSpace 
                penaltyDiff += oplossing.calcTimePenalty(source.dayStop.dayTime, source.dayStop.dayTime + sourceTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcTimePenalty(target.dayStop.dayTime, target.dayStop.dayTime + targetTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(source.ofloadStop.volume, (source.ofloadStop.volume - source.containerCount * source.containerVolume), volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(target.ofloadStop.volume, (target.ofloadStop.volume + source.containerCount * source.containerVolume), volumePenalty);

                stopsToShift[0] = (source, sourceList, target, targetList, sourceTimeDiff, targetTimeDiff);
            }
            else if (source.frequency == 4)
            {
                targetDay = 10;
                foreach (CollectionStop s in source.siblings.Append(source))
                {
                    targetDay -= s.dayStop.day;
                }
                targetTruck = rnd.Next(2);
                targetList = oplossing.MappingToList(targetDay, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay-1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                    if (target == source) return false;
                }


                sourceTimeDiff = afstandenMatrix[source.prev.matrixId, source.next.matrixId, 1]
                    - afstandenMatrix[source.prev.matrixId, source.matrixId, 1]
                    - afstandenMatrix[source.matrixId, source.next.matrixId, 1]
                    - source.loadingTime;
                targetTimeDiff = afstandenMatrix[target.matrixId, source.matrixId, 1]
                    + afstandenMatrix[source.matrixId, target.next.matrixId, 1]
                    - afstandenMatrix[target.matrixId, target.next.matrixId, 1]
                    + source.loadingTime;
                timeDiff = sourceTimeDiff + targetTimeDiff;

                //check if adding/removing the node would (exceed)/(take away excess) the dayTimeLimit/cargSpace 
                penaltyDiff += oplossing.calcTimePenalty(source.dayStop.dayTime, source.dayStop.dayTime + sourceTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcTimePenalty(target.dayStop.dayTime, target.dayStop.dayTime + targetTimeDiff, volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(source.ofloadStop.volume, (source.ofloadStop.volume - source.containerCount * source.containerVolume), volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(target.ofloadStop.volume, (target.ofloadStop.volume + source.containerCount * source.containerVolume), volumePenalty);

                stopsToShift[0] = (source, sourceList, target, targetList, sourceTimeDiff, targetTimeDiff);
            }
            else if (source.frequency == 2)
            {
                CollectionStop temp;
                sourceDay = source.dayStop.day;
                int[] mapping = (sourceDay % 3 == 0) ? new int[2] { 1, 4 } : new int[2] { 0, 3 };
                timeDiff = 0;

                for (int i = 0; i < 2; i++)
                {
                    temp = (i == 0) ? source : source.siblings[0];
                    targetDay = mapping[i];
                    targetTruck = rnd.Next(2);
                    targetList = oplossing.MappingToList(targetDay, targetTruck);
                    targetIndex = oplossing.pickRandomStop(targetList);
                    sourceList = oplossing.MappingToList(temp.dayStop.day, temp.dayStop.truckId);
                    if (targetIndex == null)
                    {
                        target = oplossing.MappingToDayStop(targetDay-1, targetTruck);
                    }
                    else
                    {
                        target = targetList[(int)targetIndex];
                        if (target == temp || target.next == temp) return false;
                    }

                    sourceTimeDiff = afstandenMatrix[temp.prev.matrixId, temp.next.matrixId, 1]
                    - afstandenMatrix[temp.prev.matrixId, temp.matrixId, 1]
                    - afstandenMatrix[temp.matrixId, temp.next.matrixId, 1]
                    - temp.loadingTime;
                    targetTimeDiff = afstandenMatrix[target.matrixId, temp.matrixId, 1]
                        + afstandenMatrix[temp.matrixId, target.next.matrixId, 1]
                        - afstandenMatrix[target.matrixId, target.next.matrixId, 1]
                        + temp.loadingTime;
                    timeDiff += sourceTimeDiff + targetTimeDiff;

                    //check if adding/removing the node would (exceed)/(take away excess) the dayTimeLimit/cargSpace 
                    penaltyDiff += oplossing.calcTimePenalty(temp.dayStop.dayTime, temp.dayStop.dayTime + sourceTimeDiff, volumePenalty);
                    penaltyDiff += oplossing.calcTimePenalty(target.dayStop.dayTime, target.dayStop.dayTime + targetTimeDiff, volumePenalty);
                    penaltyDiff += oplossing.calcVolumePenalty(temp.ofloadStop.volume, (temp.ofloadStop.volume - temp.containerCount * temp.containerVolume), volumePenalty);
                    penaltyDiff += oplossing.calcVolumePenalty(target.ofloadStop.volume, (target.ofloadStop.volume + temp.containerCount * temp.containerVolume), volumePenalty);

                    stopsToShift[i] = (temp, sourceList, target, targetList, sourceTimeDiff, targetTimeDiff);
                }
            }
            else // cant switch between days if freq -- 3
            {
                return false;
            }

            if (timeDiff <= 0) // if the change is an improvement follow through
            {
                return true;
            }
            else if (RollChance(timeDiff)) // if the chance roll returns true, follow through
            {
                return true;
            }

            return false; // else don't follow through

        }

        private bool ConsiderRemove(CollectionStop removeNode, out float penaltyDiff, out float realPenaltyDiff, out (CollectionStop, float, List<CollectionStop>)[] stopsToRemove)
        {
            // calculate difference in duration
            stopsToRemove = new (CollectionStop, float, List<CollectionStop>)[removeNode.frequency]; //stop, timeDiff, penaltyDiff

            penaltyDiff = 3 * removeNode.frequency * removeNode.loadingTime;
            realPenaltyDiff = 3 * removeNode.frequency * removeNode.loadingTime;

            float totalTimeDiff = 0;
            float timeDiff;
            int c = 0;

            foreach (CollectionStop s in removeNode.siblings.Append(removeNode))
            {
                timeDiff = -(s.loadingTime + afstandenMatrix[s.prev.matrixId, s.matrixId, 1]
                                                  + afstandenMatrix[s.matrixId, s.next.matrixId, 1]
                                                  - afstandenMatrix[s.prev.matrixId, s.next.matrixId, 1]);

                //check if removing the node would remove excess on the dayTimeLimit and remove penalty
                penaltyDiff += oplossing.calcTimePenalty(s.dayStop.dayTime, s.dayStop.dayTime + timeDiff, volumePenalty);

                // check if removing the node would remove excess in the cargoSpace and remove penalty accordingly
                penaltyDiff += oplossing.calcVolumePenalty(s.ofloadStop.volume, (s.ofloadStop.volume -  s.containerCount * s.containerVolume), volumePenalty);

                stopsToRemove[c] = ((s, timeDiff, oplossing.MappingToList(s.dayStop.day, s.dayStop.truckId)));
                c++;
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


        private bool ConsiderAdd(CollectionStop newStop, out (CollectionStop, Stop, float, List<CollectionStop>)[] stopsToAdd, out float penaltyDiff, out float realPenaltyDiff)
        {
            List<CollectionStop> targetList;
            CollectionStop source;
            Stop target;
            int? targetIndex;
            int targetDay;
            int targetTruck;
            stopsToAdd = new (CollectionStop, Stop, float, List<CollectionStop>)[newStop.frequency];

            float timeDiff;
            float totalTimeDiff = 0;

            penaltyDiff = -(3 * newStop.frequency * newStop.loadingTime);
            realPenaltyDiff = -(3 * newStop.frequency * newStop.loadingTime);

            // find targets (destinations) for each stop in order that will be added
            if (newStop.frequency == 1)
            {
                targetDay = rnd.Next(5);
                targetTruck = rnd.Next(2);
                targetList = oplossing.MappingToList(targetDay, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay-1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                }

                stopsToAdd[0] = (newStop, target, 0f, targetList);
            }
            else if (newStop.frequency == 2)
            {
                targetDay = rnd.Next(2);
                targetTruck = rnd.Next(2);
                targetList = oplossing.MappingToList(targetDay, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay - 1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                }


                stopsToAdd[0] = (newStop, target, 0f, targetList);

                source = newStop.siblings[0];
                targetDay += 3;
                targetTruck = rnd.Next(2);
                targetList = oplossing.MappingToList(targetDay, targetTruck);
                targetIndex = oplossing.pickRandomStop(targetList);
                if (targetIndex == null)
                {
                    target = oplossing.MappingToDayStop(targetDay - 1, targetTruck);
                }
                else
                {
                    target = targetList[(int)targetIndex];
                }

                stopsToAdd[1] = (source, target, 0f, targetList);
            }
            else if (newStop.frequency == 3)
            {
                int c = 0;
                foreach (CollectionStop cStop in newStop.siblings.Append(newStop))
                {
                    targetDay = c * 2;
                    targetTruck = rnd.Next(2);
                    targetList = oplossing.MappingToList(targetDay, targetTruck);
                    targetIndex = oplossing.pickRandomStop(targetList);
                    if (targetIndex == null)
                    {
                        target = oplossing.MappingToDayStop(targetDay - 1, targetTruck);
                    }
                    else
                    {
                        target = targetList[(int)targetIndex];
                    }

                    stopsToAdd[c] = (cStop, target, 0f, targetList);
                    c += 1;
                }
            }
            else if (newStop.frequency == 4)
            {
                int r = rnd.Next(5);
                int c = 0;
                targetDay = 0;

                foreach (CollectionStop cStop in newStop.siblings.Append(newStop))
                {
                    if (targetDay == r) targetDay++;
                    targetTruck = rnd.Next(2);
                    targetList = oplossing.MappingToList(targetDay, targetTruck);
                    targetIndex = oplossing.pickRandomStop(targetList);
                    if (targetIndex == null)
                    {
                        target = oplossing.MappingToDayStop(targetDay - 1, targetTruck);
                    }
                    else
                    {
                        target = targetList[(int)targetIndex];
                    }

                    stopsToAdd[c] = (cStop, target, 0f, targetList);
                    c++;
                    targetDay++;
                }
            }

            // Consider each of the source - target (destination) pairs and evaluate
            for (int i = 0; i < stopsToAdd.Length; i++)
            {
                (source, target, timeDiff, targetList) = stopsToAdd[i];
                
                // check if adding this node would exceed the cargoSpace and add penalty accordingly
                penaltyDiff += oplossing.calcVolumePenalty(target.ofloadStop.volume, (target.ofloadStop.volume + source.containerCount * source.containerVolume), volumePenalty);

                timeDiff = source.loadingTime + afstandenMatrix[target.matrixId, source.matrixId, 1]
                                + afstandenMatrix[source.matrixId, target.next.matrixId, 1]
                                - afstandenMatrix[target.matrixId, target.next.matrixId, 1];
                totalTimeDiff += timeDiff;
                

                //check if adding the node would exceed the dayTimeLimit and add penalty
                penaltyDiff += oplossing.calcTimePenalty(target.dayStop.dayTime, target.dayStop.dayTime + timeDiff, volumePenalty);

                stopsToAdd[i] = (source, target, timeDiff, targetList);
            }



            float scoreDiff = penaltyDiff + totalTimeDiff;

            if (scoreDiff <= 0) return true; // if the add is an improvement in score
            else if (RollChance(scoreDiff)) // else roll chance
            {
                return true;
            }
            else return false;

        }


        private bool ConsiderSwap(CollectionStop s1, CollectionStop s2, out float s1Diff, out float s2Diff, out float timeDiff, out int loadDiff1, out int loadDiff2, out float penaltyDiff)
        {
            s1Diff = 0;
            s2Diff = 0;
            timeDiff = 0;
            penaltyDiff = 0;

            if (s1.ofloadStop != s2.ofloadStop) //if both stops are not on the same day before the same ofloadStop
            {
                loadDiff1 = s2.containerCount * s2.containerVolume - s1.containerVolume * s1.containerCount;
                loadDiff2 = -loadDiff1;
                // add penalty if doesnt fit in cargospace
                penaltyDiff += oplossing.calcVolumePenalty(s1.ofloadStop.volume, s1.ofloadStop.volume + loadDiff1, volumePenalty);
                penaltyDiff += oplossing.calcVolumePenalty(s2.ofloadStop.volume, s2.ofloadStop.volume + loadDiff2, volumePenalty);
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

            // add penalty if doesnt fit in dayTime
            penaltyDiff += oplossing.calcTimePenalty(s1.dayStop.dayTime, s1.dayStop.dayTime + s1Diff, volumePenalty);
            penaltyDiff += oplossing.calcTimePenalty(s2.dayStop.dayTime, s2.dayStop.dayTime + s2Diff, volumePenalty);


            float scoreDiff = timeDiff + penaltyDiff;

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
            return (oplossing.tijd + oplossing.realPenalty);
        }

        public (double, double) GetScoreDetailed()
        {
            return (oplossing.tijd, oplossing.realPenalty);
        }
        public void OutputSolution()
        {
            oplossing.OutputSolution();
        }

        public bool Check()
        {
            return oplossing.Check();
        }

    }
}
