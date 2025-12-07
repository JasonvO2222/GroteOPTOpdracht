using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace GroteOPTOpdracht
{
    public class Oplossing
    {
        public List<Stop> stops;  //stops we visit
        public List<Stop> ignore; //stops we dont visit
        public int tijd;
        private static readonly Random rndIndex = new Random();

        public Oplossing(List<Stop> orderList, int[,,] afstandenMatrix)
        {
            stops = new List<Stop>();
            ignore = new List<Stop>();
            tijd = 0;

            // Assign constants
            float bezorgtijd = 720; // tijd die de auto kan bezorgen op een dag
            int vuilnisRuimte = 100000; // ruimte in de auto voor comprimeren
            int stortTijd = 30;
            Stop stort = new Stop(0, "MAARHEEZE STORT", 0, 0, 0, 30, 287, 56343016, 513026712);
            Stop dagWissel = new Stop(-1, "MAARHEEZE DAGWISSEL", 0, 0, 0, 0, 287, 56343016, 513026712); // element gebruikt om een volgende dag aan te geven in de route

            // Copy orderlist
            List<Stop> stopsOver = new List<Stop>(orderList.Count);
            foreach (Stop stop in orderList)
            {
                stopsOver.Add(new Stop(stop.orderId, stop.place, stop.frequency, stop.containerCount, stop.containerVolume, stop.loadingTime, stop.matrixId, stop.XCoordinate, stop.YCoordinate));
            }

            WeekRoute(stopsOver, afstandenMatrix, bezorgtijd, vuilnisRuimte, stortTijd, stort, dagWissel);

            // calculate total time and add prev/references to stops in stops(list)
            for (int i = 1; i < (stops.Count - 1); i++) {
                tijd += afstandenMatrix[stops[i - 1].matrixId, stops[i].matrixId, 1];
            }
            tijd += afstandenMatrix[stops[stops.Count - 2].matrixId, stops[stops.Count - 1].matrixId, 1];

        }

        public int? pickRandomStop()
        {
            int? stopsIndex;
            if (!stops.Any()) stopsIndex = null;
            else stopsIndex = rndIndex.Next(stops.Count);

            return stopsIndex;
        }


        public int? pickRandomIgnoredStop()
        {
            int? ignoreIndex;
            if (!ignore.Any()) ignoreIndex = null;
            else ignoreIndex = rndIndex.Next(ignore.Count);

            return ignoreIndex;
        }

        public void RemoveStop(Stop stop, int index)
        {
            // switch object with last object in stops(list) and add it to ignore(list) and remove it from stops(list)
            int lastIndex = stops.Count - 1;
            (stops[lastIndex], stops[index]) = (stops[index], stops[lastIndex]);
            ignore.Add(stops[lastIndex]);
            stops.RemoveAt(lastIndex);

            // add null handling --!!
            (stop.prev.next, stop.next.prev) = (stop.next.prev, stop.prev.next);

        }

        Stop DagRoute(List<Stop> stopsOver, int[,,] afstandenMatrix, float bezorgtijd, int vuilnisRuimte, int stortTijd, Stop stort, Stop laatsteStopVorigeDag)
        {
            float tijdOver = bezorgtijd;
            int ruimteOver = vuilnisRuimte;
            Stop vorigeStop = laatsteStopVorigeDag;

            while (true)
            {
                // trek randomStop uit orderlist
                int randomIndex = rndIndex.Next(stopsOver.Count);
                Stop randomStop = stopsOver[randomIndex];
                // check of er genoeg tijd is om deze stop te doen
                int reistijdNaarRandomStop = afstandenMatrix[vorigeStop.matrixId, randomStop.matrixId, 1];
                if (tijdOver < reistijdNaarRandomStop + randomStop.loadingTime + stortTijd + afstandenMatrix[randomStop.matrixId, stort.matrixId, 1])
                {
                    // als er geen tijd is om naar de randomStop te gaan, rijden we naar de stort om voor de laatste keer deze dag te storten
                    Stop tijdStort = new Stop(stort.orderId, stort.place, stort.frequency, stort.containerCount, stort.containerVolume, stort.loadingTime, stort.matrixId, stort.XCoordinate, stort.YCoordinate);
                    stops.Add(tijdStort);
                    tijdOver -= afstandenMatrix[vorigeStop.matrixId, stort.matrixId, 1] + stortTijd;
                    ruimteOver = vuilnisRuimte;
                    vorigeStop.next = tijdStort;
                    tijdStort.prev = vorigeStop;
                    return tijdStort; // Er is geen tijd meer dus de dag verandert
                }

                if (ruimteOver < (randomStop.containerCount) * (randomStop.containerVolume))
                {
                    // als er niet genoeg ruimte meer is in de auto rijden we terug naar de stort
                    Stop ruimteStort = new Stop(stort.orderId, stort.place, stort.frequency, stort.containerCount, stort.containerVolume, stort.loadingTime, stort.matrixId, stort.XCoordinate, stort.YCoordinate);
                    stops.Add(ruimteStort);
                    tijdOver -= afstandenMatrix[vorigeStop.matrixId, stort.matrixId, 1] + stortTijd;
                    ruimteOver = vuilnisRuimte;
                    vorigeStop.next = ruimteStort;
                    ruimteStort.prev = vorigeStop;
                    vorigeStop = ruimteStort;

                    continue; // de dag hoeft dan niet perse te veranderen
                }

                // voeg randomStop toe aan route auto
                stops.Add(randomStop);
                vorigeStop.next = randomStop;
                randomStop.prev = vorigeStop;

                tijdOver -= reistijdNaarRandomStop + randomStop.loadingTime;
                ruimteOver -= randomStop.containerCount * randomStop.containerVolume;

                stopsOver[randomIndex] = stopsOver[stops.Count - 1];
                stopsOver.RemoveAt(stops.Count - 1);

                // zet vorige stop voor volgende ronde van loop
                vorigeStop = randomStop;
            }
        }

        void WeekRoute(List<Stop> stopsOver, int[,,] afstandenMatrix, float bezorgtijd, int vuilnisRuimte, int stortTijd, Stop stort, Stop dagWissel)
        {

            // Start op maandag bij de stort
            stops.Add(stort);
            Stop laatsteStopVorigeDag = stort;

            // maak een route voor elke dag
            for (int i = 0; i < 5; i++)
            {
                laatsteStopVorigeDag = DagRoute(stopsOver, afstandenMatrix, bezorgtijd, vuilnisRuimte, stortTijd, stort, laatsteStopVorigeDag);

                Stop nieuweDag = new Stop(dagWissel.orderId, dagWissel.place, dagWissel.frequency, dagWissel.containerCount, dagWissel.containerVolume, dagWissel.loadingTime, dagWissel.matrixId, dagWissel.XCoordinate, dagWissel.YCoordinate);
                stops.Add(nieuweDag);
                laatsteStopVorigeDag.next = nieuweDag;
                nieuweDag.prev = laatsteStopVorigeDag;

                laatsteStopVorigeDag = nieuweDag; // Zet de laatste stop op de dagwissel voor de volgende dag
            }

            ignore = stopsOver; // De stops die niet aan de route zijn toegevoegd worden genegeerd
        }

    }
}
