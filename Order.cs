using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Stop
    {

        public Stop? next;
        public Stop? prev;

        public int orderId {  get; set; }
        public string place;
        public int frequency;
        public int containerCount;
        public int containerVolume;
        public float loadingTime; //in minutes
        public int matrixId;
        public int XCoordinate;
        public int YCoordinate;

        public Stop(int id, string plce, int freq, int contCount, int contVol, float loadTime, int MId, int XCoord, int YCoord) {
            this.orderId = id;
            this.place = plce;
            this.frequency = freq;
            this.containerCount = contCount;
            this.containerVolume = contVol;
            this.loadingTime = loadTime;
            this.matrixId = MId;
            this.XCoordinate = XCoord;
            this.YCoordinate = YCoord;
        }
    }
}
