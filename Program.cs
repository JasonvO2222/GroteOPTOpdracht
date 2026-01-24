
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;

namespace GroteOPTOpdracht
{


    public struct Parameters
    {
        public int T;
        public int T_min;
        public float T_factor;
        public long iterations;
        public long iterationsTConstant;
        public int[] weights;
        public int[] shiftWeights;
        public float maxVolumePenalty;
        public float maxTimePenalty;
        public float multiplier;

        public Parameters(int T, int T_min, float T_factor, long iterations, long iterationsTConstant, int[] weights, int[] shiftWeights, float volumePenalty, float timePenalty, float multiplier)
        {
            this.T = T;
            this.T_min = T_min;
            this.T_factor = T_factor;
            this.iterations = iterations;
            this.iterationsTConstant = iterationsTConstant;
            this.weights = weights;
            this.shiftWeights = shiftWeights;
            this.maxVolumePenalty = volumePenalty;
            this.maxTimePenalty = timePenalty;
            this.multiplier = multiplier;
        } 
    }


    public class Program
    {

        private static readonly Random rnd = new Random();
        public static float penalty = 640760.4f;

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
            bool b;
            string s;
            double score = 1000000;
            double scoreCheck;
            SimulatedAnnealing sa;
            Parameters p = new Parameters(770, 1, 0.915f, 50000000, 1000000, createWeightedList(15, 14, 11, 28), createShiftWeights(19, 13, 3), 25, 15, 2.5f);
            List<CollectionStop> ls = CreateObjectList();
            sa = new SimulatedAnnealing(afstandenMatrix, ls, penalty, p);
            scoreCheck = sa.GetScore();
            (double ti, double pen) = sa.GetScoreDetailed();
            b = sa.Check();
            s = (b) ? "CORRECT" : "invalid";
            if (b)
            {
                score = scoreCheck;
                sa.OutputSolution();
            }
            Console.WriteLine($"Found {s} solution: {scoreCheck} / {scoreCheck / 60}");
            Console.WriteLine($"time: {ti}");
            Console.WriteLine($"penalty: {pen}");
            DisplayParameters(p);


            // run paramatertuning
            int minutes = 300;
            TimeSpan timeLimit = TimeSpan.FromMinutes(minutes);

            int[] Tl = createRange(450, 900, 6);
            int[] T_minl = createRange(1, 21, 6);
            float[] t_factorl = createRange(0.75f, 0.95f, 12);
            long[] iterationsl = new long[8] { 25000000, 50000000, 100000000, 500000000, 500000000, 750000000, 1000000000, 2500000000};
            long[] iterationsTConstantl = new long[8] { 10000, 100000, 1000000, 2500000, 25000000, 50000000, 75000000, 500000000 };
            int[] weightadd = createRange(6, 9, 4);
            int[] weightremove = createRange(5, 7, 3);
            int[] weightswap = createRange(3, 5, 3);
            int[] weightshift = createRange(12, 17, 6);
            int[] weightshift0 = createRange(24, 28, 5);
            int[] weightshift1 = createRange(8, 12, 3);
            int[] weightshift2 = createRange(1, 2, 2);
            float[] volumePenaltyl = createRange(25f, 50f, 11);
            float[] timePenaltyl = createRange(4f, 8f, 11);
            float[] multiplierl = createRange(1.005f, 20f, 11);
            long it;
            long itc;
            int t;
            int tm;
            float tf;
            int[] w;
            int[] sw;



