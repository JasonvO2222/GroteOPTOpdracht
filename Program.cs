
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
            List<CollectionStop> orderList = new List<CollectionStop>();

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

            StreamReader orders = new StreamReader("Orderbestand.txt");
            line = orders.ReadLine();


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

                if (frequency > 1) { //if multiple stops required
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
                        current.siblings = new CollectionStop[frequency-1];
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

            // Pass data to SimulatedAnnealing class
            new SimulatedAnnealing(afstandenMatrix,  orderList, penalty, 1f, 0.98f, 100000, 5000000);

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

    }
}