using System;
using Microsoft.SPOT;
using System.Threading;
using NETDuinoWar;

namespace NetduinoController.Web
{
    class MyWebServer
    {
        private static readonly string pass = "pass";
        private static readonly string IP = "192.168.1.4";
        private static string msgAux = "";
        private static string inputData = "";

        private WebServer server;

        private static bool ready = false;

        /// <summary>
        /// Instantiates a new webserver with our data
        /// </summary>
        public MyWebServer()
        {
            // Enable DHCP Server
            Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].EnableDhcp();  

            // Assign an static IP to the Netduino
            Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].EnableStaticIP(IP,
                "255.255.255.0", "192.168.1.1");

            // Instantiate a new web server.
            server = new WebServer();

            // Add a handler for commands that are received by the server.
            server.CommandReceived += new WebServer.CommandReceivedHandler(server_CommandReceived);

            // Add the commands that the server will parse.
            // http://[server-ip]/index
            // http://[server-ip]/setparams/pass/tempMax/tempMin/displayRefresh/refresh/roundTime
            // http://[server-ip]/start/pass
            // http://[server-ip]/coolermode/pass
            server.AllowedCommands.Add(new WebCommand("index", 0));
            server.AllowedCommands.Add(new WebCommand("setparams", 4));
            server.AllowedCommands.Add(new WebCommand("start", 1));
            server.AllowedCommands.Add(new WebCommand("coolermode", 1));
            server.AllowedCommands.Add(new WebCommand("temp", 0));
            server.AllowedCommands.Add(new WebCommand("time", 0));
            server.AllowedCommands.Add(new WebCommand("setround", 4));
            
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        public void Start()
        {
            server.Start();
        }

