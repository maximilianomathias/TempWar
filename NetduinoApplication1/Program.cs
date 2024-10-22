﻿using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using NetduinoController.Web;
using NETDuinoWar;
using Netduino.Foundation.Displays.MicroLiquidCrystal;


namespace NetduinoController
{
    public class Program {

        // Los PINS a utilizar. 
        private static OutputPort Secador = new OutputPort(Pins.GPIO_PIN_D13, false);
        private static OutputPort Ventilador1 = new OutputPort(Pins.GPIO_PIN_D12, false);
        private static OutputPort Ventilador2 = new OutputPort(Pins.GPIO_PIN_D10, false); 
        private static OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
        private static OutputPort ledCool = new OutputPort(Pins.GPIO_PIN_A0, false);
        
        public static void Main() {
           
            off(); // primero nos aseguramos de que este todo apagado
            MyWebServer server = new MyWebServer();
            I2CDevice sensor = new I2CDevice(new I2CDevice.Configuration(0x48, 50));
           
            server.Start();// Lanzamos el servidor

            Thread LecturaTemperatura = new Thread(readTemp);
            LecturaTemperatura.Start();// mostamos el LCD los datos correspondientes
            
            TimeController timecontroller = new TimeController();
            bool configured;
            string errorMessage = null;
            configured =  timecontroller.Configure(Datos.rangos, 100000, 500, out errorMessage);
        }
        #region Comienzo de ronda
        /// <summary>
        /// Starts one round of the competition.
        /// </summary>
        public static void startRound() {

            Debug.Print("------------------------------------------NUEVA RONDA---------------------------------------------");
            
            Thread temporizador = new Thread(timer); // Empezar el temporizador de la nueva ronda
            Thread parpadeo = new Thread(blink);// Hace que el led parpadee cuando estemos en competicion
            Thread contadorPuntos = new Thread(pointCounter);

            temporizador.Start(); // cuando arranco el temporizador---> Datos.competi = true;
            parpadeo.Start();
            contadorPuntos.Start();
        }
        #endregion

        #region Temporizador
        /// <summary>
        /// funcion para determinar el tiempo restante de la ronda, Tambien se ecarga de terminar la partida en caso de obtener temperaturas muy elevadas.
        /// ---> En el IF STATEMENT podeis ver que hay una comprobacion de que si es inferior a los 50 grados. Esto es porque en reiteradas veces el sensor
        /// da unos picos muy elevados de temperaturas superiores a los 80 grados o incluso a los 4,000. Esto son errores de contacto en el cableado y por lo tanto 
        /// hemos tenido que corregirlo acotandolo a temperaturas menores de 50. 
        /// </summary>
        private static void timer() {
            Datos.competi = true;
            Datos.finishBattle = false;
            //Datos.timeLeft = Datos.roundTime; ----------------------------------------------------------------> esto estaba por defecto
            while (Datos.timeLeft > 0) {
                Datos.timeLeft--;
                Thread.Sleep(1000);
            }
            if (Datos.tempAct >= Datos.tempSafety && Datos.tempAct < 50)
            {
                Datos.timeLeft = 0;
                Datos.competi = false;
                Datos.error = true;
            }
            Datos.finishBattle = true; 
            Datos.competi = false;
        }
        #endregion

        #region Conteo de segundos en rango
        /// <summary>
        /// Funcion para acumular puntos dentro del rango.
        /// </summary>
        private static void pointCounter()
        {
            while (Datos.competi)
            {
                if ((Datos.tempAct <= Datos.tempMax) && (Datos.tempAct >= Datos.tempMin) && (Datos.roundTimeAux != 0) && (Datos.timeLeft != 0))
                {
                    Datos.timeInRangeTemp++;
                    Datos.roundTimeAux--;
                    Thread.Sleep(1000);
                }
                if (Datos.roundTimeAux == 0 && Datos.timeLeft != 0)
                {
                    Datos.competi = false;
                    Datos.finishBattle = true;
                }
            }
        }
        #endregion

        #region Parpadeo OnboardLed
        /// <summary>
        /// Funcion para indicar a través de un led que etamos en modo combate. 
        /// </summary>
        private static void blink() {
            while (Datos.competi) {
                led.Write(!led.Read());
                Thread.Sleep(500);
            }
            led.Write(false);
        }
        #endregion

        #region OFF
        /// <summary>
        /// Funcion para apagar lso ventiladores y elsecador
        /// </summary>
        public static void off()
        {
            Secador.Write(false);
            Ventilador1.Write(false);
            Ventilador2.Write(false);
        }
        #endregion

        #region Modo Enfriamiento
        /// <summary>
        /// Funcion Para mantener el sistema en 15 grados. Listo par ael combate.
        /// </summary> 
        public static void coolerMode()
        {
            Datos.coolerMode = true;
            while (!Datos.competi)
            {
                if (Datos.tempAct >= 18)
                {
                    Secador.Write(false);
                    Ventilador1.Write(true);
                    Ventilador2.Write(true);
                    Thread.Sleep(Datos.refresh);
                }
                if (Datos.tempAct < 15)
                {
                    Datos.coolerMode = false;
                    off();
                    break;
                }  
            }
        }
        #endregion

