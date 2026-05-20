using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

//This class provides emission estimation of 32 categories of untrafine particles and the total mass of all emission particles;

namespace EmissionModel
{
    class EmissionModel
    {

        public float[][] ModelParameter;
        //including:
        //0: Model ID   0-32, model 0 is the total mass;
        //1: Diameter lower bound;
        //2: Diameter upper bound;
        //3: Constant term;
        //4: Average speed coefficient;
        //5: Average volume sum coefficient;

        public int numCategories = 32;
        public int numCatColumn = 6;

        public int[][][] weatherCondition;
        //digit 1: Month, digit 2: Day, digit 3:Minute
        //1: weather condition valid for model, 0: invalid;
        public int modelYear = 2008;

        public int numDetPair;
        public int[][] detPairID;
        //First digit: station pair ID -1;
        //Second digit 0: ID; 1: A ID; 2: B ID;

        public float[][] stationVol;
        public float[][] stationSpd;
        public int maxStaID = 1600;

        public float[][][] UFPLevel;
        //digit 1: category;
        //digit 2: detector pairs;
        //digit 3: Minutes;

        public float[][][] CumulativeUFP;
        //digit 1: day ID;
        //digit 2: category;
        //digit 3: detector pairs;

        public int[] StationDetNum;

        //The maximum flowrate per lane is 2160/hour, 36/min, and 18/30 secs;
        int maxLaneRate = 18;//Using the number of lanes information to establish the upper bound;
        int maxFlowRate = 80;   //Cap the possible flow rate, flag a detector if go beyond this;
        float maxSpeed = 90;    //Cap the speed, flag a detector if go beyond this;
        int numTimeSlot = 24 * 60 * 2;//number of 30 secs time slot;

        public int[] badVolumeReading;    //Count the number of bad readings from each detector;
        public float badVolumeLimit = Convert.ToSingle(0.1);
        public int[] nonValidSpeedReading;  //Count non-valid speed readings, including those with 0 volume and have a value of -1;
        public float nonValidSpeedLimit = Convert.ToSingle(0.25);
        public Boolean[] validStation;   //Flag functioning stations

        public string Exp_Core_Output;
        public string Exp_Density_Output;
        public string Exp_Time_Output;
        public string Exp_Bin_Time_Output;
        
        public int numValidWeekday;
        public int numValidWeekend;
        public float[][] BinDailyTotalWD;
        public float[][] BinDailyTotalWK;
        public float[][] BinDailyWeightAverageWD;
        public float[][] BinDailyWeightAverageWK;
        //1: Bin Category 2: 8,11,2,5,8
        public int numBinTimePoint = 13;
        public float[][] binTotalVolumeWD;
        public float[][] binTotalVolumeWK;

        public float[][] modelVolume;
        public float[][] modelSpeed;
        //1: DetectorPair;
        //2: Minutes;

        public int maxValidDay = 60;
        public int[][] validDays;
        //1:minutes;
        //2:detectorNum;
        public float[][] nDayCore;
        //1:minutes;
        //2:detectorNum;

        //Statistics of the number of valid volume and speed combination;
        public int[][] validNumMinutes;
        public int nonValidNumMinutes;
        public int[][] validNumVolume;

        //Weekday and weekend average number of particle;
        //1: Minutes;
        //2: DetectorPair;
        
        public float[][] weekdayAvePNDet;
        public float[][] weekendAvePNDet;
        public int[][] weekdayAveNumDays;
        public int[][] weekendAveNumDays;
        public float[][] weekdayAveTotalPNDet;
        public float[][] weekendAveTotalPNDet;

        //Daily total vs Daily average;
        //1: Day;
        //2: DetectorPair;
        public float[][] dailyTotalPNDet;
        public float[][] dailyWeightedAVPNDet;
        public float[][] dailyTotalVolume;
        public float[][] dailyAvePNDet;
        public int[][] dailyTotalNumMinutes;

        public float[] averageDailyTotalPNDet;
        public float[] averageDailyWeightedAVPNDet;
        public float[] averageDailyAvePNDet;
        public float[] totalVolume;
        public int[] totalMinutes;
        
        public EmissionModel()
        {
            //Default Constructor;
        }

        #region Constructor
        public EmissionModel(string s_detectorPairs, string s_modelParameters, string s_weatherCondition)
        {
            Console.WriteLine("Start to load the detector station pair file");
            string[] s_contents = File.ReadAllLines(s_detectorPairs);
            numDetPair = s_contents.Length;
            detPairID = new int[numDetPair][];
            for (int i = 0; i < numDetPair; i++)
            {
                string[] s_split = s_contents[i].Split(',');
                detPairID[i] = new int[3];
                detPairID[i][0] = Convert.ToInt32(s_split[0]);
                detPairID[i][1] = Convert.ToInt32(s_split[1]);
                detPairID[i][2] = Convert.ToInt32(s_split[2]);
            }
            Console.WriteLine("Start to load model parameters");
            s_contents = null;
            s_contents = File.ReadAllLines(s_modelParameters);
            ModelParameter = new float[numCategories+1][];
            for (int i = 0; i < numCategories + 1; i++)
            {
                string[] s_split = s_contents[i].Split(',');
                ModelParameter[i] = new float[numCatColumn];
                for (int j = 0; j < numCatColumn; j++)
                {
                    ModelParameter[i][j] = Convert.ToSingle(s_split[j]);  
                }
            }
            Console.WriteLine("Start to load weather condition file");
            s_contents = null;
            s_contents = File.ReadAllLines(s_weatherCondition);
            weatherCondition = new int[12+1][][];
            for (int i = 1; i < 12 + 1; i++)
            {
                int numDays = System.DateTime.DaysInMonth(modelYear, i);
                weatherCondition[i] = new int[numDays+1][];
                for (int j = 1; j < numDays + 1; j++)
                {
                    int numMinutes = 60 * 24;
                    weatherCondition[i][j] = new int[numMinutes];
                    for (int k = 0; k < numMinutes; k++)
                    {
                        weatherCondition[i][j][k] = -1;
                    }
                }
            }
            //Start to load;
            int numRows = s_contents.Length;
            for (int i = 1; i < numRows; i++)
            {
                string[] s_split = s_contents[i].Split(',');
                int modelmonth = Convert.ToInt32(s_split[3]);
                int modelday = Convert.ToInt32(s_split[0]);
                int modeltime = Convert.ToInt32(s_split[1]);
                int modelhour = Convert.ToInt32(modeltime / 100);
                int modelminute = modeltime - modelhour * 100;
                int valid = Convert.ToInt32(s_split[9]);
                int cellID = modelhour * 60 + modelminute;
                weatherCondition[modelmonth][modelday][cellID] = valid;
            }
            //Fill the empty cells in between;
            for (int i = 1; i < 12 + 1; i++)
            {
                int numDays = System.DateTime.DaysInMonth(modelYear, i);
                for (int j = 1; j < numDays + 1; j++)
                {
                    int currentStatus;
                    int currentID;
                    int numMinutes = 60 * 24;
                    //Initialize the status for midnight;
                    if (weatherCondition[i][j][0] == 0 | weatherCondition[i][j][0] == 1)
                    {
                        currentStatus = weatherCondition[i][j][0];
                        currentID = 0;
                    }
                    else
                    {
                        if (j != 1)
                        {
                            //This is not the first day of the month;
                            currentStatus = weatherCondition[i][j - 1][numMinutes - 1];
                            weatherCondition[i][j][0] = currentStatus;
                        }
                        else if (i != 1)
                        {
                            //This is the first day of the month, but the month is not Jan;
                            currentStatus = weatherCondition[i - 1][System.DateTime.DaysInMonth(modelYear, i - 1)][numMinutes - 1];
                            weatherCondition[i][j][0] = currentStatus;
                        }
                        else
                        {
                            weatherCondition[i][j][0] = 0;
                            currentStatus = 0;
                        }
                        currentID = 0;
                    }
                    //Look at subsequent minutes;
                    for (int k = 1; k < numMinutes; k++)
                    {
                        if (weatherCondition[i][j][k] == 0 | weatherCondition[i][j][k] == 1)
                        {
                            //Time point where we have observations;
                            if (weatherCondition[i][j][k] != currentStatus)
                            {
                                int intermID = Convert.ToInt32(currentID + (k - currentID) / 2);
                                for (int l = currentID + 1; l < intermID + 1; l++)
                                {
                                    weatherCondition[i][j][l] = currentStatus;
                                }
                                currentStatus = weatherCondition[i][j][k];
                                for (int l = intermID + 1; l < k; l++)
                                {
                                    weatherCondition[i][j][l] = currentStatus;
                                }                               
                            }
                            else
                            {
                                for (int l = currentID + 1; l < k; l++)
                                {
                                    weatherCondition[i][j][l] = currentStatus;
                                }
                            }
                            currentID = k;
                        }
                        else if (k == numMinutes - 1)
                        {
                            //End of one day, one more digit;
                            for (int l = currentID + 1; l < k+1; l++)
                            {
                                weatherCondition[i][j][l] = currentStatus;
                            }
                        }
                        else
                        {
                            //There is no observation, continue;
                        }
                    }
                }
            }
            //End of loading weather conditions;
        }
        #endregion

