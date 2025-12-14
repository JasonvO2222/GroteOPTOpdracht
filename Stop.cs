using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Stop //Stop superclass that form the nodes in the linkedlist
    {

        public Stop? next;
        public Stop? prev;
        public int matrixId;

        public Stop(int MId) {
            this.matrixId = MId;
        }
    }

    public class OfloadStop : Stop //Stop at ofloading station
    {
        public int volume; // track how much volume is accumulated for before this stop
        public OfloadStop? nextOfloadStop; // track the next ofloadStop in case you want to remove a stop (edit: dont think this is necessary)

        public OfloadStop(int volume) : base (287)
        {
            this.volume = volume;
        }
    }

    public class DayStop : Stop // divider node for when day is finished
    {
        public string day;
        public float dayTime; // track how much time is spent in this day driving and loading/ofloading

        public DayStop(string day, int dagTijd) : base (287)
        {
            this.day = day;
            this.dayTime = dagTijd;
        }
    }

    public class CollectionStop : Stop // Companay stop where the trucks pick up trash
    {

        public CollectionStop[]? siblings; // if order with freq > 1 track order node copies (siblings)
        public DayStop? dayStop; // track which day the stop is on
        public OfloadStop? ofloadStop; // track at which ofload stop node/moment the trash picked upo will be dumped

        public bool included; // bool to quicly check if node is included in solution
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
            this.loadingTime = loadTime;
            this.included = false;
        }
    }



}
