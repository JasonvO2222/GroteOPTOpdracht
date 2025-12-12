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
        public int matrixId;

        public Stop(int MId) {
            this.matrixId = MId;
        }
    }

    public class OfloadStop : Stop
    {
        public int volume;
        public OfloadStop? nextOfloadStop;

        public OfloadStop(int volume) : base (287)
        {
            this.volume = volume;
        }
    }

    public class DayStop : Stop
    {
        public string day;
        public float dayTime;

        public DayStop(string day, int dagTijd) : base (-1)
        {
            this.day = day;
            this.dayTime = dagTijd;
        }
    }

    public class CollectionStop : Stop
    {

        public CollectionStop[]? siblings;
        public DayStop? dayStop;
        public OfloadStop? ofloadStop;

        public bool included;
        public int orderId { get; set; }
        public string place;
        public int frequency;
        public int containerCount;
        public int containerVolume;
        public float loadingTime;
        public int XCoordinate;
        public int YCoordinate;


        public CollectionStop(int MId, int id, string plce, int freq, int contCount, int contVol, float loadTime, int XCoord, int YCoord) : base(MId)
        {
            this.orderId = id;
            this.place = plce;
            this.frequency = freq;
            this.containerCount = contCount;
            this.containerVolume = contVol;
            this.loadingTime = loadTime * 60;
            this.included = false;
        }
    }



}
