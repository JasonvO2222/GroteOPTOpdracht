using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class SimulatedAnnealing
    {
        private double T; //chance variable
        private float a = 0.98f; //chance var factor 
        private int Q = 100000; // iterations before factorizing
        private readonly int[,,] afstandenMatrix;
        private readonly List<CollectionStop> orderList;
        private Oplossing oplossing;
        private static readonly Random rnd = new Random();

        public SimulatedAnnealing(int[,,] matrix, List<CollectionStop> list, float penalty)
        {
            afstandenMatrix = matrix;
            orderList = list;
            T = 1;
            Console.WriteLine($"Score voor startoplossing: {(penalty) / 60}");
            oplossing = new Oplossing(orderList, afstandenMatrix, penalty);

            Console.WriteLine($"Score na startoplossing: {(oplossing.penalty + oplossing.tijd) / 60}");
            Console.WriteLine($"Penalty: {oplossing.penalty}");
            Console.WriteLine($"Tijd: {oplossing.tijd}");


            // Simulated Annealing
            // Either add/remove/swap action
            // Need one index for remove and 2 for swap

            Stopwatch timer = new Stopwatch();
            timer.Start();

            int z = 1; 
            while (z <= 5000000)
            {
                if (z % Q == 0) // Decrease T every Q iterations by factorizing with a
                {
                    T = T * a;
                }

                int action = rnd.Next(3);
                if (action == 0) // swap
                {
                    int? index1 = oplossing.pickRandomStop();
                    int? index2 = oplossing.pickRandomStop();

                    if (index1 == null || index1 == index2)
                    {
                        continue;
                    }

                    int i1 = (int)index1;
                    int i2 = (int)index2;

                    CollectionStop s1 = oplossing.stops[i1];
                    CollectionStop s2 = oplossing.stops[i2];

                    if (ConsiderSwap(s1, s2, out float s1Diff, out float s2Diff, out float timeDiff, out float penaltyDiff, out int loadDiff1, out int loadDiff2))
                    {
                        s1.dayStop.dayTime += s1Diff;
                        s2.dayStop.dayTime += s2Diff;
                        s1.ofloadStop.volume += loadDiff1;
                        s2.ofloadStop.volume += loadDiff2;

                        oplossing.tijd += timeDiff;
                        oplossing.penalty += penaltyDiff;
                        oplossing.Swap(s1, s2);
                    }

                }

                else if (action == 1) // add
                {

                    int? indexIgnore = oplossing.pickRandomIgnoredStop();
                    int? indexInsert = oplossing.pickRandomStop();

                    if (indexIgnore == null) 
                    {
                        continue;
                    }


                    CollectionStop insertNode = oplossing.stops[(int)indexInsert];
                    CollectionStop newStop = oplossing.ignore[(int)indexIgnore];


                    if (ConsiderAdd(insertNode, newStop, out float timeDiff, out float penaltyDiff))
                    {
                        insertNode.dayStop.dayTime += timeDiff;
                        insertNode.ofloadStop.volume += (newStop.containerVolume * newStop.containerCount);
                        oplossing.tijd += timeDiff;
                        oplossing.penalty += penaltyDiff;
                        oplossing.Insert(insertNode, newStop);
                        oplossing.AddStop((int)indexIgnore);
                    }


                }

                else if (action == 2) // remove
                {
                    int? indexRemove = oplossing.pickRandomStop();

                    if (indexRemove == null)
                    {
                        continue;
                    }

                    CollectionStop removeStop = oplossing.stops[(int)indexRemove];

                    if (ConsiderRemove(removeStop, out float timeDiff, out float penaltyDiff))
                    {
                        removeStop.dayStop.dayTime += timeDiff;
                        removeStop.ofloadStop.volume -= (removeStop.containerCount * removeStop.containerVolume);
                        oplossing.tijd += timeDiff;
                        oplossing.penalty += penaltyDiff;
                        oplossing.Remove(removeStop);
                        oplossing.RemoveStop((int)indexRemove);
                    }
                }

                z++;
            }


            timer.Stop();

            Console.WriteLine($"Duration: {timer.Elapsed}");

            Console.WriteLine($"Score na simulated annealing: {(oplossing.penalty + oplossing.tijd) / 60}");
            Console.WriteLine($"Penalty: {oplossing.penalty}");
            Console.WriteLine($"Tijd: {oplossing.tijd}");
            oplossing.OutputSolution();


        }

        private bool ConsiderRemove(CollectionStop removeNode, out float timeDiff, out float penaltyDiff)
        {
            // calculate difference in duration
            timeDiff = -(removeNode.loadingTime + afstandenMatrix[removeNode.prev.matrixId, removeNode.matrixId, 1]
                                                  + afstandenMatrix[removeNode.matrixId, removeNode.next.matrixId, 1]
                                                  - afstandenMatrix[removeNode.prev.matrixId, removeNode.next.matrixId, 1]);

            if (oplossing.CorrectPickup(removeNode)) // check if the penalty needs to be added or if the penalty is already given
            {
                penaltyDiff = 3 * removeNode.frequency * removeNode.loadingTime;
            }
            else penaltyDiff = 0;

            float scoreDiff = timeDiff + penaltyDiff;

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


        private bool ConsiderAdd(CollectionStop insertNode, CollectionStop newStop, out float timeDiff, out float penaltyDiff)
        {
            timeDiff = 0;
            penaltyDiff = 0;
            // check if adding this node would exceed the cargoSpace 
            if ((insertNode.ofloadStop.volume + newStop.containerCount * newStop.containerVolume) > oplossing.cargoSpace)
            {
                return false;
            }

            timeDiff = newStop.loadingTime + afstandenMatrix[insertNode.matrixId, newStop.matrixId, 1]
                                           + afstandenMatrix[newStop.matrixId, insertNode.next.matrixId, 1]
                                           - afstandenMatrix[insertNode.matrixId, insertNode.next.matrixId, 1];
            if ( timeDiff + insertNode.dayStop.dayTime > oplossing.maxDayTime) //check if adding the node would exceed the dayTimeLimit
            {
                return false;
            }

            if (oplossing.CorrectPickup(newStop, insertNode.dayStop.day, true)) //check if adding it satisfies the order and the penalty can be removed
            {
                penaltyDiff = -(3 * newStop.frequency * newStop.loadingTime);
            }

            float scoreDiff = penaltyDiff + timeDiff;

            if (scoreDiff <= 0) return true; // if the add is an improvement in score
            else if (RollChance(scoreDiff)) // else roll chance
            {
                return true;
            }
            else return false;

        }


        private bool ConsiderSwap(CollectionStop s1, CollectionStop s2, out float s1Diff, out float s2Diff, out float timeDiff, out float penaltyDiff, out int loadDiff1, out int loadDiff2)
        {
            penaltyDiff = 0;
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

            

            if (s1.next == s2) // if adjacent stop1 -> stop2
            {               // Nieuw - Oud
                s1Diff = (nieuwNaarS2 - oudNaarS1) + (nieuwVanS1 - oudVanS2) + (afstandenMatrix[s2.matrixId, s2.prev.matrixId, 1] - oudVanS1);
                s2Diff = 0; // (stop1.p -> stop2) + (stop1 -> stop2.n) + (stop2 -> stop1) - (stop1.p -> stop1) - (stop2 -> stop2.n) - (stop1 -> stop1.n)
                timeDiff = s1Diff;

            }
            else if (s2.next == s1)
            {
                s1Diff = 0;
                s2Diff = (nieuwNaarS1 - oudNaarS2) + (nieuwVanS2 - oudVanS1) + (afstandenMatrix[s1.matrixId, s1.prev.matrixId, 1] - oudVanS2);
                timeDiff = s1Diff; // (stop2.p -> stop1) + (stop2 -> stop1.n) + (stop1 -> stop2) - (stop2.p -> stop2) - (stop1 -> stop1.n) - (stop2 -> stop2.n)
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

            //calculate diff by penalties
            float penaltyDiff1 = 0;
            float penaltyDiff2 = 0;

            if (!(s1.orderId == s2.orderId)) //check before and after whether the penalty is there or not and update penaltyDiff accordingly
            {
                bool b1 = oplossing.CorrectPickup(s1);
                bool b2 = oplossing.CorrectPickup(s2);

                bool nieuwB1 = oplossing.CorrectPickup(s1, s2.dayStop.day);
                bool nieuwB2 = oplossing.CorrectPickup(s2, s1.dayStop.day);

                if (b1 && !nieuwB1) penaltyDiff1 = s1.loadingTime * s1.frequency * 3;
                else if (!b1 && nieuwB1) penaltyDiff1 = -(s1.loadingTime * s1.frequency * 3);
                if (b2 && !nieuwB2) penaltyDiff2 = s2.loadingTime * s2.frequency * 3;
                else if (!b2 && nieuwB2) penaltyDiff2 = -(s2.loadingTime * s2.frequency * 3);
            }
            penaltyDiff = penaltyDiff1 + penaltyDiff2;
            float scoreDiff = penaltyDiff + s1Diff + s2Diff;

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


    }
}