        /// <summary>
        /// Handles the CommandReceived event.
        /// </summary>
        private static void server_CommandReceived(object source, WebCommandEventArgs e)
        {

            Debug.Print("Command received: " + e.Command.CommandString);

            switch (e.Command.CommandString)
            {

                case "index":
                    {
                        string message = msgAux;

                        if (!msgAux.Equals(""))
                            msgAux = "";
                        
                        // Return the index to web user.
                        e.ReturnString = writeHTML(message);
                        
                        break;
                    }
                case "setround":
                    {
                        Debug.Print("------->Seteando los parametros para la ronda");
                        if (ready)
                        {
                            msgAux = "No se pueden cambiar los par&aacute;metros en competici&oacute;n ni una vez preparado el sistema.";
                            e.ReturnString = redirect("index");
                            break;
                        }
                        // Si el tiempo global introducido es diferente al que ya teniamos previamente guardado, lo cambiamos. 
                        if ((int.Parse(e.Command.Arguments[3].ToString())) != Datos.timeLeft)
                            Datos.timeLeft = int.Parse(e.Command.Arguments[3].ToString());

                        if (double.Parse(e.Command.Arguments[0].ToString()) > 30 || double.Parse(e.Command.Arguments[1].ToString()) < 12)
                        {
                            msgAux = "El rango de temperatura m&aacute;ximo es entre 30 y 12 grados C.";
                            e.ReturnString = redirect("index");
                            break;
                        }
                        // Validate the data
                        if (e.Command.Arguments[0].ToString().Length == 0 ||
                            e.Command.Arguments[1].ToString().Length == 0 ||
                            e.Command.Arguments[2].ToString().Length == 0 )
                        {
                            msgAux = "Debe especificar todos los par&aacute;metros que se piden.";
                            e.ReturnString = redirect("index");
                            break;
                        }
                        else
                        {
                            // concatenamos los datos para guardarlos en la variable 'Datos.roundQueue'
                            for(int a = 0; a<3; a++)
                            {
                                Datos.roundQueue += e.Command.Arguments[a].ToString();
                                if (a == 2)
                                    Datos.roundQueue += '/';
                                else 
                                    Datos.roundQueue += '-';
                            }
                            
                            Debug.Print("------> todos los comandos estan completos: "+ e.Command.Arguments[0].ToString()+"-"+e.Command.Arguments[1].ToString()+"-"+e.Command.Arguments[2].ToString());
                            Debug.Print("------> datos roundQueue: " + Datos.roundQueue);
                            Debug.Print("------> tiempo total: " + Datos.timeLeft);
                        }

                        msgAux = "Se ha introducido una nueva ronda. Click 'set ronda' para una nueva ronda o 'Guardar' si ya estas preparado";
                        e.ReturnString = redirect("index");
                        break;
                    }
                case "setparams":
                    {
                        if (!e.Command.Arguments[0].Equals(pass))
                        {
                            msgAux = "La constrase&ntilde;a es incorrecta.";
                            e.ReturnString = redirect("index");
                            break;
                        }
                        // Check the password is correct
                        if (!e.Command.Arguments[0].Equals(pass))
                        {
                            // Return feedback to web user.
                            msgAux = "La constrase&ntilde;a es incorrecta.";
                            e.ReturnString = redirect("index");
                            break;
                        }


                        // Check we're not in cometition
                        if (ready)
                        {
                            // Return feedback to web user.
                            msgAux = "No se pueden cambiar los par&aacute;metros en competici&oacute;n ni una vez preparado el sistema.";
                            e.ReturnString = redirect("index");
                            break;
                        }

                        // Validate the data
                        if (e.Command.Arguments[1].ToString().Length == 0 || 
                            e.Command.Arguments[2].ToString().Length == 0 ||
                            e.Command.Arguments[3].ToString().Length == 0)
                        {
                            // Return feedback to web user.
                            msgAux = "Debe especificar todos los par&aacute;metros que se piden.";
                            e.ReturnString = redirect("index");
                            break;
                        }

                        // Guardamos los diferentes rangos en un array de strings separandolos por el '/'
                        String[] rangos = Datos.roundQueue.Split('/');

                        // Instanciamos lo nuevo objetos TemperatureRange
                        Datos.rangos = new TemperatureRange[rangos.Length];
                        //Debug.Print("----->Numero de rangos: " + rangos.Length);

                        for (int i = 0; i < (rangos.Length-1); i++)
                        {
                            String[] parametros= rangos[i].Split('-');
                            // TemperatureRange(double Max, double Min, int RoundTime);
                            Datos.rangos[i] = new TemperatureRange(double.Parse(parametros[1]), double.Parse(parametros[0]), int.Parse(parametros[2]));
                        }
                            double temporal, temporalMax;
                            int temporalTem;

                            for (int i = 0; i < rangos.Length - 1; i++)
                            {
                                for (int j = 0; j <= (rangos.Length - 2); j++)
                                {
                                    Debug.Print(j.ToString());
                                    if ((j + 1) <= (rangos.Length - 2))
                                    {
                                        //Debug.Print("Estoy aqui" + Datos.rangos[j].MinTemp + " - " + Datos.rangos[j + 1].MinTemp);
                                        if (Datos.rangos[j].MinTemp > Datos.rangos[j + 1].MinTemp)
                                        { // Ordena el array de mayor a menor, cambiar el "<" a ">" para ordenar de menor a mayor
                                            
                                            temporal = Datos.rangos[j].MinTemp;
                                            Datos.rangos[j].MinTemp = Datos.rangos[j + 1].MinTemp;
                                            Datos.rangos[j + 1].MinTemp = temporal;
                                            Debug.Print("Cambiado: " + Datos.rangos[j].MinTemp + " - " + Datos.rangos[j + 1].MinTemp);

                                            temporalMax = Datos.rangos[j].MaxTemp;
                                            Datos.rangos[j].MaxTemp = Datos.rangos[j + 1].MaxTemp;
                                            Datos.rangos[j + 1].MaxTemp = temporalMax;
                                            Debug.Print("Cambiado: " + Datos.rangos[j].MaxTemp + " - " + Datos.rangos[j + 1].MaxTemp);

                                            temporalTem = Datos.rangos[j].RangeTimeInMilliseconds;
                                            Datos.rangos[j].RangeTimeInMilliseconds = Datos.rangos[j + 1].RangeTimeInMilliseconds;
                                            Datos.rangos[j + 1].RangeTimeInMilliseconds = temporalTem;
                                            Debug.Print("Cambiado: " + Datos.rangos[j].RangeTimeInMilliseconds + " - " + Datos.rangos[j + 1].RangeTimeInMilliseconds);
                                        }
                                    }
                                }
                            }
                       
                        Datos.displayRefresh = int.Parse(e.Command.Arguments[1].ToString());
                        Datos.refresh = int.Parse(e.Command.Arguments[2].ToString());
                        Datos.timeLeft = int.Parse(e.Command.Arguments[3].ToString());

                        Datos.tempMax = Datos.rangos[0].MaxTemp;
                        Datos.tempMin = Datos.rangos[0].MinTemp;
                        Datos.roundTime = Datos.rangos[0].RangeTimeInMilliseconds;
                        Datos.roundTimeAux = Datos.roundTime;

                        // Indicate we are ready
                        ready = true;

                        // Return feedback to web user.
                        msgAux = "Los par&aacute;metros se han cambiado satisfactoriamente. Todo preparado.";
                        e.ReturnString = redirect("index");
                        break;
                    }
                case "start":
                    {
                        // Check the password is correct
                        if (!e.Command.Arguments[0].Equals(pass))
                        {
                            // Return feedback to web user.
                            msgAux = "La constrase&ntilde;a es incorrecta.";
                            e.ReturnString = redirect("index");
                            break;
                        }

                        // Check we're not in cometition
                        if (Datos.competi)
                        {
                            msgAux = "Ya estamos en competici&oacute;n.";
                            e.ReturnString = redirect("index");
                            break;
                        }
                        int rounds = 0;
                        Datos.timeInRangeTemp = 0;

                        Thread nuevaRonda = new Thread(Program.startRound);
                        nuevaRonda.Start();

                        // Insertamos los parametros de cada partida

                      /*  for(int a = 0; a < Datos.rangos.Length -1; a++)
                        {
                            Datos.totalRangeTime += Datos.rangos[a].RangeTimeInMilliseconds;
                            Debug.Print("---------------------->Tiempo guardado: "+Datos.totalRangeTime );
                            rounds++;
                        } */

                        
                        while (Datos.competi)
                        {
                            while (rounds < Datos.rangos.Length - 1)
                            {
                                if(Datos.roundTimeAux == 0 && Datos.rangos[rounds+1] != null)
                                {
                                    Debug.Print("--------------------------------------INSERTANDO NUEVOS DATOS DE RONDA------------------------------");
                                    rounds++;
                                    Datos.tempMax = Datos.rangos[rounds].MaxTemp;
                                    Datos.tempMin = Datos.rangos[rounds].MinTemp;
                                    Datos.roundTime = Datos.rangos[rounds].RangeTimeInMilliseconds;
                                    Datos.roundTimeAux = Datos.roundTime;

                                }
                              
                                // Return feedback to web user.
                                if (Datos.error)
                                {
                                    Datos.error = false;
                                    msgAux = "Se ha detenido la competici&oacute;n porque se detect&oacute; una temperatura superior a 40C.";
                                    e.ReturnString = redirect("index");
                                    break;
                                }
                                else
                                {
                                    /*msgAux = "Se ha terminado la ronda con " + System.Math.Round((Datos.timeInRangeTemp / 1000) * 10) / 10 + "s en el rango indicado.";*/
                                    msgAux = "Se ha terminado la ronda con " + Datos.timeInRangeTemp + " segundos en el rango indicado";
                                    e.ReturnString = redirect("index");
                                    Datos.roundQueue = "";
                                    break;
                                }  
                            }
                        }

                        ready = false;
                        if (Datos.finishBattle)
                        {
                            Debug.Print("---------terminado lso hilos");
                            nuevaRonda.Abort();
                        }
                        break;
                    }
                case "coolermode":
                    {
                        // Check the password is correct
                        if (!e.Command.Arguments[0].Equals(pass))
                        {
                            // Return feedback to web user.
                            msgAux = "La constrase&ntilde;a es incorrecta.";
                            e.ReturnString = redirect("index");
                            break;
                        }

                        // Check we're not in cometition or ready for it
                        if (ready)
                        {
                            // Return feedback to web user.
                            msgAux = "No se puede activar este modo en competici&oacute;n ni una vez preparado el sistema.";
                            e.ReturnString = redirect("index");
                            break;
                        }

                        // Starts the cooler mode
                        //new Thread(Program.coolerMode).Start();

                        // Return feedback to web user.
                        msgAux = "Se ha iniciado el modo enfriamiento satisfactoriamente.";
                        e.ReturnString = redirect("index");
                        break;
                    }
                case "temp":
                    {
                        // Return feedback to web user.
                        msgAux = "La temperatura del sistema es de " + Datos.tempAct + "C.";
                        e.ReturnString = redirect("index");
                        break;
                    }
                case "time":
                    {
                        // Return feedback to web user.
                        msgAux = "El tiempo que se ha mantenido en el rango de temperatura es de " + Datos.timeInRangeTemp+ "s.";
                        e.ReturnString = redirect("index");
                        break;
                    }
            }
        }

