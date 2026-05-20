using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//This program is designed for the estimation of Ultra-Fine Particle emissions;
//The input includes model parameters, the list of detector list, and the real time volume and speed data;
//The output is the UFP concentration level for each 1 minutes time period and for each detector pair;
//By Shanjiang Zhu March 2010;

namespace EmissionModel
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start of Ultra-Fine Particle emission estimation model, write by Shanjiang Zhu in March 2010");
            string s_detectorPairs = @"F:\Zhu_DiskF\2010Paper\EmissionModels\LoopStationIDPairs.csv";
            string s_modelParameters = @"F:\Zhu_DiskF\2010Paper\EmissionModels\ModelParameters.csv";
            string s_weatherCondition = @"F:\Zhu_DiskF\2010Paper\EmissionModels\WeatherCondition.csv";
            string s_StationDecList = @"F:\Zhu_DiskF\MnDOT_Data\Station_DetID_List.csv";

            EmissionModel em = new EmissionModel(s_detectorPairs, s_modelParameters, s_weatherCondition);
            em.LoadDetectorInfo(s_StationDecList);
            //string s_exportdir = @"F:\Zhu_DiskF\2010Paper\EmissionModels\ModelExportNew\";
            string s_exportdir = @"F:\Zhu_DiskF\2010Paper\EmissionModels\ModelExport0829\";
            string s_stationdir = @"F:\Zhu_DiskF\MnDOT_Data\Emission\";
            int modelYear = 2008;
            int monthStart = 6;
            int dayStart = 22;
            int monthEnd = 10;
            int dayEnd = 30;
            DateTime startDate = new DateTime(modelYear, monthStart, dayStart);
            DateTime endDate = new DateTime(modelYear, monthEnd, dayEnd);

            //em.ExportWeatherCondition(startDate, endDate, s_exportdir);
            em.EstimateModel(startDate, endDate, s_stationdir,s_exportdir);
            em.exportValidNumCells(s_exportdir);
            string k = "";
            em.exportStatisticalFile(s_exportdir);
            //em.exportUPFLevl(startDate, 0,s_exportdir);
            em.ExportNumValidDetector(startDate, endDate, s_exportdir);
        }
    }
}
