using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput.Native;
using WindowsInput;
using System.Windows.Forms;
using CondorAutoPilot.YoloViewer;

namespace CondorAutoPilot
{
    class AutoPilotTarget
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

    }

    enum Mode
    {
        Normal,
        InTow,
        InLanding
    }
    class AutoPilot
    {
        public float AirSpeedKmH { get; set; }
        public bool Debug { get; set; }
        private bool InPause { get; set; }
        public bool InReplay { get; set; }

        public Mode ModeStatus { get; set; }
        private bool GearExtracted { get; set; }
        private bool WaterDumping { get; set; }

        public string SpeedSetting { get; set; }

        public float TargetSpeed { get; set; }
        public float TargetHeading { get; set; }
        public float Heading { get; set; }
        public float stickPitch { get; set; }
        public float stickRoll { get; set; }



        private double dBankMax;


        const UInt32 WM_KEYDOWN = 0x0100;
        const UInt32 WM_KEYUP = 0x0101;


        private double dTresholdPitch, dTresholdBank, dTresholdYaw;
        private double dFactorV2Pitch, dFactorSin2Bank;

        public double dPitchTarget { get; set; }
        private double dPitchRateTarget = 0;
        private double dPitchSmoothed = 0;



        private double dSpeedPrev = 0;
        private double dPitchPrev = 0;
        private double dTimePrev = 0;
        private double dVarioPrev = 0;
        private double dBankPrev = 0;
        private double dYawStringPrev = 0;
        private double dTowPitchPrev = 0;
        private double dTowPitchPrev2 = 0;
        private double dTowBankPrev = 0;
        private double dTowBankPrev2 = 0;
        private float dSmoothedVario = 0;
        private int iBrakeDelay = 0;

        private double dReducedSpeedForFinalApproach = 0;

        public int InStateOfAvoidance { get; set; }

        private int IWasInStateOfAvoidance = 0;

        private enum flapStatus {kMinusSpeed, kMinus2, kMinus1, kNeutral, kPlus1, kPlus2};
        private flapStatus myFlapStatus;

        //private CondorUDPData condorDataPrev = new CondorUDPData();

        private AutoPilotTarget myTarget =  new AutoPilotTarget();

        public AutoPilot()
        {
            dBankMax = 65 * Math.PI / 180;
            dPitchTarget = 0;
            InStateOfAvoidance = 0;
            myFlapStatus = flapStatus.kMinus1;
            Debug = false;
            ModeStatus = Mode.Normal;
            GearExtracted = false;
            WaterDumping = false;
            stickPitch = 0;
            stickRoll = 0;
        }

        public bool InLanding()
        {
            return ModeStatus == Mode.InLanding;
        }


    private double SetSpeedTarget (double speedCurrent, double speedTarget, string strMessage)
        {
            if (speedCurrent > speedTarget)
            {
                speedCurrent = speedTarget;
                SpeedSetting = speedCurrent.ToString("F0") + " " + strMessage;
            }
            return speedCurrent;
        }
        public void SetTarget(float X, float Y)
        {
            myTarget.X = X;
            myTarget.Y = Y;
        }

        public void StartDumpingWater(CondorUDPData condorData)
        {
            if (!WaterDumping && condorData.water > 0)
            {
                PushKey(VirtualKeyCode.VK_W);
                WaterDumping = true;
            }
        }
        public void SetTargetAltitude(float Z, CondorUDPData condorData)
        {
            myTarget.Z = Z;
            ModeStatus = Mode.InLanding;
            bool bReadyForGearOut = condorData.ias < 135 / 3.6;

            if (!GearExtracted && bReadyForGearOut)
            {
                PushKey(VirtualKeyCode.VK_G);
                InputSimulator sim = new InputSimulator();
                sim.Keyboard.Sleep(1000);
                GearExtracted = true;
            }

            if (!WaterDumping && condorData.water > 0)
            {
                PushKey(VirtualKeyCode.VK_W);
                WaterDumping = true;
                PushKey(VirtualKeyCode.F2);
            }
            else if (WaterDumping && condorData.water == 0)
            {
                PushKey(VirtualKeyCode.VK_W);
                WaterDumping = false;
            }
        }

        public Point3d GetTarget()
        {
            return new Point3d(myTarget.X, myTarget.Y, myTarget.Z);
        }


        public void SetTarget(float X, float Y, float Xpre, float Ypre, float Xpos, float Ypos)
        {
            double dx, dy, dl;
            dx = X - Xpre;
            dy = Y - Ypre;
            dl = Math.Sqrt(dx * dx + dy * dy);
            double dxpos, dypos, dlpos;
            dxpos = Xpos - Xpre;
            dypos = Ypos - Ypre;
            if (dl == 0)
            {
                myTarget.X = X;
                myTarget.Y = Y;
                return;
            }
            dx /= dl;
            dy /= dl;

            dlpos = dx * dxpos + dy * dypos + 2500;
            if (dlpos > dl)
                dlpos = dl;
            if (dlpos < 0)
                dlpos = 0;

            myTarget.X = Convert.ToSingle(Xpre + dx * dlpos);
            myTarget.Y = Convert.ToSingle(Ypre + dy * dlpos);
        }
        public void SetSpeed(ref CondorUDPData condorData)
        {
            AirSpeedKmH = condorData.ias*3.6f;
            dPitchTarget = condorData.pitch;
        }

        public void SetSpeed(float airspeed)
        {
            AirSpeedKmH = Utils.roundKmHAirspeed10(airspeed);
        }

        public void Pause(int wait)
        {
            if (Debug)
                return;
            InputSimulator sim = new InputSimulator();
            sim.Keyboard.Sleep(wait);
            PushKey(VirtualKeyCode.VK_P);
        }
        public void CloseBrakes()
        {
            if (Debug)
                return;
            PushKey(VirtualKeyCode.VK_N, 2000);

        }
        public void Wait(int wait)
        {
            if (Debug)
                return;
            InputSimulator sim = new InputSimulator();
            sim.Keyboard.Sleep(wait);
        }
        public void Clear()
        {
            InputSimulator sim = new InputSimulator();
            sim.Keyboard.KeyUp(VirtualKeyCode.UP);
            sim.Keyboard.KeyUp(VirtualKeyCode.DOWN);
            sim.Keyboard.KeyUp(VirtualKeyCode.LEFT);
            sim.Keyboard.KeyUp(VirtualKeyCode.RIGHT);
            sim.Keyboard.KeyUp(VirtualKeyCode.VK_X);
            sim.Keyboard.KeyUp(VirtualKeyCode.VK_Z);
        }

        public void Calc(FlightPathPoint flightPoint, CondorUDPData condorData, ThermalLocation thermal, 
            TerrainMap terrainMap, FlightPlanData flightPlanData, Dijkstra dijkstra, AirportItem airport, 
            YoloData yoloData,
            RichTextBox richTextBox)
        {

            double dTimeStep = 3600 * (condorData.time - dTimePrev);
            InPause = dTimeStep == 0.0;

            float dLandingDiff = 1000;
            if (InLanding())
            {
                dijkstra.myInlanding = true;
                if (terrainMap.GetHeight(flightPoint.getPosition(), out float height))
                {
                    dLandingDiff = flightPoint.z - height;
                }
            }


            if (InStateOfAvoidance > 0)
            {
                InStateOfAvoidance--;
                //richTextBox.AppendText(Environment.NewLine + "Avoidance "
                //    + InStateOfAvoidance.ToString());
                //richTextBox.ScrollToCaret();
            }
            if (IWasInStateOfAvoidance > 0)
            {
                IWasInStateOfAvoidance--;
            }

            //if (false)//thermal.bInThermal)
            //{
            //    richTextBox.AppendText(Environment.NewLine + "Thermal radius "
            //        + thermal.dThermalRadius.ToString("F0"));
            //    if (thermal.bNarrowThermal)
            //    {
            //        richTextBox.AppendText(Environment.NewLine + "Narrow Thermal");
            //    }
            //    richTextBox.ScrollToCaret();
            //}


            if (terrainMap.bNeedAvoidance)
                IWasInStateOfAvoidance = 5;

            dFactorV2Pitch = 0.025;
            dFactorSin2Bank = 2.2;
            float dImplicitDt = 10;// 1 sec

            dTimePrev = condorData.time;
            if (dTimeStep <= 0)
                dTimeStep = 0.5;

            float dSpeed = Convert.ToSingle(Math.Sqrt(condorData.vx * condorData.vx + condorData.vy * condorData.vy));

            float targetx, targety;
            targetx = myTarget.X - flightPoint.x;
            targety = myTarget.Y - flightPoint.y;
            if (!thermal.bInThermal)
            {
                targetx -= flightPoint.windx / dSpeed;
                targety -= flightPoint.windy / dSpeed;
            }

            TargetHeading = Convert.ToSingle(Math.Atan2(targety, targetx) * 180 / Math.PI);



        float dd = Convert.ToSingle(Math.Sqrt(targetx * targetx + targety * targety));
            if (dd > 0)
            {
                targetx /= dd;
                targety /= dd;
            }

            double dWindSide = 0;
            bool bFlyingDownwind = (condorData.vx * flightPoint.windx + condorData.vy * flightPoint.windy > 0);
            double dAirX = condorData.vx;// - flightPoint.windx;
            double dAirY = condorData.vy;// - flightPoint.windy;
            double dAirZ = 0;// condorData.vz - flightPoint.vario;
            double dAir = Math.Sqrt(dAirX * dAirX + dAirY * dAirY + dAirZ * dAirZ);
            if (dAir == 0)
                dAir = 1;

            double dDirectionSin = -(-dAirX * targety + dAirY * targetx) / dAir;
            double dDirectionCos = (dAirX * targetx + dAirY * targety) / dAir;
           if (terrainMap.bNeedAvoidance)
            {
                //dDirectionSin = dDirectionSin;
                //dDirectionSin = Math.Sign(dDirectionSin);
            }
            else if (dDirectionCos < 0.1)
            {
               if (thermal.bInThermal && !terrainMap.bRidge)
               {
                    if (dWindSide  == 0)
                    {
                        dDirectionSin = Math.Sign(dDirectionSin);
                    }
                    else if (dWindSide < 0)
                    {
                        dDirectionSin = 1;
                    }
                    else
                    {
                        dDirectionSin = -1;
                    }
                }
                else
                {
                    dDirectionSin = Math.Sign(dDirectionSin);
                }
            }
            condorData.targetTurn = Convert.ToSingle(dDirectionSin);

            if (InLanding() )//&& dLandingDiff < 100)
                dDirectionSin += 0.15 * dImplicitDt * condorData.turnrate;
            else if (!terrainMap.bNeedAvoidance || IWasInStateOfAvoidance > 0)
                dDirectionSin += 0.15 * dImplicitDt * condorData.turnrate;
            else
                dDirectionSin += 0.11 * dImplicitDt * condorData.turnrate;

 
            double dVarioVario = condorData.vario - dVarioPrev;
            dVarioPrev = condorData.vario;

            double dPositiveBankRate = (Math.Abs(condorData.bank) - dBankPrev) / dTimeStep;
            dBankPrev = Math.Abs(condorData.bank);




            double dBankTarget = -dFactorSin2Bank * dDirectionSin;
            double dBankMaxToUse = dBankMax;

            if (dijkstra.myIsEscape)
            {
                dBankTarget *= 1.3;
                dBankMaxToUse = 80 * Math.PI / 180;
            }
            else if (!InLanding() && terrainMap.bRidge)
            {
                if (thermal.bInThermal)
                {
                    double dFactBank = Utils.interpolate(condorData.vario, thermal.varioMax - 1.5, 2, thermal.varioMax, 1.5);

                    dBankTarget *= dFactBank;
                    dBankMaxToUse = 85 * Math.PI / 180;
                }
                else
                {
                    dBankTarget *= 1.3;
                    dBankMaxToUse = 80 * Math.PI / 180;
                }
            }
            else if (thermal.bInThermal)
            {
                if (thermal.bNarrowThermal)
                {
                    dBankTarget *= 1.4;
                    dBankMaxToUse = 85 * Math.PI / 180;
                    if(dVarioVario > 0)
                        dBankMaxToUse = 60 * Math.PI / 180;
                }
                else
                {
                    double dFactBank = Utils.interpolate(condorData.vario, thermal.varioMax - 1.5, 1.5, thermal.varioMax, 0.5);
                    dBankMaxToUse *= dFactBank;
                    if (bFlyingDownwind)
                    //                   if (dVarioRate < 0 || (bIsDownOfThermal && bFlyingDownwind))
                    {
                        dBankTarget *= Utils.interpolate(condorData.vario, thermal.varioMax - 1.5, 2, thermal.varioMax, 1.4);
                        dBankMaxToUse = 75 * Math.PI / 180;
                    }
                    else
                    {
                        dBankTarget *= 1.4;
                    }
                }
            }
            if (dBankTarget > dBankMaxToUse)
                dBankTarget = dBankMaxToUse;
            else if (dBankTarget < -dBankMaxToUse)
                dBankTarget = -dBankMaxToUse;

            double dBankDelta;
            int iBank;

            double dBankImplicit = dImplicitDt * Utils.interpolate(condorData.ias, 25, 3.0, 35, 1);



            switch (ModeStatus)
            {
                case Mode.InTow:
                    {
                        double towBank = yoloData.dTowPlaneBank + yoloData.dTowPlaneX;
                        double dTowBankRate = (towBank - dTowBankPrev) / dTimeStep;
                        double dTowBankRate2 = (dTowBankPrev - dTowBankPrev2) / dTimeStep;
                        if (!InPause)
                        {
                            dTowBankPrev2 = dTowBankPrev;
                            dTowBankPrev = towBank;
                        }
                        dTowBankRate = 0.5*(dTowBankRate + dTowBankRate2);

                        dBankTarget = (towBank + 4*dTowBankRate) * Utils.interpolate(dLandingDiff, 2, 0, 20, 1.2) - condorData.bank;
                        dBankImplicit *= 0.8;
                        break;
                    }
                case Mode.InLanding:
                    {
                        dBankTarget *= Utils.interpolate(dLandingDiff, 1, 0, 25, 1);
                        break;
                    }

            }


            //            if (terrainMap.bNeedAvoidance || (bIsDownOfThermal && bFlyingDownwind))
            if (ModeStatus == Mode.InTow)
            {
                double dBankSpeedCorrection = Utils.interpolate(condorData.ias, 33, 0.9, 50, 1.1);
                dBankDelta = dBankTarget + dBankImplicit * condorData.bankrate;
                dBankDelta *= dBankSpeedCorrection;
            }
            else if (!InLanding() && (terrainMap.bRidge || dijkstra.myIsEscape))
            {
                double dBankSpeedCorrection = Utils.interpolate(condorData.ias, 33, 0.7, 50, 1.1);
                dBankDelta = dBankTarget + dBankImplicit * condorData.bankrate - condorData.bank;
                dBankDelta *= dBankSpeedCorrection;
            }
            else
            {
                double dBankSpeedCorrection = Utils.interpolate(condorData.ias, 33, 0.5, 50, 1);
                dBankDelta = dBankTarget + dBankImplicit * condorData.bankrate - condorData.bank;
                dBankDelta *= dBankSpeedCorrection;
            }

            switch (ModeStatus)
            {
                case Mode.InTow:
                    {
                        dBankDelta *= Utils.interpolate(condorData.ias, 2, 0, 25, 1.5);
                        //dBankDelta *= Utils.interpolate(condorData.ias, 2, 0, 5, 1);
                        break;
                    }
                case Mode.InLanding:
                    {
                        dBankDelta *= Utils.interpolate(condorData.ias, 10, 4, 45, 1);
                        break;
                    }

            }

            condorData.targetBank = Convert.ToSingle(dBankTarget);

            if (dijkstra.myIsEscape)
                iBank = Convert.ToInt32(Utils.interpolate(condorData.ias, 30, 200, 55, 120) * dBankDelta);
            else if (terrainMap.bRidge || IWasInStateOfAvoidance > 0)
                iBank = Convert.ToInt32(130 * dBankDelta);
            else
                iBank = Convert.ToInt32(110 * dBankDelta);


            dTresholdBank = Utils.deg2rad(1);

            stickRoll = -iBank;

            if (!InPause && !InReplay)
            {
                if (dBankDelta > dTresholdBank)
                {
                    PushKey(VirtualKeyCode.LEFT, iBank);
                }
                else if (dBankDelta < -dTresholdBank)
                {
                    PushKey(VirtualKeyCode.RIGHT, -iBank);
                }
            }

            /////////////////////////////////////////
            dPitchSmoothed = 0.1 * dPitchSmoothed + 0.9 * condorData.pitch;
            double dPitchrate = (dPitchSmoothed - dPitchPrev) / dTimeStep;
            dPitchPrev = dPitchSmoothed;

            condorData.acceleration =  (condorData.ias - dSpeedPrev) / dTimeStep;
            //richTextBox.AppendText(Environment.NewLine + "Acceleration = "
            //+ condorData.ias.ToString() + " "
            //+ condorData.acceleration.ToString());
            //richTextBox.ScrollToCaret();


            //            bReleasePitch = condorData.acceleration * dSpeedPrev < 0;

           bool bInRealFinalApproachNoSpeedAction = (flightPlanData.RemainingDistance + 15000) < dijkstra.myFlightRange;


            double dAirSpeedToUseKmH = AirSpeedKmH;

            dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, AirSpeedKmH - 10, "Preset");

            float dTASNottoExceed = NaviCon.TAS2IAS(275, condorData.altitude);

            //richTextBox.AppendText(Environment.NewLine + "IAS VNE : "
            //    + dTASNottoExceed.ToString());
            //richTextBox.ScrollToCaret();


            dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, dTASNottoExceed, "TASNottoExceed");

            dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, AirSpeedKmH - 3.6 * dReducedSpeedForFinalApproach,"ForFinalApproach");
            if (!bInRealFinalApproachNoSpeedAction && condorData.vario > 1 && flightPoint.z < 4300)
            {
                double dMaxSpeedInThermal = Utils.interpolate(dijkstra.myFlightRange, 40000, 160, 60000, dAirSpeedToUseKmH);
                double dVarioSpeed = Utils.interpolate(condorData.vario, 1, dAirSpeedToUseKmH, 6, dMaxSpeedInThermal);
                dVarioSpeed = Utils.interpolate(dijkstra.myFlightRange, 20000, 150, 45000, dVarioSpeed);
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, dVarioSpeed, "Adapt for vario");
            }

            dSpeedPrev = condorData.ias;

            if (!bInRealFinalApproachNoSpeedAction && flightPlanData.ParallelDistanceToNextWayPoint(flightPoint) < 2500)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 155, "Close to Target");
            }




            if (flightPlanData.nextWayPoint == 0)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 155, "No Waypoint");
            }

            if (dijkstra.myIsEscape)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 135, "Escape");
            }

            if (dijkstra.myDijkstraPath.Curvature < 0)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 155, "Curved path");
            }

            if (dijkstra.myRidgeCrossingRange < 5000 && dijkstra.myRidgeCrossingGap < 100 && condorData.vario > 0)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, Utils.interpolate(dijkstra.myRidgeCrossingGap, 0, 130, 100, 175), "Ridge Crossing");
            }


            if (dijkstra.myFlightRange < 15000 && condorData.vario > -2)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, Utils.interpolate(dijkstra.myFlightRange, 8000, 130, 15000, 155), "Short range");
            }

            if (!bInRealFinalApproachNoSpeedAction && terrainMap.bRidge)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, Utils.interpolate(dijkstra.myDijkstraPath.Curvature, 0, 135, 1, dAirSpeedToUseKmH), "Ridge"); ;
            }
            if (thermal.bInThermal && !terrainMap.IsInFinalApproach)
            {
                if (condorData.vario < thermal.varioMax - 2)
                    dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 150, "Thermal Fast");
                else
                    dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 125, "Thermal Slow");

            }

            if (terrainMap.IsInFinalApproach && (terrainMap.NeedActionToMeetFinalDestination > 0))
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 125, "Final Appraoch but need height");
            }

            if (ModeStatus == Mode.InTow)
            {
                dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 110, "Towing speed");
            }



            //dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, Utils.interpolate(flightPoint.z, 5000, AirSpeedKmH, 7000, 175), "To high");

            if (InLanding())
            {
                if (dLandingDiff < 400)
                    dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, Utils.interpolate(dLandingDiff, 0, 120, 300, 140), "Landing Slow");
                else
                    dAirSpeedToUseKmH = SetSpeedTarget(dAirSpeedToUseKmH, 155, "Landing Fast");
            }

            double dAirSpeedToUse = dAirSpeedToUseKmH/ 3.6;
            TargetSpeed = Convert.ToSingle(dAirSpeedToUse);
            double dDeltaV = dAirSpeedToUse - condorData.ias;
            double dDeltaVOriginal = dDeltaV;


            if (condorData.ias > (dTASNottoExceed + 3) / 3.6)
            {
                dPitchTarget = Utils.deg2rad(2);
            }
            if (condorData.ias > (dTASNottoExceed + 4) / 3.6)
            {
                dPitchTarget = Utils.deg2rad(5);
            }

            if (dDeltaV < -20 )
            {
                double dUpCorrection = Utils.interpolate(condorData.ias, 30, Math.Max(dPitchTarget, 5), 60, 8);
                dPitchTarget  = Utils.deg2rad(dUpCorrection);
            }

            if (InLanding())
            {
                dDeltaV += Utils.interpolate(flightPoint.z + 5 * flightPoint.vario - myTarget.Z, -15, -2, 15, 2);
                //if (dLandingDiff < 20)
                //{
                //    double dVarioTarget = -0.2;
                //    dDeltaV -= Utils.interpolate(dVarioTarget - flightPoint.vario, -3, -5, 3, 5); ;
                //}
            }


                double dFactV = Utils.interpolate(condorData.ias, 33, 0.6, 50, 0.8);
            dDeltaV -= dFactV * dImplicitDt * condorData.acceleration;

            dPitchRateTarget = 0.3 * dPitchRateTarget + 0.7 * dPitchrate;

            if (Math.Abs(dDeltaV) > 5){
                dPitchTarget -= 0.08 * dFactorV2Pitch * dDeltaV;
            }
            else
            {
                dPitchTarget -= 0.05 * dFactorV2Pitch * dDeltaV;
            }

            if (dPitchTarget > Utils.deg2rad(15))
            {
                dPitchTarget = Utils.deg2rad(15);
            }
            else if (!InLanding() && dPitchTarget < Utils.deg2rad(-7))
            {
                dPitchTarget = Utils.deg2rad(-7);
            }
            else if (InLanding() && dPitchTarget < Utils.deg2rad(-20))
            {
                dPitchTarget = Utils.deg2rad(-20);
            }

            if (InLanding() && flightPoint.speed < 10)
            {
                dPitchTarget = Utils.deg2rad(-5);
                if (flightPoint.speed < 2)
                    InPause = true;
            }
            if (dLandingDiff < 100)
            {
                dPitchTarget += Utils.deg2rad(Utils.interpolate(flightPoint.z + 5 * flightPoint.vario - myTarget.Z, -15, 8, 0, 0));
            }

            double dPitchDelta;
            if (ModeStatus == Mode.InTow)
            {
                double dTowPitchRate = (yoloData.dTowPlaneY - dTowPitchPrev) / dTimeStep;
                double dTowPitchRate2 = (dTowPitchPrev - dTowPitchPrev2) / dTimeStep;
                if (!InPause)
                {
                    dTowPitchPrev2 = dTowPitchPrev;
                    dTowPitchPrev = yoloData.dTowPlaneY;
                }

                dTowPitchRate += 2*(dTowPitchRate - dTowPitchRate2);

                dPitchDelta = 12 * (yoloData.dTowPlaneY + 5 * dTowPitchRate) + Utils.deg2rad(0) + 1.5 * dPositiveBankRate;
                //dPitchDelta = -7 * (yoloData.dTowPlaneY + 3 * (dTowPitchRate + condorData.pitchrate) + Utils.deg2rad(0)) - dPitchSmoothed;
                //dPitchDelta -= dFactorV2Pitch * dDeltaV;
                //dPitchDelta = 2 * (yoloData.dTowPlaneY + Utils.deg2rad(0)) - 1.5 * dImplicitDt * dPitchrate
                //    - dPitchSmoothed;
                dPitchDelta *= Utils.interpolate(condorData.ias, 25, 0, 30, 1.2);
            }
            else
            {
                dPitchDelta = dPitchTarget
                    - dFactorV2Pitch * dDeltaV
                    + 1.5 * dPositiveBankRate
                    - 1.5 * dImplicitDt * dPitchrate
                    - dPitchSmoothed;
            }





            if (dijkstra.myIsEscape)
            {
                dPitchDelta *= Utils.interpolate(condorData.ias, 30, 1, 50, 1.2);
            }
            if (InLanding())
            {
                dPitchDelta *= Utils.interpolate(condorData.ias, 20, 5, 30, 1);
            }

            condorData.targetPitch = Convert.ToSingle(dPitchDelta + condorData.pitch);


            int iPitchDt = Math.Min(Convert.ToInt32(90*Math.Abs(dPitchDelta)), 170);


            //richTextBox.AppendText(Environment.NewLine + "Pitch :"
            //     + Utils.rad2deg(condorData.pitch).ToString("F1")
            //     + " , "
            //     + Utils.rad2deg(dPitchTarget).ToString("F1")
            //     + " , "
            //     + Utils.rad2deg(dPitchSmoothed).ToString("F1")
            //     + " , "
            //     + Utils.rad2deg(dPitchrate).ToString("F1")
            //     + " , "
            //     + Utils.rad2deg(turnrate).ToString("F1")
            //     + " , "
            //     + Utils.rad2deg(dDeltaAcc).ToString("F1")
            //     + " : "
            //     + (Math.Sign(dPitchDelta)*iPitchDt).ToString()
            //     );
            //richTextBox.ScrollToCaret();

            if (dPitchDelta > 0)
                stickPitch = 2*iPitchDt;
            else
                stickPitch = -2*iPitchDt;

            dTresholdPitch = Utils.deg2rad(1);
            if (!InPause && !InReplay)
            {
                if (dPitchDelta > dTresholdPitch)
                {
                    PushKey(VirtualKeyCode.DOWN, iPitchDt);
                }
                else if (dPitchDelta < -dTresholdPitch)
                {
                    PushKey(VirtualKeyCode.UP, iPitchDt);
                }
            }


            double dYawStringRate;


            double dYawDelta;
            if (ModeStatus == Mode.InTow)
            {
                //double yawTow = condorData.yawstring - yoloData.dTowPlaneX / 3;
                double yawTow = yoloData.dTowPlaneX ;
                dYawStringRate = (yawTow - dYawStringPrev) / dTimeStep;
                dYawStringPrev = yawTow;
                dYawDelta = yawTow + 1 * dYawStringRate;
                condorData.targetTurn = 20*Convert.ToSingle(dYawDelta);
            }
            else
            {
                dYawStringRate = (condorData.yawstring - dYawStringPrev) / dTimeStep;
                dYawStringPrev = condorData.yawstring;
                dYawDelta = -condorData.yawstring - 0.5 * dYawStringRate;

            }
            int YawIntensity = Convert.ToInt32(500 * dYawDelta);

            //richTextBox.AppendText(Environment.NewLine + "YawString "
            //    + condorData.yawstring.ToString("F3") + "; "
            //    + dYawStringRate.ToString("F3") + ";  "
            //    + YawIntensity.ToString());
            //richTextBox.ScrollToCaret();

            if (YawIntensity < 0)
                YawIntensity = -YawIntensity;

            if (InLanding() && dLandingDiff < 12)
            {
                double dYaw = condorData.yaw - 2 * condorData.yawrate;
                Vector3d vecYaw = new Vector3d(-Math.Sin(dYaw), Math.Cos(dYaw), 0);
                Vector3d vecPerp = airport.GetPerpVector();
                dYawDelta = vecYaw.dotProduct(vecPerp);
                YawIntensity = Convert.ToInt32(Utils.interpolate(dLandingDiff, 0, 150, 12, 50));
            }


            dTresholdYaw = Utils.deg2rad(1);
            if (!InPause && !InReplay)
            {
                if (dYawDelta > dTresholdYaw)
                {
                    PushKey(VirtualKeyCode.VK_X, YawIntensity);
                }
                else if (dYawDelta < -dTresholdYaw)
                {
                    PushKey(VirtualKeyCode.VK_Z, YawIntensity);
                }
            }

            dSmoothedVario = 0.8f * dSmoothedVario + 0.2f*flightPoint.vario;

            if (!InPause && !InReplay && InLanding())
            {
                float dZ = flightPoint.z  + 5 * flightPoint.vario;
                if (dLandingDiff < 100)
                {
                    dZ -= Convert.ToSingle(Utils.interpolate(dDeltaVOriginal, -15, -50, 15, 50));
                }

                float dDistToAirport = flightPoint.distanceTo(airport.GetPosition());

                if (dZ > airport.Altitude + 500 + dDistToAirport * 25)
                    dZ += 250;
                else if (airport.PreLanding && airport.QX < -3000 && dDistToAirport < 4000)
                    dZ += 250;
                else if (flightPoint.vec.dotProduct(airport.GetVector()) < 0 && dZ > airport.Altitude + 600)
                    dZ += 250;

                float dZDiff = 5;
                if (dLandingDiff < 10)
                    dZDiff = 1;

                iBrakeDelay += 1;
                if (dLandingDiff < 5)
                {
                    PushKey(VirtualKeyCode.VK_B, 100);
                    if (condorData.ias < 20)
                        PushKey(VirtualKeyCode.DECIMAL);
                }
                else
                {
                    if (iBrakeDelay == 2)
                    {
                        iBrakeDelay = 0;
                        if (dZ < myTarget.Z - dZDiff)
                        {
                            PushKey(VirtualKeyCode.VK_N, 100);
                        }
                        else if (dZ > myTarget.Z + dZDiff)
                        {
                            PushKey(VirtualKeyCode.VK_B, 100);
                        }
                    }
                }


            }

            //Flaps
            if (ModeStatus != Mode.InTow && !InPause && !InReplay)
            {
                if (myFlapStatus == flapStatus.kPlus1 && condorData.ias > 105 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_F, 120);
                    myFlapStatus = flapStatus.kNeutral;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to Neutral ");
                    richTextBox.ScrollToCaret();

                }
                else if (myFlapStatus == flapStatus.kNeutral && condorData.ias > 125 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_F, 120);
                    myFlapStatus = flapStatus.kMinus1;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to -1 ");
                    richTextBox.ScrollToCaret();

                }
                else if (myFlapStatus == flapStatus.kMinus1 && condorData.ias > 155 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_F, 120);
                    myFlapStatus = flapStatus.kMinus2;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to -2 ");
                    richTextBox.ScrollToCaret();

                }
                else if (myFlapStatus == flapStatus.kMinus2 && condorData.ias > 175 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_F, 120);
                    myFlapStatus = flapStatus.kMinusSpeed;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to S ");
                    richTextBox.ScrollToCaret();

                }




                else if (myFlapStatus == flapStatus.kMinusSpeed && condorData.ias < 165 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_V, 120);
                    myFlapStatus = flapStatus.kMinus2;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to -2 ");
                    richTextBox.ScrollToCaret();
                }
                else if (myFlapStatus == flapStatus.kMinus2 && condorData.ias < 145 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_V, 120);
                    myFlapStatus = flapStatus.kMinus1;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to -1 ");
                    richTextBox.ScrollToCaret();
                }
                else if (myFlapStatus == flapStatus.kMinus1 && condorData.ias < 120 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_V, 120);
                    myFlapStatus = flapStatus.kNeutral;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to Neutral ");
                    richTextBox.ScrollToCaret();
                }
                else if (myFlapStatus == flapStatus.kNeutral && condorData.ias < 100 / 3.6)
                {
                    PushKey(VirtualKeyCode.VK_V, 120);
                    myFlapStatus = flapStatus.kPlus1;
                    richTextBox.AppendText(Environment.NewLine + "Flaps set to +1 ");
                    richTextBox.ScrollToCaret();
                }
            }

        }

        public void PushKey(VirtualKeyCode iKey)
        {
            PushKey(iKey, 50);
        }
        private void PushKey(VirtualKeyCode iKey, int iPressTime)
        {
            if (Debug)
                return;
            new Thread(() =>
            {
                InputSimulator sim = new InputSimulator();
                sim.Keyboard.KeyDown(iKey);
                sim.Keyboard.Sleep(iPressTime);
                sim.Keyboard.KeyUp(iKey);
            }).Start();
        }

    }
}