        /// <summary>
        /// Create an HTML webpage.
        /// </summary>
        /// <param name="message">The message you want to be shown.</param>
        /// <returns>String with the HTML page desired.</returns>
        public static string writeHTML(String message)
        {
            // If we are already ready, disable all the inputs
            string disabled = "";
            if (ready) disabled = "disabled";

            // Only show save and cooler mode in configuration mode and start round when we are ready
            string save = "<a href='#' onclick='save()'>Estoy Preparado - Guardar</a>";
            if (ready) save = "";
            string start = "";
            if (ready) start = "<a style='padding-left:80px;' href='#' onclick='start()'>Comenzar Ronda</a>";
            if (Datos.competi) start = "";
            string cooler = "<a href='#' onclick='coolerMode()'>Modo Enfriamiento</a>";
            if (ready) cooler = "";
            string inputText = "";
            //Write the HTML page
            string html = "<!DOCTYPE html><html><head><title>Grupo 1 MDV</title>" +

                            "<script type='text/javascript'>" +
                            "var i = 1;" +
                            "var dataInput; "+
                                 
                                "function set(){"+
                                    "var tempMax = document.forms['params']['tempMax'].value;" +
                                    "var tempMin = document.forms['params']['tempMin'].value;" +
                                    "var time = document.forms['params']['time'].value;" +
                                    "var globalTime = document.forms['params']['tiempoGlobal'].value;" +
                                    "window.location = 'http://" + IP + "/setround/'+ tempMax +'/'+ tempMin +'/'+ time + '/' + globalTime;" +
                                "}" +
                                                
                                "function save(){" + // save()

                                    "var tempMax = document.forms['params']['tempMax'].value;" +
                                    "var tempMin = document.forms['params']['tempMin'].value;" +
                                    "var displayRefresh = document.forms['params']['displayRefresh'].value;" +
                                    "var refresh = document.forms['params']['refresh'].value;" +
                                    "var time = document.forms['params']['time'].value;" +
                                    "var pass = document.forms['params']['pass'].value;" +
                                    "var timeGlobal = document.forms['params']['tiempoGlobal'].value;" +
                                    "window.location = 'http://" + IP + "/setparams/' + pass + '/'+ displayRefresh + '/' + refresh + '/' + timeGlobal;" +
                                "}" +

                                "function start(){"+ // start()

                                    "var pass = document.forms['params']['pass'].value;window.location = 'http://" + IP + "/start/' + pass;"+
                                "}" +

                                "function time(){"+ // time()

                                    "window.location = 'http://" + IP + "/time';"+
                                "}" +

                                "function temp(){"+ // temp()

                                    "window.location = 'http://" + IP + "/temp';"+
                                "}" +

                                "function coolerMode(){"+// coolerMode()

                                    "var pass = document.forms['params']['pass'].value;"+
                                    "window.location = 'http://" + IP + "/coolermode/' + pass;"+
                                "}"+
                            
                                "function crearRango(){" + // crearRango()

                                    "mNewObj = document.createElement('div');" +
                                    "mNewObj.id = 'BOX';" +
                                    "mNewObj.style.visibility = 'show';" +
                                    "mNewObj.innerHTML = 'Ronda ' + i + document.getElementById('myP').innerHTML;" +
                                    "document.getElementById('tid').appendChild(mNewObj);" +

                                "i++;}" +

                            "</script>" +
                            // CSS
                            "<style>" +
                             "#BOX, #myP, #globalData {border: 4px solid black; padding: 1vh;}" +
                             ".inner { border: 1px solid green; margin: 10px; width: auto; height: 20px; }" +
                            "</style>" +
                            //HTML
                            "</head>" +
                            "<body>"+
                                "<p style='padding:10px;font-weight:bold; background-color: yellow;'>" + message + "</p>"+
                                "<form action='' name='params' method ='post'>" +
                                    "<div id = 'globalData'>"+

                                        "<p>Cadencia Refresco <b>(ms)</b> <input name='displayRefresh' type='number' value='" + Datos.displayRefresh + "' " + disabled + "></input></p>" +
                                        "<p>Cadencia Interna <b>(ms)</b> <input name='refresh' type='number' value='" + Datos.refresh + "' " + disabled + "></input></p>" +
                                        "<p>Tiempo global <b>(ms)</b> <input name='tiempoGlobal' type='number' value='" + Datos.timeLeft + "' " + disabled + "></input></p>" +
                                    "</div>" +

                                    "<div id = 'myP'>" + // este el el div que se crea cuando creamos un nuevo rango

                                        "<p>Temperatura Max <b>(&deg;C)</b> <input name='tempMax' id='tempMax' type='number' max='30' min='12' step='0.1' value='" + Datos.tempMax + "' " + disabled + "></input></p>" +
                                        "<p>Temperatura Min <b>(&deg;C)</b> <input name='tempMin' id='tempMin' type='number' max='30' min='12' step='0.1' value='" + Datos.tempMin + "' " + disabled + "></input></p>" +
                                        "<p>Duraci&oacute;n Ronda <b>(s)</b> <input name='time' id='time' type='number' value='inserte el tiempo' " + disabled + "></input></p>" +
                                        "<input type = 'hidden' id = 'joint'>"+
                                        
                                        //"<input type = 'submit' value = 'Set Round' onclick = 'set();' />" +
                                        "<a href='#' onclick='set()'>Set Round</a><br/>" +

                                   "</div>" +

                                    "<div id='tid'></div>" + // aqui metemos el nuero rango
                                    "<p>Contrase&ntilde;a <input name='pass' type='password'></input></p>" +

                                "</form>"+
                                save + start + "<br/>" + cooler + "<br/>" +
                                "<a href='#' onclick='temp()'>Consultar Temperatura</a><br/><a href='#' onclick='time()'>Consultar Tiempo</a><br/>" +
                                "<a href='#' onclick='crearRango()'>Crear rango</a><br/>" +

                                "<form method = 'post'>"+
                                "</form>" +

                            "</body>";

            return html;
        }

        /// <summary>
        /// Create an HTML page that redirect to the desired page. (used to prevent params like password showing in the url)
        /// </summary>
        /// <param name="page">The page you want to redirect.</param>
        /// <returns>String with the HTML page desired.</returns>
        public static string redirect(string page)
        {
            return "<!DOCTYPE html><html><head><meta http-equiv='refresh' content='0; url=http://" + IP + "/" + page + "'></head></html>";
        }

        

    }
}
