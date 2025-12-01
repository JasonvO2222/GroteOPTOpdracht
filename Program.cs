
using System.Collections.Generic;

namespace GroteOPTOpdracht
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //initialize datastructures
            int[,,] afstandenMatrix = new int[1099, 1099, 2];
            List<Stop> orderList = new List<Stop>();

            // parse the text files
            StreamReader afstanden = new StreamReader("AfstandenMatrix.txt");
            string line = afstanden.ReadLine();
            line = afstanden.ReadLine();

            // fill distance/duration matrix
            while (line != null) {
                string[] splt = line.Split(';');
                int i = int.Parse(splt[0]);
                int j = int.Parse(splt[1]);
                afstandenMatrix[i, j, 0] = int.Parse(splt[2]);
                afstandenMatrix[i, j, 1] = int.Parse(splt[3]);
                line = afstanden.ReadLine();
            }

            StreamReader orders = new StreamReader("Orderbestand.txt");
            line = orders.ReadLine();
            line = orders.ReadLine();

            // create order objects for each order
            while (line != null) {

                string[] results = line.Split(';');
                int orderId = int.Parse(results[0]);
                string place = results[1];
                string freq = results[2].Substring(0, 1);
                int frequency = int.Parse(freq);
                int containerCount = int.Parse(results[3]);
                int containerVolume = int.Parse(results[4]);
                float loadingTime = float.Parse(results[5]);
                int matrixId = int.Parse(results[6]);
                int XCoordinate = int.Parse(results[7]);
                int YCoordinate = int.Parse(results[8]);
                Stop stop = new Stop(orderId, place, frequency, containerCount, 
                                     containerVolume, loadingTime, matrixId, 
                                     XCoordinate, YCoordinate);
                orderList.Add(stop);
            }

            // Pass data to SimulatedAnnealing class
            new SimulatedAnnealing(afstandenMatrix,  orderList);

        }

    }
}