        #region Lectura de sensor, escritura en LCD y manejo de relays
        /// <summary>
        /// Funcion encargada de 3 cosas: Leer el sensor, mostrar datos por LCD y controlar el encendido y apagado de los relays.
        /// </summary>
        private static void readTemp() {

            OneWire _oneWire = new OneWire(new OutputPort(Pins.GPIO_PIN_D0, false));

            var lcdProvider = new GpioLcdTransferProvider(
            Pins.GPIO_PIN_D11,  // RS
            Pins.GPIO_NONE,     // RW
            Pins.GPIO_PIN_D9,  // enable
            Pins.GPIO_PIN_D2,   // d0
            Pins.GPIO_PIN_D4,   // d1
            Pins.GPIO_PIN_D6,   // d2
            Pins.GPIO_PIN_D8,   // d3
            Pins.GPIO_PIN_D7,   // d4
            Pins.GPIO_PIN_D5,   // d5
            Pins.GPIO_PIN_D3,   // d6
            Pins.GPIO_PIN_D1);  // d7

            var lcd = new Lcd(lcdProvider);

            lcd.Begin(16, 2);
             // apagamos todos los compomentes externos.
            // Infinite loop that reads the temp and stores it in tempAct
            while (true) {

                Double rango = Datos.tempMax - Datos.tempMin;
                Double limiteSup = 0.50 * rango;
                Double limiteInf = 0.20 * rango;

                try
                {
                    if (_oneWire.TouchReset() > 0)
                    {
                        _oneWire.WriteByte(0xCC);     // Skip ROM, only one device
                        _oneWire.WriteByte(0x44);     // Temp conversion

                        while (_oneWire.ReadByte() == 0) ;   //Loading

                        _oneWire.TouchReset();
                        _oneWire.WriteByte(0xCC);     // Skip ROM
                        _oneWire.WriteByte(0xBE);     // Read

                        ushort temperature = (byte)_oneWire.ReadByte();
                        temperature |= (ushort)(_oneWire.ReadByte() << 8); // MSB
                        Datos.tempAct = temperature / 16.0;



                        if (Datos.competi && !Datos.finishBattle)
                        {
                            //Debug.Print("------------------------------DENTRO DE PROGRAM.170-------------------");
                            // tanto el secador como el ventilador, operan en FALSE - circuito cerrado
                            if (Datos.tempAct >= (Datos.tempMax - limiteSup))      // FRIO
                            {
                                Secador.Write(false);
                                Ventilador1.Write(true);
                                Ventilador2.Write(true);
                                ledCool.Write(true);

                            }
                            else if (Datos.tempAct <= (Datos.tempMin + limiteInf)) // CALOR
                            {
                                Secador.Write(true);
                                Ventilador1.Write(false);
                                Ventilador2.Write(false);
                            }
                            else                                                   // APAGAMOS TODO
                                off();
                            

                            //Datos.tempAct = Microsoft.SPOT.Math.(Datos.tempAct, 1);
                            lcd.Clear();
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("[" + Datos.tempMin.ToString("N1") + "-" + Datos.tempMax.ToString("N1") + "]");

                            lcd.SetCursorPosition(12, 0);
                            lcd.Write(Datos.roundTime.ToString() + "s");

                            lcd.SetCursorPosition(0, 1);
                            lcd.Write(Datos.tempAct.ToString("N1") + "C [" + Datos.timeInRangeTemp.ToString() + "s" + "]");

                            lcd.SetCursorPosition(13, 1);
                            lcd.Write(Datos.timeLeft.ToString());

                            Thread.Sleep(Datos.refresh);
                        }
                        if (!Datos.competi && !Datos.coolerMode)
                        {
                            lcd.Clear();
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Temp War Grupo 1");
                            lcd.SetCursorPosition(0, 1);
                            lcd.Write("[" + Datos.tempAct.ToString("N1") + "C] Pts:" + Datos.timeInRangeTemp + "s");
                            Thread.Sleep(Datos.displayRefresh);
                        }
                        if(Datos.coolerMode)
                        {
                            lcd.Clear();
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Cooling Mode.");
                            lcd.SetCursorPosition(0, 1);
                            lcd.Write("Temp: " + Datos.tempAct.ToString("N1") + "C");
                            Thread.Sleep(Datos.displayRefresh);
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Cooling Mode..");
                            Thread.Sleep(Datos.displayRefresh);
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Cooling Mode...");
                        }
                    }
                    else
                    {
                        Debug.Print("Fallo de sensor");
                        //Could be that you read to fast after previous read. Include 
                        Thread.Sleep(1000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("ReadTemperatureToConsole " + ex.Message);
                }
            }
        }
        #endregion
    } // Program
} // namespace