        #region EstimateModel for one day
        public void EstimateModel(DateTime startDate, DateTime endDate, string s_stationdir,string s_exportdir)
        {
            int modelYear;
            int modelMonth;
            int modelDay;
            int dayofweek;
            TimeSpan span = endDate.Subtract(startDate);
            int numCalendarDays = span.Days + 1;
            CumulativeUFP = new float[numCalendarDays][][];

            //Numofvalidcells;
            validNumMinutes = new int[7][];
            validNumVolume = new int[7][];
            for (int i = 0;i<7;i++)
            {
                validNumMinutes[i] = new int[8];
                validNumVolume[i] = new int[8];
                for (int j=0;j<8;j++)
                {
                    validNumMinutes[i][j] = 0;
                    validNumVolume[i][j] = 0;
                }
            }
            nonValidNumMinutes = 0;

            //Detector PN output;
            int numMinutes = 24 * 60;
            weekdayAvePNDet = new float[numMinutes][];
            weekendAvePNDet = new float[numMinutes][];
            weekdayAveNumDays = new int[numMinutes][];
            weekendAveNumDays = new int[numMinutes][];
            weekdayAveTotalPNDet = new float[numMinutes][];
            weekendAveTotalPNDet = new float[numMinutes][];
            for (int i = 0; i < numMinutes; i++)
            {
                weekdayAvePNDet[i] = new float[numDetPair];
                weekendAvePNDet[i] = new float[numDetPair];
                weekdayAveNumDays[i] = new int[numDetPair];
                weekendAveNumDays[i] = new int[numDetPair];
                weekdayAveTotalPNDet[i] = new float[numDetPair];
                weekendAveTotalPNDet[i] = new float[numDetPair];
                for (int j = 0; j < numDetPair; j++)
                {
                    weekdayAvePNDet[i][j] = 0;
                    weekdayAveNumDays[i][j] = 0;
                    weekendAvePNDet[i][j] = 0;
                    weekendAveNumDays[i][j] = 0;
                    weekdayAveTotalPNDet[i][j] = 0;
                    weekendAveTotalPNDet[i][j] = 0;
                }
            }

            //Daily total vs Daily average;
            int periods = 44;
            dailyTotalPNDet = new float[periods][];
            dailyWeightedAVPNDet = new float[periods][];
            dailyTotalVolume = new float[periods][];
            dailyAvePNDet = new float[periods][];
            dailyTotalNumMinutes = new int[periods][];
            for (int i = 0; i < periods; i++)
            {
                dailyTotalPNDet[i] = new float[numDetPair];
                dailyWeightedAVPNDet[i] = new float[numDetPair];
                dailyTotalVolume[i] = new float[numDetPair];
                dailyAvePNDet[i] = new float[numDetPair];
                dailyTotalNumMinutes[i] = new int[numDetPair];
                for (int j = 0; j < numDetPair; j++)
                {
                    dailyTotalPNDet[i][j] = 0;
                    dailyWeightedAVPNDet[i][j] = 0;
                    dailyTotalVolume[i][j] = 0;
                    dailyAvePNDet[i][j] = 0;
                    dailyTotalNumMinutes[i][j] = 0;
                }
            }
            int validDaysCounter = 0;

            //44Day average;
            averageDailyTotalPNDet = new float[numDetPair];
            averageDailyWeightedAVPNDet = new float[numDetPair];
            averageDailyAvePNDet = new float[numDetPair];
            totalVolume = new float[numDetPair];
            totalMinutes = new int[numDetPair];
            for (int i = 0; i < numDetPair; i++)
            {
                averageDailyAvePNDet[i] = 0;
                averageDailyWeightedAVPNDet[i] = 0;
                averageDailyTotalPNDet[i] = 0;
                totalVolume[i] = 0;
                totalMinutes[i] = 0;
            }

            //Prepare ouuput results;
            Exp_Time_Output = "Year,Month,Day,Weekday,Minutes";
            Exp_Core_Output = "Year,Month,Day,DetPair,ASta,BSta,AverageCore,Exposure";
            Exp_Density_Output = "Year,Month,Day,DetPair,ASta,BSta,AverageDensity,Exposure";
            Exp_Bin_Time_Output = "Year,Month,Day,Weekday,Minutes";

            numValidWeekday = 0;
            numValidWeekend = 0;
            BinDailyTotalWD = new float[numCategories+1][];
            BinDailyTotalWK = new float[numCategories+1][];
            BinDailyWeightAverageWD = new float[numCategories+1][];
            BinDailyWeightAverageWK = new float[numCategories+1][];
            binTotalVolumeWD = new float[numCategories+1][];
            binTotalVolumeWK = new float[numCategories+1][];
            for (int i = 0; i<numCategories+1; i++)
            {
                BinDailyTotalWD[i] = new float[numBinTimePoint];
                BinDailyTotalWK[i] = new float[numBinTimePoint];
                BinDailyWeightAverageWD[i] = new float[numBinTimePoint];
                BinDailyWeightAverageWK[i] = new float[numBinTimePoint];
                binTotalVolumeWD[i] = new float[numBinTimePoint];
                binTotalVolumeWK[i] = new float[numBinTimePoint];
                for (int j = 0; j < numBinTimePoint; j++)
                {
                    BinDailyTotalWD[i][j] = 0;
                    BinDailyTotalWK[i][j] = 0;
                    BinDailyWeightAverageWD[i][j] = 0;
                    BinDailyWeightAverageWK[i][j] = 0;
                    binTotalVolumeWD[i][j] = 0;
                    binTotalVolumeWK[i][j] = 0;
                }
            }

            validDays = new int[24*60][];
            nDayCore = new float[24*60][];
            for (int i = 0; i < 24 * 60; i++)
            {
                validDays[i] = new int[numDetPair];
                nDayCore[i] = new float[numDetPair];
                for (int j = 0; j < numDetPair; j++)
                {
                    validDays[i][j] = 0;
                    nDayCore[i][j] = 0;
                }
            }


            //Export;
            string exportFile1 = s_exportdir + "Exposure_Time_Output.csv";
            TextWriter tw1 = new StreamWriter(exportFile1);
            tw1.WriteLine(Exp_Time_Output);

            string exportFile2 = s_exportdir + "Exposure_Density_Output.csv";
            TextWriter tw2 = new StreamWriter(exportFile2);
            tw2.WriteLine(Exp_Core_Output);

            string exportFile3 = s_exportdir + "Exposure_Core_Output.csv";
            TextWriter tw3 = new StreamWriter(exportFile3);
            tw3.WriteLine(Exp_Density_Output);

            string exportFile4 = s_exportdir + "Exposure_Bin_Time_Output.csv";
            TextWriter tw4 = new StreamWriter(exportFile4);
            tw4.WriteLine(Exp_Bin_Time_Output);


            for (int m = 0; m < numCalendarDays; m++)
            {
                DateTime currentDate = startDate.AddDays(m);
                modelYear = currentDate.Year;
                modelMonth = currentDate.Month;
                modelDay = currentDate.Day;
                dayofweek = (int)currentDate.DayOfWeek;
                
                Console.WriteLine("Start to load the traffic conditon on month" + modelMonth + " day" + modelDay);
                stationVol = new float[maxStaID + 1][];
                stationSpd = new float[maxStaID + 1][];

                //Reformat the indicator for malfunctioning statinos;
                //Remove the detector completely if more than 3/4 detectors readings are bad;
                badVolumeReading = new int[maxStaID];
                nonValidSpeedReading = new int[maxStaID];
                validStation = new Boolean[maxStaID];
                Boolean[] validVolume = new Boolean[maxStaID];
                for (int i = 0; i < maxStaID; i++)
                {
                    badVolumeReading[i] = 0;
                    nonValidSpeedReading[i] = 0;
                    validStation[i] = false;
                    validVolume[i] = false;
                }

                //Loading volume data, every 30 second;
                string s_volumefile = s_stationdir + "2008Volume\\V" + (modelYear * 10000 + modelMonth * 100 + modelDay) + ".csv";
                string[] s_contents = File.ReadAllLines(s_volumefile);
                int numRows = s_contents.Length;
                for (int i = 2; i < numRows; i++)
                {
                    string[] s_split = s_contents[i].Split(',');
                    int staID = Convert.ToInt32(s_split[0].Substring(1));
                    int numColumn = 60 * 24 * 2;
                    stationVol[staID] = new float[numColumn];
                    validVolume[staID] = true; //Have readings on volume;

                    for (int j = 0; j < numColumn; j++)
                    {
                        int column = j + 4 - 1;
                        int volume = Convert.ToInt32(Convert.ToSingle(s_split[column]));
                        int maxVolume;
                        if (StationDetNum[staID] > 0)
                        {
                            maxVolume = StationDetNum[staID] * maxLaneRate;
                        }
                        else
                        {
                            maxVolume = maxFlowRate;
                        }
                        if (volume >= 0 & volume < maxVolume)
                        {
                            stationVol[staID][j] = volume;
                        }
                        else
                        {
                            badVolumeReading[staID]++;
                            stationVol[staID][j] = -1;
                        }
                    }
                }

                //Loading speed data, every 30 seconds;
                string s_speedfile = s_stationdir + "2008Speed\\S" + (modelYear * 10000 + modelMonth * 100 + modelDay) + ".csv";
                s_contents = null;
                s_contents = File.ReadAllLines(s_speedfile);
                numRows = s_contents.Length;
                for (int i = 2; i < numRows; i++)
                {
                    string[] s_split = s_contents[i].Split(',');
                    int staID = Convert.ToInt32(s_split[0].Substring(1));
                    int numColumn = 60 * 24 * 2; //Every 30 seconds data;
                    stationSpd[staID] = new float[numColumn];
                    if (validVolume[staID] == true)
                    {
                        validStation[staID] = true; //Have readings on both;
                    }

                    for (int j = 0; j < numColumn; j++)
                    {
                        int column = j + 4 - 1;
                        float speed = Convert.ToSingle(s_split[column]);
                        if (speed > 0 & stationVol[staID][j] >= 0 & speed <= maxSpeed)
                        {
                            stationSpd[staID][j] = speed;
                        }
                        else
                        {
                            nonValidSpeedReading[staID]++;
                            stationSpd[staID][j] = -1;
                        }
                    }
                }
                //Check the limit and flag the stations that are not valid;
                for (int j = 0; j < maxStaID; j++)
                {
                    if (validStation[j] == true)
                    {
                        //Stations that have readings;
                        float badVolumeRatio = Convert.ToSingle(1.0) * badVolumeReading[j] / numTimeSlot;
                        float badSpeedRatio = Convert.ToSingle(1.0) * nonValidSpeedReading[j] / numTimeSlot;
                        if ((badVolumeRatio < badVolumeLimit) & (badSpeedRatio < nonValidSpeedLimit))
                        {
                            //Readings that are good;
                            for (int k = 0; k < numTimeSlot; k++)
                            {
                                if (stationVol[j][k] == -1)
                                {
                                    stationVol[j][k] = 0;
                                }
                                if (stationSpd[j][k] == -1 & stationVol[j][k] == 0)
                                {
                                    stationSpd[j][k] = maxSpeed;
                                }else if (stationSpd[j][k] == -1 & stationVol[j][k] > 0)
                                {
                                    stationSpd[j][k] = maxSpeed-10; //Practical free flow speed;
                                    stationVol[j][k] = 0;
                                }
                            }
                        }
                        else
                        {
                            //Not valid station, too many bad readings;
                            validStation[j] = false;
                        }
                    }
                }

                Console.WriteLine("Start to estimate model");
                //Start to estimate model;
                //Initiate model;
                //int numMinutes = 60 * 24;
                UFPLevel = new float[numCategories + 1][][];
                CumulativeUFP[m] = new float[numCategories + 1][];
                for (int i = 0; i < numCategories + 1; i++)
                {
                    UFPLevel[i] = new float[numDetPair][];
                    CumulativeUFP[m][i] = new float[numDetPair];
                    modelVolume = new float[numDetPair][];
                    modelSpeed = new float[numDetPair][];
                    for (int j = 0; j < numDetPair; j++)
                    {
                        UFPLevel[i][j] = new float[numMinutes];
                        CumulativeUFP[m][i][j] = 0;
                        modelVolume[j] =  new float[numMinutes];
                        modelSpeed[j] = new float[numMinutes];
                    }
                }
                //Start to check weather conditions;
                for (int i = 0; i < numMinutes; i++)
                {
                    if (weatherCondition[modelMonth][modelDay][i] == 0)
                    {
                        for (int j = 0; j < numDetPair; j++)
                        {
                            for (int k = 0; k < numCategories + 1; k++)
                            {
                                //Invalid weather conditions;
                                UFPLevel[k][j][i] = -2;
                            }
                        }
                    }
                    else
                    {
                        //Weather condition is valid;
                        //Start to check the volume and speed;
                        for (int j = 0; j < numDetPair; j++)
                        {
                            int staID1 = detPairID[j][1];
                            int staID2 = detPairID[j][2];
                            float volume11 = stationVol[staID1][i * 2];
                            float volume12 = stationVol[staID1][i * 2 + 1];
                            float volume21 = stationVol[staID2][i * 2];
                            float volume22 = stationVol[staID2][i * 2 + 1];
                            float speed11 = stationSpd[staID1][i * 2];
                            float speed12 = stationSpd[staID1][i * 2 + 1];
                            float speed21 = stationSpd[staID2][i * 2];
                            float speed22 = stationSpd[staID2][i * 2 + 1];
                            Boolean valid = true;
                            //float volume1 = 0;
                            //float volume2 = 0;
                            //float speed1 = 0;
                            //float speed2 = 0;
                            float volume;
                            float speed;
                            //float flowRateCap1;
                            //float flowRateCap2;
                            //if (StationDetNum[staID1] != -1)
                            //{
                            //    flowRateCap1 = StationDetNum[staID1] * maxLaneRate;
                            //}
                            //else
                            //{
                            //    flowRateCap1 = maxFlowRate;
                            //}
                            //if (StationDetNum[staID2] != -1)
                            //{
                            //    flowRateCap2 = StationDetNum[staID2] * maxLaneRate;
                            //}
                            //else
                            //{
                            //    flowRateCap2 = maxFlowRate;
                            //}
                            
                            //Check if both stations are valid;
                            if (validStation[staID1] == true & validStation[staID2] == true)
                            {
                                //Both stations are valid;
                                /*
                                if (volume11 >= 0 & volume12 >= 0 & volume21 >= 0 & volume22 >= 0 & volume11 <= flowRateCap1 & volume12 <= flowRateCap1 & volume21 <= flowRateCap2 & volume22 <= flowRateCap2)
                                {
                                    //Process one direction;
                                    if ((volume11 == 0 & volume12 > 0 & speed12 > 0 & speed12 < maxSpeed) | (volume11 > 0 & volume12 == 0 & speed11 > 0 & speed11 < maxSpeed))
                                    {
                                        volume1 = volume11 + volume12;
                                        speed1 = (speed11 * volume11 + speed12 * volume12) / (volume11 + volume12);
                                    }
                                    else if (volume11 > 0 & volume12 > 0 & speed11 > 0 & speed12 > 0 & speed11 < maxSpeed & speed12 < maxSpeed)
                                    {
                                        volume1 = volume11 + volume12;
                                        speed1 = (speed11 * volume11 + speed12 * volume12) / (volume11 + volume12);
                                    }
                                    else
                                    {
                                        valid = false;
                                    }
                                    //Process the opposite direction;
                                    if (((volume21 == 0 & volume22 > 0 & speed22 > 0 & speed22 < maxSpeed) | (volume21 > 0 & volume22 == 0 & speed21 > 0 & speed21 < maxSpeed)) & valid == true)
                                    {
                                        volume2 = volume21 + volume22;
                                        speed2 = (speed21 * volume21 + speed22 * volume22) / (volume21 + volume22);
                                    }
                                    else if (volume21 > 0 & volume22 > 0 & speed21 > 0 & speed22 > 0 & speed21 < maxSpeed & speed22 < maxSpeed & valid == true)
                                    {
                                        volume2 = volume21 + volume22;
                                        speed2 = (speed21 * volume21 + speed22 * volume22) / (volume21 + volume22);
                                    }
                                    else
                                    {
                                        valid = false;
                                    }
                                }
                                else
                                {
                                    valid = false;
                                }
                                */
                                //Console.WriteLine("StationPair"+j+" Time"+i+"Volume"+volume11+" "+volume12+" "+volume21+" "+volume22+" Speed"+speed11+" "+speed12+" "+speed21+ " " + speed22);
                                volume = volume11 + volume12 + volume21 + volume22;
                                if (volume > 0)
                                {
                                    speed = (speed11 * volume11 + speed12 * volume12 + speed21 * volume21 + speed22 * volume22) / volume;
                                }
                                else
                                {
                                    speed = maxSpeed;
                                }
                                //Console.WriteLine("Volume" + volume + " Speed" + speed);
                            }
                            else
                            {
                                volume = 0;
                                speed = maxSpeed;
                                valid = false;
                            }

                            if (valid == true)
                            {
                                //volume = volume1 + volume2;
                                //speed = (speed1 * volume1 + speed2 * volume2) / (volume1 + volume2);
                                //Do we need to check if it is valid?; ********************************************
                                for (int k = 0; k < numCategories + 1; k++)
                                {
                                    //Applied the linear model;
                                    UFPLevel[k][j][i] = ModelParameter[k][3] + speed * ModelParameter[k][4] + volume * ModelParameter[k][5];
                                    //Output from this linear model is the logrithm of particle number concentration PNC;
                                    // log(PNC) = constant + a(Speed) + b(Volume);
                                    CumulativeUFP[m][k][j] = CumulativeUFP[m][k][j] + Convert.ToSingle(Math.Pow(10,UFPLevel[k][j][i])) * volume;
                                }
                                modelVolume[j][i] = volume;
                                modelSpeed[j][i] = speed;
                                //Add the number of valid cells;
                                //checkVolumeSpeedCombination(volume, speed, true);
                            }
                            else
                            {
                                for (int k = 0; k < numCategories + 1; k++)
                                {
                                    UFPLevel[k][j][i] = -1;
                                }
                                modelVolume[j][i] = -1;
                                modelSpeed[j][i] = -1;
                                //Add the number of valid cells;
                                //checkVolumeSpeedCombination(volume, speed, false);
                            }
                        }
                    }
                }
                //********************************************************************************************************
                Boolean Healthy = DetectorHealthCheck(currentDate, m);
                if (Healthy == true) 
                {
                    //Add the number of valid cells
                    for (int i = 0; i < numMinutes; i++)
                    {
                        for (int j = 0; j < numDetPair; j++)
                        {
                            checkVolumeSpeedCombination(modelVolume[j][i],modelSpeed[j][i]);
                        }
                    }
                    
                    //exportUPFLevl(currentDate, m, s_exportdir);
                    //More than 400 detectors are healthy for more than 3/4 of the time;
                    //Export results;
                    for (int i = 0; i < numMinutes; i++)
                    {
                        Exp_Time_Output = modelYear + "," + modelMonth + "," + modelDay + "," + dayofweek+","+i;
                        for (int j = 0; j < numDetPair; j++)
                        {
                            if (UFPLevel[0][j][i] > 0)
                            {
                                Exp_Time_Output = Exp_Time_Output + "," + UFPLevel[0][j][i];
                                validDays[i][j]++;
                                nDayCore[i][j] = nDayCore[i][j] + UFPLevel[0][j][i];
                            }
                            else
                            {
                                Exp_Time_Output = Exp_Time_Output + ",";
                            }
                        }
                        tw1.WriteLine(Exp_Time_Output);
                    }
                    //Construct export for core, density
                    for (int j = 0; j < numDetPair; j++)
                    {
                        //24 hour average core vs exposure;
                        float totalCore = 0;
                        float totalDensity = 0;
                        int countCore = 0;
                        int counterDensity = 0;
                        int staID1 = detPairID[j][1];
                        int staID2 = detPairID[j][2];
                        int numLanes1 = StationDetNum[staID1];
                        int numLanes2 = StationDetNum[staID2];
                        for (int i = 0; i < numMinutes; i++)
                        {
                            if (UFPLevel[0][j][i] > 0)
                            {
                                totalCore = totalCore + UFPLevel[0][j][i];
                                countCore++;
                            }
                            if (UFPLevel[0][j][i] > 0 & numLanes1 >0 & numLanes2 >0)
                            {
                                totalDensity = totalDensity + modelVolume[j][i]*60/(numLanes1 + numLanes2)/modelSpeed[j][i];
                                counterDensity++;
                            }
                        }
                        if (countCore > 0)
                        {
                            Exp_Core_Output = modelYear + "," + modelMonth + "," + modelDay + "," + j + "," + detPairID[j][1] + "," + detPairID[j][2] + "," + (totalCore / countCore) + "," + CumulativeUFP[m][0][j];
                            }
                        else 
                        {
                            Exp_Core_Output = modelYear + "," + modelMonth + "," + modelDay + "," + j + "," + detPairID[j][1] + "," + detPairID[j][2] + "," + -1 + "," + CumulativeUFP[m][0][j];
                        }
                        if (counterDensity > 0)
                        {
                            Exp_Density_Output = modelYear + "," + modelMonth + "," + modelDay + "," + j + "," + detPairID[j][1] + "," + detPairID[j][2] + "," + (totalDensity / counterDensity) + "," + CumulativeUFP[m][0][j];
                        }
                        else
                        {
                            Exp_Density_Output = modelYear + "," + modelMonth + "," + modelDay + "," + j + "," + detPairID[j][1] + "," + detPairID[j][2] + "," + -1 + "," + CumulativeUFP[m][0][j];
                        }
                        tw2.WriteLine(Exp_Core_Output);
                        tw3.WriteLine(Exp_Core_Output);
                    }
                    
                    //ConstructBinTotal;
                    for (int i = 0; i < numMinutes; i++)
                    {
                        Exp_Bin_Time_Output = modelYear + "," + modelMonth + "," + modelDay + "," + dayofweek + "," + i;
                        for (int j = 0; j<numCategories+1;j++)
                        {
                            float binCoreTotal = 0;
                            for (int k = 0; k < numDetPair; k++)
                            {
                                if (CumulativeUFP[m][j][k] > 0)
                                {
                                    binCoreTotal = binCoreTotal + CumulativeUFP[m][j][k];
                                }
                            }
                            Exp_Bin_Time_Output = Exp_Bin_Time_Output + "," + binCoreTotal;
                        }
                        tw4.WriteLine(Exp_Bin_Time_Output);
                    }
                    //Construct average particle for each minute;
                    for (int i = 0; i < numMinutes; i++)
                    {
                        for (int j = 0; j < numDetPair; j++)
                        {
                            if (dayofweek >= 1 & dayofweek <= 5)
                            {
                                if (UFPLevel[0][j][i] > 0)
                                {
                                    weekdayAvePNDet[i][j] = weekdayAvePNDet[i][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i]));
                                    weekdayAveTotalPNDet[i][j] = weekdayAveTotalPNDet[i][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                    weekdayAveNumDays[i][j]++;
                                }
                            }
                            else 
                            {
                                if (UFPLevel[0][j][i] > 0)
                                {
                                    weekendAvePNDet[i][j] = weekendAvePNDet[i][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i]));
                                    weekendAveTotalPNDet[i][j] = weekendAveTotalPNDet[i][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                    weekendAveNumDays[i][j]++;
                                }
                            }
                            //Daily total vs Daily average;
                            if (UFPLevel[0][j][i] > 0)
                            {
                                dailyTotalPNDet[validDaysCounter][j] = dailyTotalPNDet[validDaysCounter][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                dailyWeightedAVPNDet[validDaysCounter][j] = dailyWeightedAVPNDet[validDaysCounter][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                dailyTotalVolume[validDaysCounter][j] = dailyTotalVolume[validDaysCounter][j] + modelVolume[j][i];
                                dailyAvePNDet[validDaysCounter][j] = dailyAvePNDet[validDaysCounter][j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i]));
                                dailyTotalNumMinutes[validDaysCounter][j]++;
                            }
                            //44Day Exposure vs Concentration;
                            if (UFPLevel[0][j][i] > 0)
                            {
                                averageDailyTotalPNDet[j] = averageDailyTotalPNDet[j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                averageDailyWeightedAVPNDet[j] = averageDailyWeightedAVPNDet[j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i])) * modelVolume[j][i];
                                averageDailyAvePNDet[j] = averageDailyAvePNDet[j] + Convert.ToSingle(Math.Pow(10, UFPLevel[0][j][i]));
                                totalVolume[j] = totalVolume[j] + modelVolume[j][i];
                                totalMinutes[j]++;
                            }
                        }
                    }
                    validDaysCounter++;
                    //construct weekday/weekend bin total and average;
                    if (dayofweek >= 1 & dayofweek <= 5)
                    {
                        numValidWeekday++;
                        for (int j = 0; j < numCategories+1; j++)
                        {
                            int position;
                            for (int n = 0; n < numBinTimePoint; n++)
                            {
                                position = (n * 1 + 8) * 60;
                                for (int k = 0; k < numDetPair; k++)
                                {
                                    if (UFPLevel[j][k][position] > 0)
                                    {
                                        BinDailyTotalWD[j][n] = BinDailyTotalWD[j][n] + Convert.ToSingle(Math.Pow(10,UFPLevel[j][k][position])) * modelVolume[k][position];
                                        binTotalVolumeWD[j][n] = binTotalVolumeWD[j][n] + modelVolume[k][position];
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        numValidWeekend++;
                        for (int j = 0; j < numCategories+1; j++)
                        {
                            int position;
                            for (int n = 0; n < numBinTimePoint; n++)
                            {
                                position = (n * 1 + 8) * 60;
                                for (int k = 0; k < numDetPair; k++)
                                {
                                    if (UFPLevel[j][k][position] > 0)
                                    {
                                        BinDailyTotalWK[j][n] = BinDailyTotalWK[j][n] + Convert.ToSingle(Math.Pow(10,UFPLevel[j][k][position])) * modelVolume[k][position];
                                        binTotalVolumeWK[j][n] = binTotalVolumeWK[j][n] + modelVolume[k][position];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            for (int j = 0; j < numCategories+1; j++)
            {
                for (int n = 0; n < numBinTimePoint; n++) 
                {
                    if (binTotalVolumeWD[j][n] > 0)
                    {
                        BinDailyWeightAverageWD[j][n] = BinDailyTotalWD[j][n] / binTotalVolumeWD[j][n];
                    }
                    else 
                    {
                        BinDailyWeightAverageWD[j][n] = -1;
                    }
                    if (binTotalVolumeWK[j][n] > 0)
                    {
                        BinDailyWeightAverageWK[j][n] = BinDailyTotalWK[j][n] / binTotalVolumeWK[j][n];
                    }
                    else
                    {
                        BinDailyWeightAverageWK[j][n] = -1;
                    }
                    if (numValidWeekday > 0)
                    {
                        BinDailyTotalWD[j][n] = BinDailyTotalWD[j][n] / numValidWeekday;
                    }
                    if (numValidWeekend > 0) 
                    {
                        BinDailyTotalWK[j][n] = BinDailyTotalWK[j][n] / numValidWeekend;
                    }
                }
            }
            //string exportFile5 = s_exportdir + "Exposure_Average_Core_Output.csv";
            //TextWriter tw5 = new StreamWriter(exportFile5);
            //for (int i = 0; i < 24 * 60; i++)
            //{
            //    for (int j = 0; j < numDetPair; j++)
            //    {
            //        if (validDays[i][j] > 0)
            //        {
            //            nDayCore[i][j] = nDayCore[i][j] / validDays[i][j];
            //        }
            //    }
            //}
            for (int i = 0; i < numMinutes; i++)
            {
                for (int j = 0; j < numDetPair; j++)
                {
                    if (weekdayAveNumDays[i][j] > 0)
                    {
                        weekdayAvePNDet[i][j] = weekdayAvePNDet[i][j] / weekdayAveNumDays[i][j];
                        weekdayAveTotalPNDet[i][j] = weekdayAveTotalPNDet[i][j] / weekdayAveNumDays[i][j];
                    }
                    else 
                    {
                        weekdayAvePNDet[i][j] = 0;
                        weekdayAveTotalPNDet[i][j] = 0;
                    }
                    if (weekendAveNumDays[i][j] > 0)
                    {
                        weekendAvePNDet[i][j] = weekendAvePNDet[i][j] / weekendAveNumDays[i][j];
                        weekendAveTotalPNDet[i][j] = weekendAveTotalPNDet[i][j] / weekendAveNumDays[i][j];
                    }
                    else 
                    {
                        weekendAvePNDet[i][j] = 0;
                        weekendAveTotalPNDet[i][j] = 0;
                    }
                }
            }
            string exportFile5 = s_exportdir + "Exposure_Average_Core_Weekday.csv";
            TextWriter tw5 = new StreamWriter(exportFile5);
            String Output_String;
            Output_String = "Minute";
            for (int i = 0; i < numDetPair; i++)
            {
                Output_String = Output_String + ",StaInd" + (i + 1);
            }
            tw5.WriteLine(Output_String);
            for (int i = 0; i < numMinutes; i++)
            {
                Output_String = "" + i;
                for (int j = 0; j < numDetPair; j++)
                {
                    Output_String = Output_String + "," + weekdayAvePNDet[i][j];
                }
                tw5.WriteLine(Output_String);
            }
            tw5.Close();

            string exportFile6 = s_exportdir + "Exposure_Average_Core_Weekend.csv";
            TextWriter tw6 = new StreamWriter(exportFile6);
            Output_String = "Minute";
            for (int i = 0; i < numDetPair; i++)
            {
                Output_String = Output_String + ",StaInd" + (i + 1);
            }
            tw6.WriteLine(Output_String);
            for (int i = 0; i < numMinutes; i++)
            {
                Output_String = "" + i;
                for (int j = 0; j < numDetPair; j++)
                {
                    Output_String = Output_String + "," + weekendAvePNDet[i][j];
                }
                tw6.WriteLine(Output_String);
            }
            tw6.Close();

            string exportFile7 = s_exportdir + "Exposure_Average_Core_WeekdayTotal.csv";
            TextWriter tw7 = new StreamWriter(exportFile7);
            Output_String = "Minute";
            for (int i = 0; i < numDetPair; i++)
            {
                Output_String = Output_String + ",StaInd" + (i + 1);
            }
            tw7.WriteLine(Output_String);
            for (int i = 0; i < numMinutes; i++)
            {
                Output_String = "" + i;
                for (int j = 0; j < numDetPair; j++)
                {
                    Output_String = Output_String + "," + weekdayAveTotalPNDet[i][j];
                }
                tw7.WriteLine(Output_String);
            }
            tw7.Close();

            string exportFile8 = s_exportdir + "Exposure_Average_Core_WeekendTotal.csv";
            TextWriter tw8 = new StreamWriter(exportFile8);
            Output_String = "Minute";
            for (int i = 0; i < numDetPair; i++)
            {
                Output_String = Output_String + ",StaInd" + (i + 1);
            }
            tw8.WriteLine(Output_String);
            for (int i = 0; i < numMinutes; i++)
            {
                Output_String = "" + i;
                for (int j = 0; j < numDetPair; j++)
                {
                    Output_String = Output_String + "," + weekendAveTotalPNDet[i][j];
                }
                tw8.WriteLine(Output_String);
            }
            tw8.Close();

            //Daily total vs Daily average;
            for (int i = 0; i < periods; i++)
            {
                for (int j = 0; j < numDetPair; j++)
                {
                    if (dailyTotalVolume[i][j] != 0)
                    {
                        dailyWeightedAVPNDet[i][j] = dailyWeightedAVPNDet[i][j] / dailyTotalVolume[i][j];
                    }
                    else 
                    {
                        dailyWeightedAVPNDet[i][j] = 0;
                    }
                    if (dailyTotalNumMinutes[i][j] != 0)
                    {
                        dailyAvePNDet[i][j] = dailyAvePNDet[i][j] / dailyTotalNumMinutes[i][j];
                    }
                    else 
                    {
                        dailyAvePNDet[i][j] = 0;
                    }
                }
            }
            string exportFile9 = s_exportdir + "Exposure_vs_Concentration.csv";
            TextWriter tw9 = new StreamWriter(exportFile9);
            Output_String = "StationIndex,DailyTTExp,AveConc,WgAveConc";
            tw9.WriteLine(Output_String);
            for (int i = 0; i < periods; i++)
            {
                for (int j = 0; j < numDetPair; j++)
                {
                    Output_String = ""+j+","+dailyTotalPNDet[i][j]+","+dailyAvePNDet[i][j]+"," +dailyWeightedAVPNDet[i][j];
                    tw9.WriteLine(Output_String);
                }     
            }
            tw9.Close();

            //Average daily
            for (int i = 0; i < numDetPair; i++)
            {
                if (totalVolume[i] != 0)
                {
                    averageDailyWeightedAVPNDet[i] = averageDailyWeightedAVPNDet[i] / totalVolume[i];
                }
                else 
                {
                    averageDailyWeightedAVPNDet[i] = 0;
                }
                if (totalMinutes[i] != 0)
                {
                    averageDailyAvePNDet[i] = averageDailyAvePNDet[i] / totalMinutes[i];
                }
                else 
                {
                    averageDailyAvePNDet[i] = 0;
                }
                averageDailyTotalPNDet[i] = averageDailyTotalPNDet[i] / periods;
            }
            string exportFile10 = s_exportdir + "Exposure_vs_Concentration_44DayAverage.csv";
            TextWriter tw10 = new StreamWriter(exportFile10);
            Output_String = "StationIndex,DailyTTExp,AveConc,WgAveConc";
            tw10.WriteLine(Output_String);
            for (int i = 0; i < numDetPair; i++)
            {
                Output_String = "" + i + "," + averageDailyTotalPNDet[i] + "," + averageDailyAvePNDet[i] + "," + averageDailyWeightedAVPNDet[i];
                tw10.WriteLine(Output_String);
            }
            tw10.Close();
            //
            tw1.Close();
            tw2.Close();
            tw3.Close();
            tw4.Close();
        }
        #endregion

        public Boolean DetectorHealthCheck(DateTime currentDate, int currentCalanderDay)
        {
            //If a detector is healthy 75% of time, then it should function well;
            Boolean Healthy;
            int numHealthyDetector = 0;
            int modelYear;
            int modelMonth;
            int modelDay;

            int[] numHealthyMinutes = new int[numDetPair];
            for (int i = 0; i < numDetPair; i++)
            {
                numHealthyMinutes[i] = 0;
            }

            modelYear = currentDate.Year;
            modelMonth = currentDate.Month;
            modelDay = currentDate.Day;

            int numMinutes = 24 * 60;
            for (int j = 0; j < numMinutes; j++)
            {
                for (int k = 0; k < numDetPair; k++)
                {
                    //Only check the aggregate model, which should be consistent with all other 32 bin models;
                    if (UFPLevel[0][k][j] > 0)
                    {
                        numHealthyMinutes[k]++;
                    }
                }
            }
            //If a detector is healthy for 75% of the time, then we think it is good;
            //Otherwise, erase all the export;
            for (int j = 0; j < numDetPair; j++)
            {
                if (numHealthyMinutes[j] < (24 * 60 * 3 / 4))
                {
                    for (int k = 0; k < numCategories + 1; k++)
                    {
                        //Category k,detector pair j;
                        CumulativeUFP[currentCalanderDay][k][j] = 0;
                        for (int m = 0; m < numMinutes; m++)
                        {
                            UFPLevel[k][j][m] = 0;
                        }
                    }
                }
                else
                {
                    numHealthyDetector++;
                }
            }
            if (numHealthyDetector >= 350)
            {
                //If more than 400 detector pairs are healthy;
                Healthy = true;
            }
            else
            {
                Healthy = false;
            }
            return (Healthy);
        }

        #region ExportFile
        public void exportStatisticalFile(string s_exportdir)
        {
            Console.WriteLine("Start to export Statistical Files, one category per file");
            string exportFile;
            TextWriter tw;
            string outportline;
            /*
            exportFile = s_exportdir + "Exposure_Time_Output.csv";
            tw = new StreamWriter(exportFile);
            tw.WriteLine(Exp_Time_Output);
            tw.Close();

            exportFile = s_exportdir + "Exposure_Density_Output.csv";
            tw = new StreamWriter(exportFile);
            tw.WriteLine(Exp_Density_Output);
            tw.Close();

            exportFile = s_exportdir + "Exposure_Core_Output.csv";
            tw = new StreamWriter(exportFile);
            tw.WriteLine(Exp_Core_Output);
            tw.Close();

            exportFile = s_exportdir + "Exposure_Bin_Time_Output.csv";
            tw = new StreamWriter(exportFile);
            tw.WriteLine(Exp_Bin_Time_Output);
            tw.Close();
            */

            //Export Bin Total And Bin Mean
            exportFile = s_exportdir + "WeekDay_Bin_Total.csv";
            tw = new StreamWriter(exportFile);
            outportline = "Bin,8am;11am;2pm;5pm;8pm";
            tw.WriteLine(outportline);
            for (int i = 0; i < numCategories+1; i++)
            {
                outportline = "Bin" + i;
                for (int j = 0; j < numBinTimePoint; j++)
                {
                    outportline = outportline +"," + BinDailyTotalWD[i][j];
                }
                tw.WriteLine(outportline);
            }
            tw.Close();

            exportFile = s_exportdir + "WeekEnd_Bin_Total.csv";
            tw = new StreamWriter(exportFile);
            outportline = "Bin,8am;11am;2pm;5pm;8pm";
            tw.WriteLine(outportline);
            for (int i = 0; i < numCategories + 1; i++)
            {
                outportline = "Bin" + i;
                for (int j = 0; j < numBinTimePoint; j++)
                {
                    outportline = outportline + "," + BinDailyTotalWK[i][j];
                }
                tw.WriteLine(outportline);
            }
            tw.Close();

            exportFile = s_exportdir + "WeekDay_Bin_Weighted_Average.csv";
            tw = new StreamWriter(exportFile);
            outportline = "Bin,8am;11am;2pm;5pm;8pm";
            tw.WriteLine(outportline);
            for (int i = 0; i < numCategories + 1; i++)
            {
                outportline = "Bin" + i;
                for (int j = 0; j < numBinTimePoint; j++)
                {
                    outportline = outportline + "," + BinDailyWeightAverageWD[i][j];
                }
                tw.WriteLine(outportline);
            }
            tw.Close();

            exportFile = s_exportdir + "WeekEnd_Bin_Weighted_Average.csv";
            tw = new StreamWriter(exportFile);
            outportline = "Bin,8am;11am;2pm;5pm;8pm";
            tw.WriteLine(outportline);
            for (int i = 0; i < numCategories + 1; i++)
            {
                outportline = "Bin" + i;
                for (int j = 0; j < numBinTimePoint; j++)
                {
                    outportline = outportline + "," + BinDailyWeightAverageWK[i][j];
                }
                tw.WriteLine(outportline);
            }
            tw.Close();
        }

        public void exportUPFLevl(DateTime currentDate, int currentCalanderDay, string s_exportdir)
        {
            Console.WriteLine("Start to export UFP level, one category per file");
            int modelYear;
            int modelMonth;
            int modelDay;
            modelYear = currentDate.Year;
            modelMonth = currentDate.Month;
            modelDay = currentDate.Day;

            int numMinutes = 24 * 60;

            string exportFile;
            TextWriter tw;
            string outportline;
            //Export 32 bin files plus one aggregate exposure file;

            //for (int i = 0; i < numCategories + 1; i++)

            for (int i = 0; i < 1; i++)
            {
                exportFile = s_exportdir + "UFPBin" + i + "_" + (modelYear * 10000 + modelMonth * 100 + modelDay) + ".csv";
                tw = new StreamWriter(exportFile);
                outportline = "DetPair,ASta,BSta";
                for (int j = 0; j < numMinutes; j++)
                {
                    outportline = outportline + "," + j;
                }
                tw.WriteLine(outportline);
                for (int j = 0; j < numDetPair; j++)
                {
                    outportline = "" + detPairID[j][0] + "," + detPairID[j][1] + "," + detPairID[j][2];
                    for (int k = 0; k < numMinutes; k++)
                    {
                        outportline = outportline + "," + UFPLevel[i][j][k];
                    }
                    tw.WriteLine(outportline);
                }
                tw.Close();
            }

            exportFile = s_exportdir + "CumulativeUFPBin"+ "_" + (modelYear * 10000 + modelMonth * 100 + modelDay)+ ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "DetPair,ASta,BSta,Total";
            for (int i = 1; i < numCategories + 1; i++)
            {
                outportline = outportline + ",bin" + i;
            }
            tw.WriteLine(outportline);
            for (int i = 0; i < numDetPair; i++)
            {
                outportline = "" + detPairID[i][0] + "," + detPairID[i][1] + "," + detPairID[i][2];
                for (int j = 0; j < numCategories + 1;j++ )
                {
                    outportline = outportline + "," + CumulativeUFP[currentCalanderDay][j][i];
                }
                tw.WriteLine(outportline);
            }
            tw.Close();

            exportFile = s_exportdir + "ModelVolume"+ "_" + (modelYear * 10000 + modelMonth * 100 + modelDay)+ ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "DetPair,ASta,BSta,Volume";
            tw.WriteLine(outportline);
            for (int i = 0; i < numDetPair; i++)
            {
                 outportline = detPairID[i][0] + "," + detPairID[i][1] + "," + detPairID[i][2];
                 for (int j=0;j<numMinutes;j++)
                 {
                     outportline = outportline + "," + modelVolume[i][j];
                 }
                 tw.WriteLine(outportline);
            }
            tw.Close();

            exportFile = s_exportdir + "ModelSpeed"+ "_" + (modelYear * 10000 + modelMonth * 100 + modelDay)+ ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "DetPair,ASta,BSta,Speed";
            tw.WriteLine(outportline);
            for (int i = 0; i < numDetPair; i++)
            {
                 outportline = detPairID[i][0] + "," + detPairID[i][1] + "," + detPairID[i][2];
                 for (int j=0;j<numMinutes;j++)
                 {
                     outportline = outportline + ","+modelSpeed[i][j];
                 }
                 tw.WriteLine(outportline);
            }
            tw.Close();
        }
        #endregion

        public void ExportWeatherCondition(DateTime startDate, DateTime endDate, string s_exportdir)
        {
            Console.WriteLine("Export weather conditions of the studying time period");
            int modelYear;
            int modelMonth;
            int modelDay;
            TimeSpan span = endDate.Subtract(startDate);
            int numCalendarDays = span.Days + 1;

            int[] numMinutesValid;
            numMinutesValid = new int[numCalendarDays];
            for (int i = 0; i < numCalendarDays; i++)
            {
                numMinutesValid[i] = 0;
            }

            for (int i = 0; i < numCalendarDays; i++)
            {
                DateTime currentDate = startDate.AddDays(i);
                modelYear = currentDate.Year;
                modelMonth = currentDate.Month;
                modelDay = currentDate.Day;

                int numMinutes = 24 * 60;
                for (int j = 0; j < numMinutes; j++)
                {
                    if (weatherCondition[modelMonth][modelDay][j] == 1)
                    {
                        numMinutesValid[i]++;
                    }
                }
            }

            string exportFile;
            TextWriter tw;
            string outportline;
            exportFile = s_exportdir + "WeatherValid" + ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "Month,Day,HoursValid";
            tw.WriteLine(outportline);

            for (int i = 0; i < numCalendarDays; i++)
            {
                DateTime currentDate = startDate.AddDays(i);
                modelYear = currentDate.Year;
                modelMonth = currentDate.Month;
                modelDay = currentDate.Day;
                float validHours = numMinutesValid[i];

                outportline = "" + modelMonth + "," + modelDay + "," + validHours;
                tw.WriteLine(outportline);
            }
            tw.Close();
        }

        public void ExportNumValidDetector(DateTime currentDate, string s_exportdir)
        {
            Console.WriteLine("Export weather conditions of the studying time period");
            int modelYear;
            int modelMonth;
            int modelDay;

            int numRows = 24 * 60;

            int[] numValidDetecotr = new int[numRows];
            for (int i = 0; i < numRows; i++)
            {
                numValidDetecotr[i] = 0;
            }

            modelYear = currentDate.Year;
            modelMonth = currentDate.Month;
            modelDay = currentDate.Day;

            int numMinutes = 24 * 60;
            for (int j = 0; j < numMinutes; j++)
            {
                for (int k = 0;k< numDetPair;k++)
                {
                    //Only check the aggregate model, which should be consistent with all other 32 bin models;
                    if (UFPLevel[0][k][j] >0)
                    {
                        numValidDetecotr[j]++;
                    }
                }
            }


            string exportFile;
            TextWriter tw;
            string outportline;
            exportFile = s_exportdir + "numDetectorValid" + (modelYear * 10000 + modelMonth *100 + modelDay) + ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "Month,Day,Minutes,numValidDet";
            tw.WriteLine(outportline);

            for (int j = 0; j < numMinutes; j++)
            {
                outportline = "" + modelMonth +"," + modelDay + "," + numValidDetecotr[j];
                tw.WriteLine(outportline);
            }
            tw.Close();
        }

        public void ExportNumValidDetector(DateTime startDate, DateTime endDate, string s_exportdir)
        {
            Console.WriteLine("Export weather conditions of the studying time period");
            int modelYear;
            int modelMonth;
            int modelDay;
            TimeSpan span = endDate.Subtract(startDate);
            int numCalendarDays = span.Days + 1;

            int[] numValidDetecotr = new int[numCalendarDays];
            for (int i = 0; i < numCalendarDays; i++)
            {
                numValidDetecotr[i] = 0;
            }
            for (int i = 0; i < numCalendarDays; i++)
            {
                DateTime currentDate = startDate.AddDays(i);
                modelYear = currentDate.Year;
                modelMonth = currentDate.Month;
                modelDay = currentDate.Day;

                //Only check the first of accumulative is enough;
                for (int k = 0; k < numDetPair; k++)
                {
                    //Only check the aggregate model, which should be consistent with all other 32 bin models;
                    if (CumulativeUFP[i][0][k] > 0)
                    {
                        numValidDetecotr[i]++;
                    }
                }
               
            }

            string exportFile;
            TextWriter tw;
            string outportline;
            exportFile = s_exportdir + "numDetectorValid" + ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "Month,Day,Minutes,numValidDet";
            tw.WriteLine(outportline);

            for (int i = 0; i < numCalendarDays; i++)
            {
                DateTime currentDate = startDate.AddDays(i);
                modelYear = currentDate.Year;
                modelMonth = currentDate.Month;
                modelDay = currentDate.Day;

                outportline = "" + modelMonth + "," + modelDay + "," + numValidDetecotr[i];
                tw.WriteLine(outportline);
            }
            tw.Close();
        }
        
        public void LoadDetectorInfo(string s_StationDecList)
        {
            StationDetNum = new int[maxStaID];
            for (int i = 0; i < maxStaID; i++)
            {
                StationDetNum[i] = -1;
            }
            string[] s_contents = File.ReadAllLines(s_StationDecList);
            int numRows = s_contents.Length;
            for (int i = 1; i < numRows; i++)
            {
                string[] s_split = s_contents[i].Split(',');
                int staID = Convert.ToInt32(s_split[0]);
                StationDetNum[staID] = Convert.ToInt32(s_split[1]);
            }
        }

        public void checkVolumeSpeedCombination(float volume, float speed, Boolean valid)
        {
            int columnID = 0;
            int rowID = 0;
            if (valid == false)
            {
                nonValidNumMinutes++;
            }
            else 
            {
                //Check Volume;
                if (volume <= 40)
                {
                    rowID = 0;
                }
                else if (volume <= 50)
                {
                    rowID = 1;
                }
                else if (volume <= 60)
                {
                    rowID = 2;
                }
                else if (volume <= 70)
                {
                    rowID = 3;
                }
                else if (volume <= 80)
                {
                    rowID = 4;
                }
                else 
                {
                    rowID = 5;
                }

                //Check Speed;
                if (speed < 15)
                {
                    columnID = 0;
                }
                else if (speed < 40)
                {
                    columnID = 1;
                }
                else if (speed < 50)
                {
                    columnID = 2;
                }
                else if (speed < 54)
                {
                    columnID = 3;
                }
                else if (speed < 58)
                {
                    columnID = 4;
                }
                else if (speed < 62)
                {
                    columnID = 5;
                }
                else if (speed < 65)
                {
                    columnID = 6;
                }
                else 
                {
                    speed = 7;
                }
                validNumMinutes[rowID][columnID]++;
                validNumVolume[rowID][columnID] = validNumVolume[rowID][columnID] + Convert.ToInt32(volume);
            }
        }
        public void checkVolumeSpeedCombination(float volume, float speed)
        {
            int columnID = 0;
            int rowID = 0;
            if (volume<0 | speed<0 | speed > maxSpeed)
            {
                nonValidNumMinutes++;
            }
            else
            {
                //Check Volume;
                if (volume == 0)
                {
                    rowID = 6;
                }
                else if (volume <= 40)
                {
                    rowID = 0;
                }
                else if (volume <= 50)
                {
                    rowID = 1;
                }
                else if (volume <= 60)
                {
                    rowID = 2;
                }
                else if (volume <= 70)
                {
                    rowID = 3;
                }
                else if (volume <= 80)
                {
                    rowID = 4;
                }
                else
                {
                    rowID = 5;
                }

                //Check Speed;
                if (speed < 15)
                {
                    columnID = 0;
                }
                else if (speed < 40)
                {
                    columnID = 1;
                }
                else if (speed < 50)
                {
                    columnID = 2;
                }
                else if (speed < 54)
                {
                    columnID = 3;
                }
                else if (speed < 58)
                {
                    columnID = 4;
                }
                else if (speed < 62)
                {
                    columnID = 5;
                }
                else if (speed < 65)
                {
                    columnID = 6;
                }
                else
                {
                    columnID = 7;
                }
                validNumMinutes[rowID][columnID]++;
                validNumVolume[rowID][columnID] = validNumVolume[rowID][columnID] + Convert.ToInt32(volume);
            }
        }

        public void exportValidNumCells(string s_exportdir)
        {
            string exportFile;
            TextWriter tw;
            string outportline;
            exportFile = s_exportdir + "numValidCells" + ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "<15,15-40,40-50,50-54,54-58,58-62,62-65,>65";
            tw.WriteLine(outportline);
            for (int i = 0; i < 7; i++)
            {
                outportline = "";
                for (int j = 0; j < 8; j++)
                {
                    outportline = outportline + validNumMinutes[i][j] + ",";
                }
                tw.WriteLine(outportline);
            }
            outportline = "" + nonValidNumMinutes;
            tw.WriteLine(outportline);
            tw.Close();

            exportFile = s_exportdir + "numValidVolume" + ".csv";
            tw = new StreamWriter(exportFile);
            outportline = "<15,15-40,40-50,50-54,54-58,58-62,62-65,>65";
            tw.WriteLine(outportline);
            for (int i = 0; i < 7; i++)
            {
                outportline = "";
                for (int j = 0; j < 8; j++)
                {
                    outportline = outportline + validNumVolume[i][j] + ",";
                }
                tw.WriteLine(outportline);
            }
            tw.Close();
        }
    }
}
