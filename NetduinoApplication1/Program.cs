using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
//using SecretLabs.NETMF.Hardware.OneWire;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using NetduinoController.Web;
using NETDuinoWar;
using Netduino.Foundation.Displays.MicroLiquidCrystal;


namespace NetduinoController
{
    public class Program {

        private static OutputPort Secador = new OutputPort(Pins.GPIO_PIN_D13, false);
        private static OutputPort Ventilador1 = new OutputPort(Pins.GPIO_PIN_D12, false);
        private static OutputPort Ventilador2 = new OutputPort(Pins.GPIO_PIN_D10, false); 
        private static OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);

       
        public static void Main() {
            // Create a WebServer
            MyWebServer server = new MyWebServer();
            I2CDevice sensor = new I2CDevice(new I2CDevice.Configuration(0x48, 50));

            server.Start();
  
            new Thread(readTemp).Start();
            
            TimeController timecontroller = new TimeController();
            bool configured;
            string errorMessage = null;
            // el segundo argumento tiene que ser la suma de los arrays ^^^^
            configured =  timecontroller.Configure(Datos.rangos, 100000, 500, out errorMessage);


            // Start a new thread to read the temperature
            

            // Thread showing display info
            //new Thread(displayTemp).Start();

            // Make sure Netduino keeps running.
            while (true) {
                Debug.Print("Netduino still running...");
                Thread.Sleep(10000);
            }

        }

        /// <summary>
        /// Starts one round of the competition.
        /// </summary>
        public static void startRound() {

            Debug.Print("------------------------------------------NUEVA RONDA---------------------------------------------");
            
            Thread temporizador = new Thread(timer); // Empezar el temporizador de la nueva ronda
            Thread parpadeo = new Thread(blink);// Hace que el led parpadee cuando estemos en competicion

            temporizador.Start();
            parpadeo.Start();

            while (Datos.competi)
            {
                Debug.Print("----------DENTRO DEL WHILE Program.78---------");
                if ((Datos.tempAct <= Datos.tempMax) && (Datos.tempAct >= Datos.tempMin) && (Datos.roundTimeAux != 0) && (Datos.timeLeft!=0))
                {
                    Datos.timeInRangeTemp++;
                    Datos.roundTimeAux--;
                    Debug.Print("--->tiempo restante para este rango: " + Datos.roundTimeAux);
                    Debug.Print(" DENTRO DEL RANGO");
                }
                // Wait for the refresh rate
                Thread.Sleep(1000);
            }

            if (Datos.finishBattle)
            {
                Debug.Print("---------TERMINANDO LSO PROCESOS DE TEMPORIZADOR Y PARPADEO---------");
                temporizador.Abort();
                parpadeo.Abort();
            }
            
        }

        /// <summary>
        /// Starts a timer that will indicate when the round finish
        /// </summary>
        private static void timer() {
            Datos.competi = true;
            //Datos.timeLeft = Datos.roundTime; ---------------------------------------------------------------->
            while (Datos.timeLeft > 0) {
                Datos.timeLeft--;
                Thread.Sleep(1000);
            }
            Datos.finishBattle = true;
            Datos.competi = false;
            
            
        }

        /// <summary>
        /// Blinks the onboard led while we're in competition
        /// </summary>
        private static void blink() {
            while (Datos.competi) {
                led.Write(!led.Read());
                Thread.Sleep(500);
            }
            led.Write(false);
        }

        private static void off()
        {
            Secador.Write(false);
            Ventilador1.Write(false);
            Ventilador2.Write(false);
        }

        /// <summary>
        /// Refresh the temp reading the sensor
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

            // Infinite loop that reads the temp and stores it in tempAct
            while (true) {

                Double rango = Datos.tempMax - Datos.tempMin;
                Double limiteSup = 0.35 * rango;
                Double limiteInf = 0.25 * rango;

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
                            // tanto el secador como el ventilador, operan en FALSE - circuito cerrado
                            if (Datos.tempAct >= (Datos.tempMax - limiteSup))      // FRIO
                            {
                                Secador.Write(false);
                                Ventilador1.Write(true);
                                Ventilador2.Write(true);
                                //Debug.Print("VENTILADOR");
                            }
                            else if (Datos.tempAct <= (Datos.tempMin + limiteInf)) // CALOR
                            {
                                Secador.Write(true);
                                Ventilador1.Write(false);
                                Ventilador2.Write(false);
                                //Debug.Print("SECADOR");
                            }
                            else                                                   // APAGAMOS TODO
                            {
                                off();
                            }

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

                            Thread.Sleep(1000);
                            
                            if (Datos.roundTimeAux == 0)
                            {
                                Datos.competi = false;
                                Debug.Print("-------->Se ha acabado el rountTime de esta ronda");
                            }


                        }
                        else if (Datos.finishBattle)
                        {
                            lcd.Clear();
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Temp War Grupo 1");
                            lcd.SetCursorPosition(0, 1);
                            lcd.Write("total: " + Datos.timeInRangeTemp+" seg.");
                            off();
                            Thread.Sleep(Datos.displayRefresh);
                           
                        }
                        else
                        {
                            lcd.Clear();
                            lcd.SetCursorPosition(0, 0);
                            lcd.Write("Temp War Grupo 1");
                            lcd.SetCursorPosition(0, 1);
                            lcd.Write("Temp: " + Datos.tempAct.ToString("N1") + "C");
                            off();
                            Thread.Sleep(Datos.displayRefresh);
                        }

                    }
                    else
                    {
                        Debug.Print("----Modo de espera");
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
    } // Program
} // namespace
