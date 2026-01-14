
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GroteOPTOpdracht
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //initialize datastructures
            int[,,] afstandenMatrix = new int[1099, 1099, 2];

            // parse the text files

            StreamReader afstanden = new StreamReader("AfstandenMatrix.txt");
            string line = afstanden.ReadLine();

            // fill distance/duration matrix
            while ((line = afstanden.ReadLine()) != null) {

                int i = 0, j = 0, dist = 0, time = 0;
                int index = 0, start = 0;

                for (int k = 0; k < line.Length; k++)
                {
                    if (line[k] == ';')
                    {
                        int num = ParseInt(line, start, k - start);
                        if (index == 0) i = num;
                        else if (index == 1) j = num;
                        else if (index == 2) dist = num;
                        index++;
                        start = k + 1;
                    }
                }
                time = ParseInt(line, start, line.Length - start);

                afstandenMatrix[i, j, 0] = dist;
                afstandenMatrix[i, j, 1] = time;
            }
            afstanden.Close();


            // run

            float penalty = 640760.4f;

            //SimulatedAnnealing sa = new SimulatedAnnealing(afstandenMatrix,
            //    orderList, penalty, 1, 0.985f, 1000, 5000000);
            //double score = sa.GetScore();
            //sa.OutputSolution();
            //Console.WriteLine($"{score}");
            //return;


            // optimize parameters
            float[] Tlist = new float[3] { 46, 48, 50 };
            float[] aList = new float[3] { 0.985f, 0.90f, 0.995f};
            int[] qList = new int[3] { 100000, 200000, 1000000};
            int[] totalList = new int[3] { 80000000, 100000000, 120000000};

            int totalParameterCombinations = Tlist.Length * aList.Length * qList.Length * totalList.Length;


            float T = 0;
            float a = 0;
            int q = 0;
            int total = 0;

            float aT = 0;
            float aa = 0;
            int aq = 0;
            int atotal = 0;

            int counter = 1;
            double best = 99999;
            double bestAverage = 99999;

            for (int indexTotal = 0; indexTotal < totalList.Length; indexTotal++)
            {
                Console.WriteLine($"Iterations: {totalList[indexTotal]}");


                for (int indexA = 0; indexA < aList.Length; indexA++)
                {
                    for (int indexQ = 0; indexQ < qList.Length; indexQ++)
                    {
                        for (int indexT = 0; indexT < Tlist.Length; indexT++)
                        {
                            List<CollectionStop> ls = CreateObjectList();

                            Console.WriteLine($"{counter}/{totalParameterCombinations}; 1/3; ({indexTotal+1}/{totalList.Length})");
                            SimulatedAnnealing s1 = new SimulatedAnnealing(afstandenMatrix,
                                ls, penalty, Tlist[indexT], aList[indexA], qList[indexQ], totalList[indexTotal]);
                            double score1 = s1.GetScore();



                            if (score1 < best)
                            {
                                best = score1;
                                s1.OutputSolution();

                                T = Tlist[indexT];
                                a = aList[indexA];
                                q = qList[indexQ];
                                total = totalList[indexTotal];
                            }

                            Console.WriteLine($"{counter}/{totalParameterCombinations}; 2/3; ({indexTotal + 1}/{totalList.Length})");


                            ls = CreateObjectList();

                            SimulatedAnnealing s2 = new SimulatedAnnealing(afstandenMatrix,
                                ls, penalty, Tlist[indexT], aList[indexA], qList[indexQ], totalList[indexTotal]);
                            double score2 = s2.GetScore();


                            if (score2 < best)
                            {
                                best = score2;
                                s2.OutputSolution();

                                T = Tlist[indexT];
                                a = aList[indexA];
                                q = qList[indexQ];
                                total = totalList[indexTotal];
                            }

                            Console.WriteLine($"{counter}/{totalParameterCombinations}: 3/3; ({indexTotal + 1}/{totalList.Length})");


                            ls = CreateObjectList();
                            SimulatedAnnealing s3 = new SimulatedAnnealing(afstandenMatrix,
                                ls, penalty, Tlist[indexT], aList[indexA], qList[indexQ], totalList[indexTotal]);
                            double score3 = s3.GetScore();


                            if (score3 < best)
                            {
                                best = score3;
                                s3.OutputSolution();

                                T = Tlist[indexT];
                                a = aList[indexA];
                                q = qList[indexQ];
                                total = totalList[indexTotal];
                            }

                            ls = CreateObjectList();
                            SimulatedAnnealing s4 = new SimulatedAnnealing(afstandenMatrix,
                                ls, penalty, Tlist[indexT], aList[indexA], qList[indexQ], totalList[indexTotal]);
                            double score4 = s4.GetScore();


                            if (score4 < best)
                            {
                                best = score4;
                                s4.OutputSolution();

                                T = Tlist[indexT];
                                a = aList[indexA];
                                q = qList[indexQ];
                                total = totalList[indexTotal];
                            }

                            ls = CreateObjectList();
                            SimulatedAnnealing s5 = new SimulatedAnnealing(afstandenMatrix,
                                ls, penalty, Tlist[indexT], aList[indexA], qList[indexQ], totalList[indexTotal]);
                            double score5 = s4.GetScore();


                            if (score5 < best)
                            {
                                best = score5;
                                s5.OutputSolution();

                                T = Tlist[indexT];
                                a = aList[indexA];
                                q = qList[indexQ];
                                total = totalList[indexTotal];
                            }


                            if ((score1 + score2 + score3 + score4 + score5) / 5 < bestAverage)
                            {
                                bestAverage = (score1 + score2 + score3 + score4 + score5) / 5;
                                aT = Tlist[indexT];
                                aa = aList[indexA];
                                aq = qList[indexQ];
                                atotal = totalList[indexTotal];

                            }


                            counter++;

                        }
                    }
                }
            }

            Console.WriteLine($"Best Settings on Average: {bestAverage}");
            Console.WriteLine($"starting T value: {aT}");
            Console.WriteLine($"alpha value: {aa}");
            Console.WriteLine($"iteration before alpha: {aq}");
            Console.WriteLine($"iteration total: {atotal}\n");

            Console.WriteLine($"Settings for best route: {best}");
            Console.WriteLine($"starting T value: {T}");
            Console.WriteLine($"alpha value: {a}");
            Console.WriteLine($"iteration before alpha: {q}");
            Console.WriteLine($"iteration total: {total}");


        }

        static int ParseInt(string str, int start, int length)
        {
            int result = 0;
            for (int i = 0; i < length; i++)
            {
                result = result * 10 + (str[start + i] - '0');
            }
            return result;
        }


        static List<CollectionStop> CreateObjectList()
        {


            StreamReader orders = new StreamReader("Orderbestand.txt");
            string line = orders.ReadLine();
            List<CollectionStop> orderList = new List<CollectionStop>();


            float penalty = 0; //Calculate penalty ahead of time




            // create order objects for each order
            while ((line = orders.ReadLine()) != null)
            {

                string[] results = line.Split(';');
                int orderId = int.Parse(results[0]);
                string place = results[1];
                string freq = results[2].Substring(0, 1);
                int frequency = int.Parse(freq);
                int containerCount = int.Parse(results[3]);
                int containerVolume = int.Parse(results[4]);
                float loadingTime = float.Parse(results[5]); //in minutes
                penalty += (loadingTime * 3 * 60 * frequency); //accumulate penalty
                int matrixId = int.Parse(results[6]);
                int XCoordinate = int.Parse(results[7]);
                int YCoordinate = int.Parse(results[8]);

                if (frequency > 1)
                { //if multiple stops required
                    CollectionStop[] stops = new CollectionStop[frequency];
                    for (int i = 0; i < frequency; i++) //create that many stops
                    {

                        CollectionStop s = new CollectionStop(matrixId, orderId, place, frequency, containerCount,
                                             containerVolume, (loadingTime * 60), // *60 to convert to seconds
                                             XCoordinate, YCoordinate);
                        stops[i] = s;
                        orderList.Add(s);
                    }
                    for (int i = 0; i < frequency; i++) //refer them all to one another
                    {
                        CollectionStop current = stops[i];
                        current.siblings = new CollectionStop[frequency - 1];
                        int k = 0;

                        for (int j = 0; j < frequency; j++)
                        {
                            if (i != j)
                            {
                                current.siblings[k] = stops[j];
                                k++;
                            }
                        }
                    }
                }
                else //if a single stop is required simply add that one
                {
                    CollectionStop stop = new CollectionStop(matrixId, orderId, place, frequency, containerCount,
                                         containerVolume, loadingTime * 60,
                                         XCoordinate, YCoordinate);
                    orderList.Add(stop);

                }
            }



            orders.Close();

            return orderList;
        }


    }
}