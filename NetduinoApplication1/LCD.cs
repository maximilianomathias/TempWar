using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Text;
using System.Threading;

namespace NetduinoDisplay
{
    class LCD
    {

        #region Constructor
        public LCD(Cpu.Pin rs, Cpu.Pin enable,
            Cpu.Pin d4, Cpu.Pin d5, Cpu.Pin d6, Cpu.Pin d7,
            byte columns, Operational lineSize, int numberOfRows, Operational dotSize)
        {
            RS = new OutputPort(rs, false);
            Enable = new OutputPort(enable, false);
            D4 = new OutputPort(d4, false);
            D5 = new OutputPort(d5, false);
            D6 = new OutputPort(d6, false);
            D7 = new OutputPort(d7, false);

            Columns = columns;
            DotSize = (byte)dotSize;
            NumberOfLines = (byte)lineSize;
            NumberOfRows = numberOfRows;

            Initialize();
        }
        #endregion


        #region Public Methods
        public void Show(string text, int delay, bool newLine)
        {
            if (newLine) dirtyColumns = 0;
            foreach (char textChar in text.ToCharArray())
            {
                ResetLines();
                Show(Encoding.UTF8.GetBytes(textChar.ToString()));
                dirtyColumns += 1;
                Thread.Sleep(delay);
            }
        }

        public void Show(string text)
        {
            string[] splitedText = SplitText(text);
            Show(splitedText);
        }


        public void ClearDisplay()
        {
            SendCommand((byte)Command.Clear);
            currentRow = 0;
            dirtyColumns = 0;
        }
        public void GoHome()
        {
            SendCommand((byte)Command.Home);
            currentRow = 0;
            dirtyColumns = 0;
        }
        public void JumpAt(byte column, byte row)
        {
            if (NumberOfLines == (byte)Operational.DoubleLIne) row = (byte)(row % 4);
            else row = (byte)(row % 2);

            SendCommand((byte)((byte)Command.SetDdRam | (byte)(column + rowAddress[row]))); //0 based index
        }

        public void PushContentToLeft()
        {
            SendCommand(0x18 | 0x00);
        }

        public void PushContentToRight()
        {
            SendCommand(0x18 | 0x04);
        }

        #endregion


        #region Private Methods
        private void Initialize()
        {
            //initialize fields
            isVisible = true;
            showCursor = false;
            isBlinking = false;

            rowAddress = new byte[] { 0x00, 0x40, 0x14, 0x54 };
            firstHalfAddress = new byte[] { 0x10, 0x20, 0x40, 0x80 };
            secondHalfAddress = new byte[] { 0x01, 0x02, 0x04, 0x08 };

            currentRow = 0;
            dirtyColumns = 0;

            Thread.Sleep(50); // must wait for a few milliseconds


            // RS to high = data transfer
            // RS to low = command/instruction transfer
            RS.Write(false);

            // Enable provides a clock function to synchronize data transfer
            Enable.Write(false);


            // Set for 4 bit model
            Write(0x03, secondHalfAddress);
            Thread.Sleep(4);
            Write(0x03, secondHalfAddress);
            Thread.Sleep(4);
            Write(0x03, secondHalfAddress);
            Thread.Sleep(150);
            Write(0x02, secondHalfAddress);


            // Set the LCD properties 
            byte operationalValue = (byte)((byte)Operational.FourBit | (byte)NumberOfLines | (byte)DotSize);
            SendCommand((byte)((byte)Command.Operational | operationalValue));

            UpdateDisplayOptions();

            ClearDisplay();

            byte entranceValue = (byte)Entrance.FromLeft | (byte)Entrance.ShiftDecrement;
            SendCommand((byte)((byte)Command.Entrance | entranceValue));

        }

        private string[] SplitText(string str)
        {
            if (str.Length > Columns * NumberOfRows) str = str.Substring(0, Columns * NumberOfRows);

            int stringArrayCounter = 0;
            dirtyColumns = 0;

            char[] charArray = str.ToCharArray();
            int arraySize = (int)System.Math.Ceiling((double)(str.Length + dirtyColumns) / Columns);
            string[] stringArray = new string[arraySize];

            for (int i = 0; i < charArray.Length; i++)
            {
                if (dirtyColumns < Columns)
                {
                    stringArray[stringArrayCounter] = stringArray[stringArrayCounter] + charArray[i];
                    dirtyColumns += 1;
                }
                else
                {
                    dirtyColumns = 1;
                    stringArrayCounter += 1;
                    stringArray[stringArrayCounter] = stringArray[stringArrayCounter] + charArray[i];
                }
            }
            return stringArray;
        }


