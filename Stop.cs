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
        public Stop[]? siblings;

        public int orderId {  get; set; }
        public string place;
        public int frequency;
        public int containerCount;
        public int containerVolume;
        public float loadingTime; //in minutes
        public int matrixId;
        public int XCoordinate;
        public int YCoordinate;
        public int dag; // Dagen 0-4 corresponderen respectievelijk met maandag-vrijdag (-1 is dagwissel, -2 betekent dat de stop niet in de route zit)

        public Stop(int id, string plce, int freq, int contCount, int contVol, float loadTime, int MId, int XCoord, int YCoord, int dag = -2) {
            this.orderId = id;
            this.place = plce;
            this.frequency = freq;
            this.containerCount = contCount;
            this.containerVolume = contVol;
            this.loadingTime = loadTime;
            this.matrixId = MId;
            this.XCoordinate = XCoord;
            this.YCoordinate = YCoord;
            this.dag = dag;
            if (freq > 1)
                this.siblings = new Stop[freq-1];
        }
    }
}
