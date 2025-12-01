using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Order
    {
        public int orderId {  get; set; }
        public string place;
        public int frequency;
        public int containerCount;
        public int containerVolume;
        public float loadingTime; //in minutes
        public int matrixId;
        public int XCoordinate;
        public int YCoordinate;

        public Order(string line) {
            string[] results = line.Split(';');
            this.orderId = int.Parse(results[0]);
            this.place = results[1];
            string freq = results[2].Substring(0, 1);
            this.frequency = int.Parse(freq);
            this.containerCount = int.Parse(results[3]);
            this.containerVolume = int.Parse(results[4]);
            this.loadingTime = float.Parse(results[5]);
            this.matrixId = int.Parse(results[6]);
            this.XCoordinate = int.Parse(results[7]);
            this.YCoordinate = int.Parse(results[8]);
        }
    }
}
