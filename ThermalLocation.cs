using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondorAutoPilot
{

    class ThermalLocation
    {
        public FlightPathPoint thermalPoint { get; set; }
        public FlightPathPoint thermalTargetPoint { get; set; }
        public bool bInThermal { get; set; }
        public bool bNormalThermal { get; set; }
        public bool bWeakThermal { get; set; }
        public bool bStrongThermal { get; set; }
        public bool bVeryStrongThermal { get; set; }
        public bool bNarrowThermal { get; set; }
        public double dThermalRadius { get; set; }

        public double thermalTimeStart { get; set; }
        public double dRangeRequired { get; set; }

        private bool stopThermal { get; set; }

        bool bHighWindCondition = false;
        bool bHighWindConditionStarted = false;

        public float smoothedVario = 0;
        private float smoothedVarioLast = 0;

        public double varioMax { get; set; }

        public float dRadius { get; set; }

        public int iEscapeThermal = 0;

        public float dEscapeRangeWeak = 0;
        public float dEscapeRangeNormal = 0;
        public float dEscapeRangeStrong = 0;
        public float dEscapeRangeVeryStrong = 0;

        private int iEight = 0;
        public bool bEight { get; set; }
        private double dCurrentMaxVario = 0;

        public ThermalLocation()
        {
            thermalPoint = new FlightPathPoint();
            thermalTargetPoint = new FlightPathPoint();
            stopThermal = false;
        }

        public void StopThermal()
        {
            stopThermal = true;
        }

        public bool Calc(FlightPath flightPath, TerrainMap terrainMap, FlightPlanData flightPlanData, Dijkstra dijkstra)
        {
            varioMax = 0;
            float dWeight = 1;
            float dWeightTotal = 0;
            int nSamples = Math.Min(flightPath.path.Length, 120);

            bool bCurrentlyInThermal = bInThermal;
            int iStart = 0;
            bInThermal = false;
            bNormalThermal = false;
            bWeakThermal = false;
            bStrongThermal = false;
            bVeryStrongThermal = false;

            if (stopThermal)
            {
                stopThermal = false;
                bCurrentlyInThermal = false;
                for (int i = 1; i < nSamples; i++)
                {
                    flightPath.path[i].vario = 0;
                }
                return true;
            }

            dRadius = 0;

            terrainMap.GetHeight(flightPath.path[0].x, flightPath.path[0].y, out float localHeight);

            float dSaferTargetHeight = 0;

            if (terrainMap.myDistanceForFinalApproach - 10000 < flightPlanData.RemainingDistance
                &&
                terrainMap.myDistanceForFinalApproach + 35000 > flightPlanData.RemainingDistance
                )
            {
                dSaferTargetHeight = 6000 + Convert.ToSingle(
                    Math.Tan(2.4) * (10000 + flightPlanData.RemainingDistance - terrainMap.myDistanceForFinalApproach));
            }

            int iCountNormalThermal = 0;
            int iCountWeakThermal = 0;
            int iCountStrongThermal = 0;
            int iCountVeryStrongThermal = 0;

            double dExtraThermalHeight = Utils.interpolate(flightPlanData.RemainingIntegrationLiftPotential, 350, 1000, 450, 0);

            if (flightPath.path[0].vario > 4)
                smoothedVario = 0.75f * smoothedVario + 0.25f * flightPath.path[0].vario;
            else
                smoothedVario = 0.9f * smoothedVario + 0.1f * flightPath.path[0].vario;

            if (smoothedVario > 5.5 && flightPath.path[0].z < 6000)
            {
                dEscapeRangeWeak -= 2 * flightPath.path[0].speed;
                dEscapeRangeNormal -= 2 * flightPath.path[0].speed;
                dEscapeRangeStrong -= 2 * flightPath.path[0].speed;
                dEscapeRangeVeryStrong -= 2 * flightPath.path[0].speed;
            }

            Vector3d vecPlane = new Vector3d(flightPath.path[0].vec.x, flightPath.path[0].vec.y, 0);
            vecPlane.normalize();

            Vector3d vecWind = new Vector3d(flightPath.path[0].windx, flightPath.path[0].windy, 0);
            float fWind = vecWind.length();
            vecWind.normalize();


            bHighWindConditionStarted = false;
            bool bHighWindConditionPre = bHighWindCondition;
            bHighWindCondition = false;


            for (int i = 0; i < nSamples-1; i++)
            {
                flightPath.path[i].vario *= 0.995f;
                if (flightPath.path[i].getWind().length() > 12.5)
                {
                    if (!bHighWindConditionPre)
                        bHighWindConditionStarted = true;
                    bHighWindCondition = true;
                    break;
                }
            }

            bool bPlaneDirectionOK2LeaveThermal;
            bPlaneDirectionOK2LeaveThermal = !bHighWindCondition || vecPlane.dotProduct(vecWind) < -0.3;

            for (int i = 0; i < nSamples - 1; i++)
            {
                //if (
                //    !bInteresting &&
                //    flightPath.path[0].vario < 0 &&
                //    flightPath.path[i].vario < 0
                //    )
                //{
                //    iCountWeakThermal--;
                //    iCountNormalThermal--;
                //    iCountStrongThermal--;
                //    iCountVeryStrongThermal--;
                //}

                if (flightPath.path[i].vario > 1.8)
                {
                    iCountWeakThermal++;
                }
                if (flightPath.path[i].vario > 2.3)
                {
                    iCountNormalThermal++;
                }
                if (flightPath.path[i].vario > 3.9)
                {
                    iCountStrongThermal++;
                }
                if (flightPath.path[i].vario > 5.0)
                {
                    iCountVeryStrongThermal++;
                }
                varioMax = Math.Max(varioMax, flightPath.path[i].vario);
            }

            dEscapeRangeWeak = Math.Max(0, dEscapeRangeWeak - flightPath.path[0].speed);
            dEscapeRangeNormal = Math.Max(0, dEscapeRangeNormal - flightPath.path[0].speed);
            dEscapeRangeStrong = Math.Max(0, dEscapeRangeStrong - flightPath.path[0].speed);
            dEscapeRangeVeryStrong = Math.Max(0, dEscapeRangeVeryStrong - flightPath.path[0].speed);


            bWeakThermal = iCountWeakThermal > 3;
            bNormalThermal = iCountNormalThermal > 3;
            bStrongThermal = iCountStrongThermal > 3;
            bVeryStrongThermal = iCountVeryStrongThermal > 3;

            for (int i = 1; i < nSamples - 1; i++)
            {
                flightPath.path[i].vario = 0.996f * flightPath.path[i].vario;
            }

            float fReturnThermalDist = 0;
            if (bWeakThermal)
            {
                fReturnThermalDist = thermalPoint.distanceTo(flightPath.path[0])/2;
            }


            if (bWeakThermal)
            {
                if (//!bPlaneDirectionOK2LeaveThermal || 
                    (dijkstra.myFlightRange < 12000 - fReturnThermalDist - dEscapeRangeWeak)
                    )
                {
                    bInThermal = true;
                    iEscapeThermal = 0;
                    dEscapeRangeWeak = 0;
                }
                else
                {
                    bInThermal = false;
                    iEscapeThermal = 90;
                    if (bCurrentlyInThermal) dEscapeRangeWeak = 4000;
                }
            }


            if (bNormalThermal)
            {
                if (//!bPlaneDirectionOK2LeaveThermal ||
                    dijkstra.myFlightRange < 20000 - fReturnThermalDist - dEscapeRangeNormal ||
                    (terrainMap.IsInFinalApproach && terrainMap.NeedActionToMeetFinalDestination > 0)
                    )
                {
                    bInThermal = true;
                    iEscapeThermal = 0;
                    dEscapeRangeNormal = 0;
                }
                else
                {
                    bInThermal = false;
                    iEscapeThermal = 90;
                    if (bCurrentlyInThermal) dEscapeRangeNormal = 4000;
                }
            }

            if (bStrongThermal)
            {
               if (//!bPlaneDirectionOK2LeaveThermal ||
                    dijkstra.myFlightRange < 33000 - fReturnThermalDist - dEscapeRangeStrong ||
                    (terrainMap.IsInFinalApproach && terrainMap.NeedActionToMeetFinalDestination > 0))
                {
                    bInThermal = true;
                    iEscapeThermal = 0;
                    dEscapeRangeStrong = 0;
                }
                else
                {
                    bInThermal = false;
                    iEscapeThermal = 90;
                    if (bCurrentlyInThermal) dEscapeRangeStrong = 8000;
                }
            }

            dRangeRequired = 50000  + Utils.interpolate(flightPath.path[0].z, 4250, 1, 5500, 0) * Utils.interpolate(varioMax, 5.5f, 0, 9, 100000);


            if (bVeryStrongThermal)
            {
                dRangeRequired = dRangeRequired - fReturnThermalDist - dEscapeRangeVeryStrong;
                    
                //float dTarget = terrainMap.dHeightFor100Km - 1000;
                if (dijkstra.myFlightRange < dRangeRequired)
                {
                    bInThermal = true;
                    iEscapeThermal = 0;
                    dEscapeRangeVeryStrong = 0;
                }
                else
                {
                    bInThermal = false;
                    iEscapeThermal = 90;
                    if (bCurrentlyInThermal) dEscapeRangeVeryStrong = 12000;
                }
            }


            if (bCurrentlyInThermal)
            {
                if (!bInThermal)
                {
                    Vector3d vecFar = new Vector3d(flightPath.path[0].getPosition(), dijkstra.myPtFlightFarTarget.getPosition());
                    vecFar.normalize();


                    if (-vecWind.dotProduct(vecPlane) > 0.5)// || vecPlane.dotProduct(vecFar) > 0.5)
                    {
                        dCurrentMaxVario = 0;
                        double dq1 = -vecWind.dotProduct(vecPlane);
                        double dq2 = vecPlane.dotProduct(vecFar);
                    }
                    else
                    {
                        bInThermal = true;
                        iEscapeThermal = 0;
                        //dEscapeRange = 0;
                    }
                }
                else
                {
                    if (3600 * (flightPath.path[0].timestamp - thermalTimeStart) > 120)
                    {
                        int i = Math.Min(120, flightPath.path.Count() - 1);
                        double dVario120 = (flightPath.path[0].z - flightPath.path[i].z) /
                            (3600 * (flightPath.path[0].timestamp - flightPath.path[i].timestamp));
                        double dCutOff = Utils.interpolate(dijkstra.myFlightRange, 10000, 1, 70000, 3);
                        bInThermal = dCutOff < dVario120;
                        if (!bInThermal)
                        {
                            //dEscapeRange = 2000;
                            for (i = 0; i < flightPath.path.Count(); i++)
                            {
                                flightPath.path[i].vario = 0;
                            }

                        }
                    }
                }
            }
            else // currently not in thermal
            {
                dCurrentMaxVario = Math.Max(dCurrentMaxVario, flightPath.path[0].vario);
                if (bInThermal)
                {
                    thermalTimeStart = flightPath.path[0].timestamp;
                    if (dCurrentMaxVario * 0.3 < flightPath.path[0].vario && dCurrentMaxVario - 4 < flightPath.path[0].vario)
                    {
                        bInThermal = false;// wait to thermal
                    }
                }
            }

            this.thermalPoint.x = 0;
            this.thermalPoint.y = 0;

            for (int i = 0; i < nSamples - 1; i++)
            {
                if (flightPath.path[i].vario > 1)
                {
                    float dLocalWeight = flightPath.path[i].vario - 1;
                    dLocalWeight *= dLocalWeight * dLocalWeight;
                    iStart += 1;

                    float dT = 3600 * Convert.ToSingle(flightPath.path[i].timestamp - flightPath.path[0].timestamp);


                    if (bNarrowThermal)
                    {
                        this.thermalPoint.x += dWeight * (flightPath.path[i].x - 0.3f* flightPath.path[i].windx* dT) * dLocalWeight;
                        this.thermalPoint.y += dWeight * (flightPath.path[i].y - 0.3f * flightPath.path[i].windy * dT) * dLocalWeight;
                    }
                    else
                    {
                        this.thermalPoint.x += dWeight * flightPath.path[i].x * dLocalWeight;
                        this.thermalPoint.y += dWeight * flightPath.path[i].y * dLocalWeight;
                    }
                    dWeightTotal += dWeight * dLocalWeight;
                    dWeight *= 0.98f;
                }
                else if (iStart > 5)
                {
                    dWeight *= 0.98f;
                }
            }




            if (dWeightTotal > 0)
            {
                this.thermalPoint.x /= dWeightTotal;
                this.thermalPoint.y /= dWeightTotal;

                Point3d ptPlane = flightPath.path[0].getPosition();
                Point3d ptThermal = new Point3d(this.thermalPoint.x, this.thermalPoint.y, ptPlane.Z);
                Vector3d vecToThermal = new Vector3d(ptPlane, ptThermal);

                for (int i = 0; i < nSamples-1; i++)
                {
                    double dCheck = (flightPath.path[i].vario - 0.7 * varioMax) * (flightPath.path[i + 1].vario - 0.7 * varioMax);
                    if (dCheck < 0)
                    {
                        dRadius = thermalPoint.distanceTo(flightPath.path[i]);
                        break;
                    }
                }
                if (!bHighWindCondition)
                {
                    thermalTargetPoint.x = this.thermalPoint.x - 3*flightPath.path[0].windx;
                    thermalTargetPoint.y = this.thermalPoint.y - 3*flightPath.path[0].windy;
                }

                if (terrainMap.bRidge) 
                {
                    vecWind.set(terrainMap.vecRidge);
                }

                Point3d[] ptEight = new Point3d[4];
                Vector3d vecWindPerp = new Vector3d(vecWind.y, -vecWind.x, 0);

                ptEight[0] = thermalPoint.getPosition().add(6 * fWind + 50, vecWind).add(20 * fWind + 250, vecWindPerp);
                ptEight[1] = thermalPoint.getPosition().add(-10 * fWind - 50, vecWind).add(32 * fWind + 350, vecWindPerp);
                ptEight[2] = thermalPoint.getPosition().add(6 * fWind + 50, vecWind).add(-20 * fWind - 250, vecWindPerp);
                ptEight[3] = thermalPoint.getPosition().add(-10 * fWind - 50, vecWind).add(-32 * fWind - 350, vecWindPerp);
                bEight = false;
                if (vecToThermal.length() > 0)
                {
                    if (vecToThermal.dotProduct(vecPlane) > 0)
                    {
                        Vector3d vecZ = new Vector3d(0, 0, 1);
                        Vector3d vecSide = vecToThermal.crossProduct(vecZ);
                        vecSide.normalize();
                        if (vecPlane.dotProduct(vecSide) < 0)
                            vecSide.negate();
                        if (vecToThermal.dotProduct(vecPlane) < 0)
                            vecSide.setNull();

                        thermalTargetPoint.x = this.thermalPoint.x + 0.5f * dRadius * vecSide.x;
                        thermalTargetPoint.y = this.thermalPoint.y + 0.5f * dRadius * vecSide.y;
                    }
                    else
                    {
                        thermalTargetPoint.x = this.thermalPoint.x - 3 * flightPath.path[0].windx;
                        thermalTargetPoint.y = this.thermalPoint.y - 3 * flightPath.path[0].windy;
                    }
                }

                if (bHighWindCondition)
                {
                    if (bHighWindConditionStarted)
                    {
                        Vector3d vecPlaneEight = new Vector3d(flightPath.path[0].getPosition(), ptEight[0]);

                        if (vecPlaneEight.dotProduct(vecPlane) > 0)
                            iEight = 0;
                        else
                            iEight = 2;
                    }
                    else if (ptEight[iEight].distanceXYTo(flightPath.path[0].getPosition()) < 400)
                    {
                        //if (enlargeArea == 0 && smoothedVario > smoothedVarioLast)
                        //{
                        //    enlargeArea = 5;
                        //}
                        //else {
                        //    enlargeArea = 0;
                            iEight++;
                            if (iEight == 4)
                                iEight = 0;

                        //}
                    }

                    thermalTargetPoint.x = ptEight[iEight].X;
                    thermalTargetPoint.y = ptEight[iEight].Y;
                    bEight = true;

                    Point3d ptThermalEight = new Point3d(this.thermalTargetPoint.x, this.thermalTargetPoint.y, ptPlane.Z);
                    Vector3d vecToThermalEight = new Vector3d(ptPlane, ptThermalEight);
                    vecToThermalEight.normalize();
                    double dQ = vecToThermalEight.dotProduct(vecPlane);
                    if (dQ < -0.5)
                    {
                        Vector3d vecWindNorm = flightPath.path[0].getWindNorm();
                        float fQ = Convert.ToSingle(Utils.interpolate(dQ, -1, 1000, -0.5, 0));
                        thermalTargetPoint.x -= fQ * vecWindNorm.x;
                        thermalTargetPoint.y -= fQ * vecWindNorm.y;
                    }

                    //}
                }

            }
            else
            {
                thermalPoint.x = flightPath.path[0].x;
                thermalPoint.y = flightPath.path[0].y;
            }


            if (!bInThermal)
            {
                bNarrowThermal = false;
                dThermalRadius = 0;
            }
            else if (!bNarrowThermal)
            {
                dThermalRadius = 0;
                if (flightPath.path[0].vario < 0)
                {
                    bool bHasUpdraft = false;
                    for (int i = 0; i < nSamples - 1; i++)
                    {
                        if (flightPath.path[i].vario > 0)
                        {
                            bHasUpdraft = true;
                            dThermalRadius = Math.Max(dThermalRadius,
                                flightPath.path[i].getPosition().distanceXYTo
                                (new Point3d(this.thermalPoint.x, this.thermalPoint.y, flightPath.path[i].z))
                                );
                        }
                        else if (bHasUpdraft)
                        {
                            break;
                        }
                    }
                    if (dThermalRadius < 500)
                        bNarrowThermal = true;
                }
            }



            smoothedVarioLast = smoothedVario;

            return bInThermal;
        }
    }
}
