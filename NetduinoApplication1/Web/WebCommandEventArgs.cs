using System;
using Microsoft.SPOT;

namespace NetduinoController.Web
{
    /// <summary>
    /// Event arguments of an incoming web command.
    /// </summary>
    public class WebCommandEventArgs
    {
        public WebCommandEventArgs()
        {
        }

        public WebCommandEventArgs(WebCommand command)
        {
            Command = command;
        }

        public WebCommand Command { get; set; }
        public string ReturnString { get; set; }
    }
}
