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

        private static OutputPort pruebaRelay = new OutputPort(Pins.GPIO_PIN_D13, false);
        private static OutputPort pruebaRelay2 = new OutputPort(Pins.GPIO_PIN_D12, false); 
        private static OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);
        public static void Main() {
            // Create a WebServer
            MyWebServer server = new MyWebServer();
            I2CDevice sensor = new I2CDevice(new I2CDevice.Configuration(0x48, 50));

           // byte[] currentConfig = ReadTMP102Configuration(sensor);
            // Start the WebServer
            server.Start();

            // ************************************************start the Time Controller
            TimeController timecontroller = new TimeController();

            //Pruebas 
            /*
                string de numero decimal separado por comas hay que hacersel un double.parse(string). 
                Hay que asegurarse de que hay un try catch dentro de  parse. 
            */

            
            // ************************************************definir los array de rangos 
            TemperatureRange[] ranges = new TemperatureRange[3];
            bool configured;

            ranges[0] = new TemperatureRange(12, 15, 5000);
            ranges[1] = new TemperatureRange(22, 25, 5000);
            ranges[2] = new TemperatureRange(12.5, 13, 2000);
            string errorMessage = null;
            // el segundo argumento tiene que ser la suma de los arrays ^^^^
            configured =  timecontroller.Configure(ranges, 100000, 500, out errorMessage);


            // Start a new thread to read the temperature
            new Thread(readTemp).Start();

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
            Datos.timeInRangeTemp = 0;

            // Start a timer thread for the round
            new Thread(timer).Start();

            // Blink the led to indicate we're in competition
            new Thread(blink).Start();

            // TODO: Do the competition stuff here
            while (Datos.competi) {

                /**
                 * 
                 * TODO: Implement devices control logic here
                 * 
                 * */
                // Print current temperature
               Debug.Print("temperatura actual:");
                Debug.Print(Datos.tempAct+100 + "");

                // Wait for the refresh rate
                Thread.Sleep(Datos.refresh);
            }

            // TODO: The round has finished => Turn off devices if needed
        }

        /// <summary>
        /// Starts a timer that will indicate when the round finish
        /// </summary>
        private static void timer() {
            Datos.competi = true;
            Datos.timeLeft = Datos.roundTime;
            while (Datos.timeLeft > 0) {
                Datos.timeLeft--;
                Thread.Sleep(1000);
            }
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

        /// <summary>
        /// Refresh the temp reading the sensor
        /// </summary>
        private static void readTemp() {

            // TODO: Implement your way to read the temperature from the sensor
            // This is just an example, you may do it your way

            // Define the input
            //SecretLabs.NETMF.Hardware.AnalogInput a0 = new SecretLabs.NETMF.Hardware.AnalogInput(Pins.GPIO_PIN_A0);
            OneWire _oneWire = new OneWire(new OutputPort(Pins.GPIO_PIN_D0, false));
           // LCD lcd = new LCD(
               // Pins.GPIO_PIN_D11, // RS
                //Pins.GPIO_PIN_D9,  // Enable
                //Pins.GPIO_PIN_D7,  // D4
                //Pins.GPIO_PIN_D5,  // D5
                //Pins.GPIO_PIN_D3,  // D6
                //Pins.GPIO_PIN_D1,  // D7
                //20,                // Number of Columns 
                //LCD.Operational.DoubleLIne, // LCD Row Format
                //4,                 // Number of Rows in LCD
                //LCD.Operational.Dot5x8);    // Dot Size of LCD
            //Double valor;
            //Double voltage;
            //lcd.Show("prueba",1000,true);

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

            lcd.SetCursorPosition(0, 0);
            lcd.Write(Datos.tempMin.ToString("N1") + " - " + Datos.tempMax.ToString("N1"));

            Double rango = Datos.tempMax - Datos.tempMin;
            Double limiteSup = 0.35 * rango;
            Double limiteInf = 0.25 * rango;

            int tiempo = 0;
            Datos.timeInRangeTemp = 0;
            // Infinite loop that reads the temp and stores it in tempAct
            while (true) {
                /*valor = a0.Read();
                voltage = valor * 3.3 / 1024;
                Datos.tempAct = (voltage - 0.5) * 100;
                //Deberia primero imprimir la temperatura
                Debug.Print("temp :" + Datos.tempAct);
                //Debug.Print("temp :" + Datos.tempMax);
                //Debug.Print("temp :" + Datos.tempMin);
                //Debug.Print("Valor :"+valor);
                // Sleep
                Thread.Sleep(Datos.refresh);
                */
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

                        tiempo++;
                        Datos.tempAct = temperature / 16.0;
                        


                        if (Datos.tempAct <= Datos.tempMax && Datos.tempAct >= Datos.tempMin && Datos.timeLeft != 0 )
                        {
                            Datos.timeInRangeTemp++;
                        }
                        if (Datos.tempAct >= (Datos.tempMax - limiteSup))      // FRIO
                        {
                            pruebaRelay.Write(false); 
                            pruebaRelay2.Write(true);
                            Debug.Print("VENTILADOR");
                        }
                        else if (Datos.tempAct <= (Datos.tempMin + limiteInf)) // CALOR
                        {
                            pruebaRelay.Write(true);
                            pruebaRelay2.Write(false);
                            Debug.Print("SECADOR");
                        }else                                                   // APAGAMOS TODO
                        {
                            pruebaRelay.Write(false);
                            pruebaRelay2.Write(false);
                            Debug.Print("OFF - DENTRO DEL RANGO");
                        }
                       

                        //Datos.tempAct = Microsoft.SPOT.Math.(Datos.tempAct, 1);
                        

                        lcd.SetCursorPosition(13, 0);
                        lcd.Write(Datos.timeLeft.ToString());

                        lcd.SetCursorPosition(0, 1);                
                        lcd.Write(Datos.tempAct.ToString("N1") + "C");

                        lcd.SetCursorPosition(13,1);
                        lcd.Write(Datos.timeInRangeTemp.ToString());

                        Thread.Sleep(1000);

                        lcd.SetCursorPosition(12, 0);
                        lcd.Write("    ");

                        lcd.SetCursorPosition(0, 1);
                        lcd.Write("                ");
                        
                        //lcd.Clear();
                        // lcd.ClearDisplay();
                        //lcd.Show("Temp:" + Datos.tempAct + " C", 200, true);
                    }
                    else
                    {
                        Debug.Print("ReadTemperatureToConsole " + "No device detected");
                        //Could be that you read to fast after previous read. Include Thread.Sleep(100);
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
