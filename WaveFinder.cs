using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    class WaveFinder
    {
        public float dCorrection { get; set; }

        private int nTime { get; set; }
        private FlightPath myFlightPath;


        public WaveFinder()
        {
            dCorrection = 0;
            nTime = 0;
        }

        public void Init(FlightPath flightPath)
        {
            myFlightPath = flightPath;
            dCorrection = 0;
            nTime = 0;
        }

        public bool Calc(bool bNoWaveDetectionNeeded)
        {
            if (myFlightPath.path.Count() < 20 ||  myFlightPath.path[0].vario > 2 || myFlightPath.path[0].z < 4500 || bNoWaveDetectionNeeded)
            {
                dCorrection = 0;
                nTime = 0;
                return true;
            }
            double dDeltaVario20 = myFlightPath.path[0].vario - myFlightPath.path[20].vario;
            double dDeltaVario5 = myFlightPath.path[0].vario - myFlightPath.path[5].vario;
            int iSide = Math.Sign(dCorrection);
            if (iSide == 0)
                iSide = 1;

            nTime += 1;
            if (nTime < 5)
            {
                     return true;
            }

            nTime = 0;


            if (dDeltaVario20 < 0 && dDeltaVario5 < -0.5) // sinking faster
            {
                dCorrection = -iSide * Convert.ToSingle(Utils.interpolate(myFlightPath.path[0].vario, -5, 10, 1, 0));
            }
            else
            {
                dCorrection = iSide * Convert.ToSingle(Utils.interpolate(myFlightPath.path[0].vario, -5, 5, 1, 5));
            }


            return true;
        }
    }
}