            Parameters bestP = new Parameters();


            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeLimit)
            {
                itc = iterationsTConstantl[rnd.Next(iterationsTConstantl.Length)];
                it = itc + iterationsl[rnd.Next(iterationsl.Length)];
                t = Tl[rnd.Next(Tl.Length)];
                tm = T_minl[rnd.Next(T_minl.Length)];
                tf = t_factorl[rnd.Next(t_factorl.Length)];
                if (t <= tm + 10) continue;
                w = createWeightedList(weightswap[rnd.Next(weightswap.Length)], weightadd[rnd.Next(weightadd.Length)], weightremove[rnd.Next(weightremove.Length)], weightshift[rnd.Next(weightshift.Length)]);
                sw = createShiftWeights(weightshift0[rnd.Next(weightshift0.Length)], weightshift1[rnd.Next(weightshift1.Length)], weightshift2[rnd.Next(weightshift2.Length)]);

                p = new Parameters(t, tm, tf, it, itc, w, sw, volumePenaltyl[rnd.Next(volumePenaltyl.Length)], timePenaltyl[rnd.Next(timePenaltyl.Length)], multiplierl[rnd.Next(multiplierl.Length)]);

                ls = CreateObjectList();
                sa = new SimulatedAnnealing(afstandenMatrix, ls, penalty, p);

                scoreCheck = sa.GetScore();
                if (scoreCheck < score)
                {
                    b = sa.Check();
                    if (b)
                    {
                        score = scoreCheck;
                        bestP = p;
                        sa.OutputSolution();
                    }
                    s = (b) ? "CORRECT" : "invalid";
                    Console.WriteLine($"Found new {s} best: {scoreCheck} / {scoreCheck / 60}");
                    (ti, pen) = sa.GetScoreDetailed();
                    Console.WriteLine($"time: {ti}");
                    Console.WriteLine($"penalty: {pen}");
                    DisplayParameters(p);
                    Console.WriteLine("\n");

                }
                else if (scoreCheck < score + 6000)
                {
                    b = sa.Check();
                    s = (b) ? "CORRECT" : "invalid";
                    Console.WriteLine($"{s} result within 100 points of best: ");
                    DisplayParameters(p);
                    Console.WriteLine("\n");
                }

            }

            stopwatch.Stop();
            Console.WriteLine($"finished with score: {score} / {(score/60)}");
            Console.WriteLine("best parameter combinations:");
            DisplayParameters(bestP);
            return;

        }

        static int[] createWeightedList(int swap, int add, int remove, int shift)
        {
            int[] list = new int[(swap+add+remove+shift)];
            for (int i = 0; i < list.Length; i++)
            {
                if (i < swap)
                {
                    list[i] = 0;
                }
                else if (i < (add + swap))
                {
                    list[i] = 1;
                }
                else if (i < (remove + add + swap))
                {
                    list[i] = 2;
                }
                else if (i < (shift+ remove + add + swap))
                {
                    list[i] = 3;
                }
            }

            return list;
        }

        static int[] createShiftWeights(int truck, int day, int weak)
        {
            int[] list = new int[(truck + day + weak)];
            for (int i = 0; i < list.Length; i++)
            {
                if (i < truck)
                {
                    list[i] = 0;
                }
                else if (i < (day + truck))
                {
                    list[i] = 1;
                }
                else if (i < (weak + truck + day))
                {
                    list[i] = 2;
                }
            }

            return list;
        }


        static long[] createRangeLong(long start, long end, int freq)
        {
            long diff = end - start;
            long[] range = new long[freq];
            long steps = diff / (freq-1);
            for (int i = 0; i < freq; i++)
            {
                range[i] = start;
                start += steps;
            }
            return range;
        }

        static int[] createRange(int start, int end, int freq)
        {
            int diff = end - start;
            int[] range = new int[freq];
            int steps = diff / (freq - 1);
            for (int i = 0; i < freq; i++)
            {
                range[i] = start;
                start += steps;
            }
            return range;
        }

        static float[] createRange(float start, float end, int freq)
        {
            float diff = end - start;
            float[] range = new float[freq];
            float steps = diff / (freq - 1);
            for (int i = 0; i < freq; i++)
            {
                range[i] = start;
                start += steps;
            }
            return range;
        }


        static void DisplayParameters(Parameters p)
        {

            Console.WriteLine($"T: {p.T}");
            Console.WriteLine($"T_min: {p.T_min}");
            Console.WriteLine($"T_factor: {p.T_factor}");
            Console.WriteLine($"iterations: {p.iterations}");
            Console.WriteLine($"iterationsTConstant: {p.iterationsTConstant}");

            int ws = 0;
            int wa = 0;
            int wr = 0;
            int wsh = 0;

            foreach (int i in p.weights)
            {
                if (i == 0) ws++;
                if (i == 1) wa++;
                if (i == 2) wr++;
                if (i == 3) wsh++;
            }

            int s0 = 0;
            int s1 = 0;
            int s2 = 0;

            foreach (int i in p.shiftWeights)
            {
                if (i == 0) s0++;
                if (i == 1) s1++;
                if (i == 2) s2++;
            }


            Console.WriteLine($"weights: {ws} (swap), {wa} (add), {wr} (remove), {wsh} (shift)");
            Console.WriteLine($"weights: {s0} (truck), {s1} (day), {s2} (week)");
            Console.WriteLine($"volumePenalty: {p.maxVolumePenalty}");
            Console.WriteLine($"timePenalty: {p.maxTimePenalty}");
            Console.WriteLine($"multiplier: {p.multiplier}");
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