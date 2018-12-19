using System;
using Microsoft.SPOT;

namespace NetduinoController
{
    public class Datos {
        
        public static Double tempMax = 30; // In ºC
        public static Double tempMin = 25; // In ºC
        public static int displayRefresh = 500; // In ms
        public static int refresh = 250; // In ms
        public static int roundTime = 60; // in s

        public static bool competi = false;
        public static bool error = false;
        public static int rangeNumbers = 1; // por defecto siempre habra un rango de inicio 
        public static int timeLeft = 0; //In s
        public static int timeInRangeTemp = 0; //In ms.
        public static Double tempAct; // In C

    }
}
