using System;
using NETDuinoWar; // libreria para TemperatureRange[]

namespace NetduinoController
{
    public class Datos {
        
        public static Double tempMax = 30; // In ºC | Temperatura maxima 
        public static Double tempMin = 25; // In ºC | Temperatura minima
        public static int displayRefresh = 500; // In ms | Refresco de la pantalla 
        public static int refresh = 250; // In ms | Cadencia de refresco interna
        public static int roundTime = 30; // in s  | Tiempo de ronda 
        public static int roundTimeAux = 0; // in s || Variable auxiliar que copia el valor de 'roundTime' para controlar el tiempo en el rango
        public static String roundQueue = ""; // se guarda la temperatura maxima, minima y el tiempo de la siguiente forma "xx-xx-xx/"

        public static TemperatureRange[] rangos; // aqui guardamos las diferentes rondas. 

        public static bool coolerMode = false; // Para indicar si estamos en modo enfriamiento
        public static bool competi = false; // Para indicar que estamoe n modo competicion
        public static bool error = false; // para indicar que hubo un error. p.e.: tempAct >= 40grados
        public static bool finishBattle = false; // Esto nos indica si hemos acabado la ronda antes de que acabe el tiempo global
        public static int rangeNumbers = 1; // por defecto siempre habra un rango de inicio 
        public static int timeLeft = 0; //In s || Para el tiempo global
        public static int timeInRangeTemp = 0; //In ms. || para contar el tiempo que etsamos dentro del rango
        public static Double tempAct; // In C || temperatura actual
        public static int tempSafety = 40; // Temperatura de seguridad. 

    }
}