        private void ResetLines()
        {
            if (dirtyColumns == 0) return;
            if (dirtyColumns % Columns == 0)
            {
                currentRow += 1;
                JumpAt((byte)0, (byte)(currentRow));
            }
        }

        private void Write(byte[] data)
        {
            foreach (byte value in data)
            {
                Write(value, firstHalfAddress); // First half
                Write(value, secondHalfAddress); // Second half
            }
        }

        private void Write(byte value, byte[] halfAddress)
        {
            D4.Write((value & halfAddress[0]) > 0);
            D5.Write((value & halfAddress[1]) > 0);
            D6.Write((value & halfAddress[2]) > 0);
            D7.Write((value & halfAddress[3]) > 0);

            Enable.Write(true);
            Enable.Write(false);
            //Debug.Print("Wrote " + value.ToString());
        }

        private void SendCommand(byte value)
        {
            RS.Write(false); // command/instruction transfer
            Write(new byte[] { value });

            Thread.Sleep(5);
        }

        private void UpdateDisplayOptions()
        {
            byte command = (byte)Command.DisplayControl;
            command |= isVisible ? (byte)DisplayControl.ScreenOn : (byte)DisplayControl.ScreenOff;
            command |= showCursor ? (byte)DisplayControl.CursorOn : (byte)DisplayControl.CursorOff;
            command |= isBlinking ? (byte)DisplayControl.BlinkBoxOn : (byte)DisplayControl.BlinkBoxOff;

            SendCommand(command);
        }

        private void Show(string[] splitedText)
        {
            foreach (string text in splitedText)
            {
                JumpAt((byte)0, (byte)(currentRow));
                currentRow += 1;

                Show(Encoding.UTF8.GetBytes(text));
            }
        }

        private void Show(byte[] bytes)
        {
            RS.Write(true);
            Write(bytes);
        }

        #endregion

        #region Public Properties

        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; UpdateDisplayOptions(); }
        }

        public bool IsBlinking
        {
            get { return isBlinking; }
            set { isBlinking = value; UpdateDisplayOptions(); }
        }

        public bool ShowCursor
        {
            get { return showCursor; }
            set { showCursor = value; UpdateDisplayOptions(); }
        }

        #endregion

        #region Fields
        private OutputPort RS;
        private OutputPort Enable;
        private OutputPort D4;
        private OutputPort D5;
        private OutputPort D6;
        private OutputPort D7;
        private byte Columns;
        private byte DotSize;
        private byte NumberOfLines;
        private byte[] rowAddress;
        private byte[] firstHalfAddress;
        private byte[] secondHalfAddress;
        private byte visibilityValue;


        private int currentRow;
        private int dirtyColumns;
        private int NumberOfRows;

        private bool isVisible;
        private bool showCursor;
        private bool isBlinking;
        #endregion


        #region Enums
        public enum Command : byte
        {
            Clear = 0x01,
            Home = 0x02,
            Entrance = 0x04,
            DisplayControl = 0x08,
            Move = 0x10,
            Operational = 0x20,
            SetCgRam = 0x40,
            SetDdRam = 0x80
        }

        public enum Entrance : byte
        {
            FromRight = 0x00,
            FromLeft = 0x02,
            ShiftIncrement = 0x01,
            ShiftDecrement = 0x00
        }

        public enum DisplayControl : byte
        {
            ScreenOn = 0x04,
            ScreenOff = 0x00,
            CursorOn = 0x02,
            CursorOff = 0x00,
            BlinkBoxOn = 0x01,
            BlinkBoxOff = 0x00
        }

        public enum Operational : byte
        {
            Dot5x10 = 0x04,
            Dot5x8 = 0x00,
            SingleLine = 0x00,
            DoubleLIne = 0x08,
            FourBit = 0x00
        }
        #endregion
    }
}
