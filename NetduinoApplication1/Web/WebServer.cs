using System;
using Microsoft.SPOT;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

namespace NetduinoController.Web
{
    /// <summary>
    /// Simple multithreaded webserver for Netduino.
    /// Lets user define a set of allowed commands (+ parameters) that can be called on the server.
    /// When the server receives a valid command, it fires the CommandReceived event with details
    /// stored in the WebCommandEventArgs parameter.
    /// 
    /// Usage:
    /// - Instantiate a new instance of WebServer:
    ///   WebServer server = new WebServer();
    ///   
    /// - Add a handler to the CommandReceived event:
    ///   server.CommandReceived += new WebServer.CommandReceivedHandler(server_CommandReceived);
    ///   
    /// - Add one or more commands:
    ///   server.AllowedCommands.Add(new WebCommand("setled", 1));        // Means that you can call http://[ip-address]/setled/argument1
    ///   server.AllowedCommands.Add(new WebCommand("writechannel", 2));  // Means that you can call http://[ip-address]/writechannel/argument1/argument2 (e.g. channel/value)
    /// 
    /// - Start the server:
    ///   server.Start();
    /// </summary>
    public class WebServer : IDisposable
    {
        private Socket socket = null;
        private Thread serverThread = null;

        #region Constructors

        /// <summary>
        /// Instantiates a new webserver.
        /// </summary>
        public WebServer()
        {
            this.serverThread = new Thread(StartServer);
            Debug.Print("WebControl started on port 80");
        }
        
        #endregion

        #region Events

        /// <summary>
        /// Delegate for the CommandReceived event.
        /// </summary>
        public delegate void CommandReceivedHandler(object source, WebCommandEventArgs e);

        /// <summary>
        /// CommandReceived event is triggered when a valid command (plus parameters) is received.
        /// Valid commands are defined in the AllowedCommands property.
        /// </summary>
        public event CommandReceivedHandler CommandReceived;

        #endregion

        #region Public and private methods

        /// <summary>
        /// Start the multithreaded server.
        /// </summary>
        public void Start()
        {
            // Start server
            serverThread.Start();
            Debug.Print("Started server in thread " + serverThread.GetHashCode().ToString());
        }

        /// <summary>
        /// Parses a raw web request and filters out the command and arguments.
        /// </summary>
        /// <param name="rawData">The raw web request (including headers).</param>
        /// <returns>The parsed WebCommand if the request is valid, otherwise Null.</returns>
        private WebCommand InterpretRequest(string rawData)
        {

            string commandData;

            // Remove GET + Space
            if (rawData != null && rawData.Length > 5)
                commandData = rawData.Substring(5, rawData.Length - 5);
            else
                return null;

            // Remove everything after first space
            int idx = commandData.IndexOf("HTTP/1.1");
            commandData = commandData.Substring(0, idx - 1);

            // Split command and arguments
            string[] parts = commandData.Split('/');

            string command = null;
            if (parts.Length > 0)
            {
                // Parse first part to command
                command = parts[0].ToLower();
            }

            // Check if this is a valid command
            WebCommand outCmd = null;
            foreach (WebCommand cmd in AllowedCommands)
            {
                if (cmd.CommandString.ToLower() == command)
                {
                    outCmd = new WebCommand(cmd.CommandString, cmd.ArgumentCount);
                    break;
                }
            }
            if (outCmd == null)
            {
                return null;
            }
            else // Get command arguments
            {
                if ((parts.Length - 1) != outCmd.ArgumentCount)
                {
                    return null;
                }
                else
                {
                    outCmd.Arguments = new string[parts.Length - 1];

                    for (int i = 1; i < parts.Length; i++)
                    {
                        outCmd.Arguments[i - 1] = parts[i];
                    }
                    return outCmd;
                }
            }
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        private void StartServer()
        {
            // Initialize Socket class
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Request and bind to an IP from DHCP server
            socket.Bind(new IPEndPoint(IPAddress.Any, 80));
            // Debug print our IP address
            Debug.Print("Listening on " + Microsoft.SPOT.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].IPAddress + ":80");
            // Start listen for web requests
            socket.Listen(10);

            while (true)
            {
                using (Socket clientSocket = socket.Accept())
                {
                    // Get clients IP
                    IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;
                    Debug.Print("Received request from " + clientIP.ToString());
                    EndPoint clientEndPoint = clientSocket.RemoteEndPoint;

                    if (clientSocket.Poll(-1, SelectMode.SelectRead))
                    {
                        // Create buffer and receive raw bytes.
                        byte[] bytes = new byte[clientSocket.Available];
                        int count = clientSocket.Receive(bytes);

                        // Convert to string, will include HTTP headers.
                        string rawData = new string(Encoding.UTF8.GetChars(bytes));
                        WebCommand command = InterpretRequest(rawData);

                        if (command != null)
                        {
                            WebCommandEventArgs args = new WebCommandEventArgs(command);
                            if (CommandReceived != null)
                            {
                                CommandReceived(this, args);
                                byte[] returnBytes = Encoding.UTF8.GetBytes(args.ReturnString);
                                clientSocket.Send(returnBytes, 0, returnBytes.Length, SocketFlags.None);
                            }
                        }
                    }
                }
                Thread.Sleep(10);
            }

        }

        #endregion

        #region Properties

        /// <summary>
        /// List of commands that can be handled by the server.
        /// Because of the limited support for generics on the .NET micro framework,
        /// this property is implemented as an ArrayList. Make sure you only add
        /// objects of type Blinq.Netduino.WebCommand to this list.
        /// </summary>
        public readonly System.Collections.ArrayList AllowedCommands = new System.Collections.ArrayList();

        #endregion

        #region IDisposable Members

        ~WebServer()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (socket != null)
                socket.Close();
        }

        #endregion
    }
}
