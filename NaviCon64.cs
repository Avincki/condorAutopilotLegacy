using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{
    // https://en.wikipedia.org/wiki/Universal_Transverse_Mercator_coordinate_system
    public class NaviCon64
    {
        static double a = 6378.137;
        static double f = 1 / 298.257223563;
        double n;
        double nRoot;
        double A;
        double[] alfa = new double[4];
        double[] beta = new double[4];
        double[] delta = new double[4];
        double N0;
        double E0 = 500;
        double k0 = 0.9996;
        double refLon = 0;// to be read from TRN in radial;
        int Zone;
        double rad6, rad183;

        double dRefX, dRefY;
        double dIterX, dIterY;

        public void init(TerrainMap terrainmap)
        {
            n = f / (2 - f);
            double n2 = n * n;
            double n3 = n2 * n;
            double n4 = n3 * n;
            nRoot = 2 * Math.Sqrt(n) / (1 + n);

            A = a / (1 + n) * (1 + n2 / 4 + n4 / 64);
            alfa[0] = n / 2 - 2 * n2 / 3 + 5 * n3 / 16;
            alfa[1] = 13 * n2 / 48 - 3 * n3 / 5;
            alfa[2] = 61 * n3 / 240;
            beta[0] = n / 2 - 2 * n2 / 3 + 37 * n3 / 96;
            beta[1] = n2 / 48 + n3 / 15;
            beta[2] = 17 * n3 / 480;
            delta[0] = 2 * n - 2 * n2 / 3 - 2 * n3;
            delta[1] = 7 * n2 / 3 - 8 * n3 / 5;
            delta[2] = 56 * n3 / 15;

            rad6 = 6 * Math.PI / 180;
            rad183 = 183 * Math.PI / 180;

            Zone = terrainmap.TrnUTMX;
            if (terrainmap.TrnUTMY == 'N')
                N0 = 0;
            else
                N0 = 10000;


            dIterX = dRefX = 0.001 * terrainmap.TrnRefX;
            dIterY = dRefY = 0.001 * terrainmap.TrnRefY;
        }

        double Atanh(double x)
        {
            return 0.5 * Math.Log((1 + x) / (1 - x));
        }

        public void calcUTM(float lat, float lon, out float E, out float N)
        {
            calcUTM(lat, lon, out double dE, out double dN);
            E = Convert.ToSingle(dE);
            N = Convert.ToSingle(dN);
        }
        public void calcUTM(double lat, double lon, out double E, out double N)
        {
            double radLat = lat * Math.PI / 180;
            double radLon = lon * Math.PI / 180;

            double radRefLon = radLon - refLon;

            double t = Math.Sinh(
                    Atanh(Math.Sin(radLat)) -
                    nRoot * Atanh(nRoot * Math.Sin(radLat))
                    );

            double eps = Math.Atan(t / Math.Cos(radRefLon));
            double neta = Atanh(Math.Sin(radRefLon) / Math.Sqrt(1 + t * t));

            E = 0;
            N = 0;
            for (int i = 1; i < 4; i++)
            {
                E += alfa[i - 1] * Math.Cos(2 * i * eps) * Math.Sinh(2 * i * neta);
                N += alfa[i - 1] * Math.Sin(2 * i * eps) * Math.Cosh(2 * i * neta);
            }
            E = E0 + k0 * A * (neta + E);
            N = N0 + k0 * A * (eps + N);

            E = (E - dRefX )* 1000;
            N = (N - dRefY )* 1000;
        }

        public void calcUTMIter(float lat, float lon, out float E, out float N)
        {
            calcUTMIter(lat, lon, out double dE, out double dN);
            E = Convert.ToSingle(dE);
            N = Convert.ToSingle(dN);
        }


        public void calcUTMIter(double targetLat, double targetLon, out double X, out double Y)
        {
            X = dIterX;
            Y = dIterY;
            double dO = 100f;
            double dPrecision = 10;
            double dLat0, dLon0;
            double dLat1, dLon1;
            int iter = 0;
            while (dPrecision > 1f && iter < 20)
            {
                dPrecision = 0;
                iter++;
                double dX = 0;
                double dY = 0;
                //do X
                {
                    calcLatLon(X, Y, out dLat0, out dLon0);
                    calcLatLon(X, Y + dO, out dLat1, out  dLon1);

                    if (dLat1 != dLat0)
                    {
                        double fact = dO * (targetLat - dLat0) / (dLat1 - dLat0);
                        dY = 0.9f * fact;
                        dPrecision = Math.Abs(fact);
                    }
                }
                //do Y
                {
                    calcLatLon(X + dO, Y, out dLat1, out dLon1);

                    if (dLon1 != dLon0)
                    {
                        double fact = dO * (targetLon - dLon0) / (dLon1 - dLon0);
                        dX = 0.9f * fact;
                        dPrecision += Math.Abs(fact);
                    }
                }
                X += dX;
                Y += dY;
            }
            dIterX = X;
            dIterY = Y;

            iter = 0;
        }

        public void calcLatLon(float E, float N, out float lat, out float lon)
        {
            calcLatLon(E, N, out double dlat, out double dlon);
            lat = Convert.ToSingle(dlat);
            lon = Convert.ToSingle(dlon);
        }
        public void calcLatLon(double E, double N, out double lat, out double lon)
        {
            dIterX = E;
            dIterY = N;

            double eps = (dRefY + 0.001 * N - N0) / (k0 * A);
            double neta = (dRefX - 0.001 * E - E0) / (k0 * A);

            double eps0 = 0;
            double neta0 = 0;

            for (int i = 1; i < 4; i++)
            {
                eps0 += beta[i - 1] * Math.Sin(2 * i * eps) * Math.Cosh(2 * i * neta);
                neta0 += beta[i - 1] * Math.Cos(2 * i * eps) * Math.Sinh(2 * i * neta);
            }

            eps0 = eps - eps0;
            neta0 = neta - neta0;

            double Xi = Math.Asin(Math.Sin(eps0) / Math.Cosh(neta0));

            lat = Xi;
            for (int i = 1; i < 4; i++)
            {
                lat += delta[i - 1] * Math.Sin(2 * i * Xi);
            }

            lon = Zone * rad6 - rad183 + Math.Atan(Math.Sinh(neta0)/Math.Cos(eps0));

            lat *= 180 / Math.PI;
            lon *= 180 / Math.PI;
        }
    }
}
