using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Oplossing
    {
        public List<Stop> stopsAuto1;  //stops we visit with truck 1
        public List<Stop> stopsAuto2;  //stops we visit with truck 2
        public List<Stop> ignore; //stops we dont visit
        public float tijd;
        private static readonly Random rndIndex = new Random();

        public Oplossing(List<Stop> orderList, int[,,] afstandenMatrix)
        {
            stopsAuto1 = new List<Stop>();
            stopsAuto2 = new List<Stop>();
            ignore = new List<Stop>();
            tijd = 0;

            // Assign constants
            float bezorgtijd = 43200; // tijd die de auto kan bezorgen op een dag in seconden
            int vuilnisRuimte = 100000; // ruimte in de auto voor comprimeren
            int stortTijd = 1800;
            Stop stort = new Stop(0, "MAARHEEZE STORT", 0, 0, 0, stortTijd, 287, 56343016, 513026712);
            Stop dagWissel = new Stop(-1, "MAARHEEZE DAGWISSEL", 0, 0, 0, 0, 287, 56343016, 513026712); // element gebruikt om een volgende dag aan te geven in de route

            // Copy orderlist
            List<Stop> stopsOver = OrdersToStops(orderList);

            WeekRoute(stopsAuto1, stopsOver, afstandenMatrix, bezorgtijd, vuilnisRuimte, stortTijd, stort, dagWissel);
            WeekRoute(stopsAuto2, stopsOver, afstandenMatrix, bezorgtijd, vuilnisRuimte, stortTijd, stort, dagWissel);
        }

        public int? pickRandomStop(List<Stop> stopsAuto)
        {
            int? stopsIndex;
            if (!stopsAuto.Any()) stopsIndex = null;
            else stopsIndex = rndIndex.Next(stopsAuto.Count);

            return stopsIndex;
        }


        public int? pickRandomIgnoredStop()
        {
            int? ignoreIndex;
            if (!ignore.Any()) ignoreIndex = null;
            else ignoreIndex = rndIndex.Next(ignore.Count);

            return ignoreIndex;
        }

        public void RemoveStop(List<Stop> stopsAuto, Stop stop, int index)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            int lastIndex = stopsAuto.Count - 1;
            (stopsAuto[lastIndex], stopsAuto[index]) = (stopsAuto[index], stopsAuto[lastIndex]);
            ignore.Add(stopsAuto[lastIndex]);
            stopsAuto.RemoveAt(lastIndex);

            // add null handling --!!
            (stop.prev.next, stop.next.prev) = (stop.next.prev, stop.prev.next);
        }

        Stop DagRoute(List<Stop> stops, List<Stop> stopsOver, int[,,] afstandenMatrix, float bezorgtijd, int vuilnisRuimte, int stortTijd, Stop stort, Stop laatsteStopVorigeDag)
        {
            float tijdOver = bezorgtijd;
            int ruimteOver = vuilnisRuimte;
            Stop vorigeStop = laatsteStopVorigeDag;

            while (stopsOver.Count > 0)
            {
                // trek randomStop uit orderlist
                int randomIndex = rndIndex.Next(stopsOver.Count);
                Stop randomStop = stopsOver[randomIndex];

                // check of er genoeg tijd is om deze stop te doen
                int reistijdNaarRandomStop = afstandenMatrix[vorigeStop.matrixId, randomStop.matrixId, 1];
                int TijdNaarStortvanafRandomStop = stortTijd + afstandenMatrix[randomStop.matrixId, stort.matrixId, 1];
                int tijdNaarStortVorigeStop = afstandenMatrix[vorigeStop.matrixId, stort.matrixId, 1] + stortTijd;

                // als er geen tijd is om naar de randomStop te gaan, rijden we naar de stort om voor de laatste keer deze dag te storten
                if (tijdOver < (float)(reistijdNaarRandomStop + (float)(60 * randomStop.loadingTime) + TijdNaarStortvanafRandomStop))
                {
                    Stop tijdStort = new Stop(stort.orderId, stort.place, stort.frequency, stort.containerCount, stort.containerVolume, stort.loadingTime, stort.matrixId, stort.XCoordinate, stort.YCoordinate);
                    stops.Add(tijdStort);
                    tijdOver -= afstandenMatrix[vorigeStop.matrixId, stort.matrixId, 1] + stortTijd;
                    ruimteOver = vuilnisRuimte;
                    vorigeStop.next = tijdStort;
                    tijdStort.prev = vorigeStop;

                    tijd += tijdNaarStortVorigeStop; // tel de tijd om naar de stort te rijden op bij de route tijd

                    return tijdStort; // Er is geen tijd meer dus de dag verandert
                }

                // als er niet genoeg ruimte meer is in de auto rijden we terug naar de stort
                if (ruimteOver < (randomStop.containerCount) * (randomStop.containerVolume))
                {
                    Stop ruimteStort = new Stop(stort.orderId, stort.place, stort.frequency, stort.containerCount, stort.containerVolume, stort.loadingTime, stort.matrixId, stort.XCoordinate, stort.YCoordinate);
                    stops.Add(ruimteStort);
                    tijdOver -= afstandenMatrix[vorigeStop.matrixId, stort.matrixId, 1] + stortTijd;
                    ruimteOver = vuilnisRuimte;
                    vorigeStop.next = ruimteStort;
                    ruimteStort.prev = vorigeStop;
                    vorigeStop = ruimteStort;

                    tijd += tijdNaarStortVorigeStop; // tel de tijd om naar de stort te rijden op bij de route tijd

                    continue; // de dag hoeft dan niet perse te veranderen
                }

                // voeg randomStop toe aan route auto
                stops.Add(randomStop);
                vorigeStop.next = randomStop;
                randomStop.prev = vorigeStop;

                float tijdGekost = (float)(reistijdNaarRandomStop + (float)(60 * randomStop.loadingTime)); // de tijd die het kost om naar de stop te rijden en te laden
                tijdOver -= tijdGekost;
                ruimteOver -= randomStop.containerCount * randomStop.containerVolume;

                stopsOver[randomIndex] = stopsOver[stopsOver.Count - 1];
                stopsOver.RemoveAt(stopsOver.Count - 1);

                tijd += tijdGekost; // tel de tijd om deze stop te doen op bij de route tijd

                // zet vorige stop voor volgende ronde van loop
                vorigeStop = randomStop;
            }

            return vorigeStop; // If there are no more stops to go to something has gone wrong
        }

        void WeekRoute(List<Stop> stops, List<Stop> stopsOver, int[,,] afstandenMatrix, float bezorgtijd, int vuilnisRuimte, int stortTijd, Stop stort, Stop dagWissel)
        {
            // Start op maandag bij de stort
            stops.Add(stort);
            Stop laatsteStopVorigeDag = stort;

            // maak een route voor elke dag
            for (int i = 0; i < 5; i++)
            {
                laatsteStopVorigeDag = DagRoute(stops, stopsOver, afstandenMatrix, bezorgtijd, vuilnisRuimte, stortTijd, stort, laatsteStopVorigeDag);

                Stop nieuweDag = new Stop(dagWissel.orderId, dagWissel.place, dagWissel.frequency, dagWissel.containerCount, dagWissel.containerVolume, dagWissel.loadingTime, dagWissel.matrixId, dagWissel.XCoordinate, dagWissel.YCoordinate);
                stops.Add(nieuweDag);
                laatsteStopVorigeDag.next = nieuweDag;
                nieuweDag.prev = laatsteStopVorigeDag;

                laatsteStopVorigeDag = nieuweDag; // Zet de laatste stop op de dagwissel voor de volgende dag
            }

            ignore = stopsOver; // De stops die niet aan de route zijn toegevoegd worden genegeerd
        }

        List<Stop> OrdersToStops(List<Stop> orderList)
        {
            List<Stop> stops = new List<Stop>(orderList.Count);
            foreach (Stop stop in orderList)
            {
                if (stop.frequency > 1)
                {
                    List<Stop> temp = new List<Stop>(stop.frequency);
                    // Make as much stops as the frequency demands
                    for (int i = 0; i < stop.frequency; i++)
                    {
                        Stop sibling = new Stop(stop.orderId, stop.place, stop.frequency, stop.containerCount, stop.containerVolume, stop.loadingTime, stop.matrixId, stop.XCoordinate, stop.YCoordinate);
                        stops.Add(sibling);
                        temp.Add(sibling);
                    }

                    // Add the siblings for each stop
                    for (int j = 0; j < temp.Count; j++)
                    {
                        Stop s = temp[j];
                        int indexSiblings = 0;

                        for (int i = 0; i < s.frequency; i++)
                        {
                            if (i != j)
                            {
                                s.siblings[indexSiblings] = temp[i];
                                indexSiblings++;
                            }
                        }
                    }
                }
                else
                    stops.Add(new Stop(stop.orderId, stop.place, stop.frequency, stop.containerCount, stop.containerVolume, stop.loadingTime, stop.matrixId, stop.XCoordinate, stop.YCoordinate));
            }
            return stops;
        }
    }
}
