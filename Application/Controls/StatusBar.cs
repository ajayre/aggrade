using AgGrade.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

using Timer = System.Timers.Timer;

namespace AgGrade.Controls
{
    public partial class StatusBar : UserControl
    {
        public enum LedState
        {
            OK,
            Error,
            Disabled
        }

        public enum Leds
        {
            TractorRTK,
            FrontRTK,
            RearRTK,
            TractorIMU,
            FrontIMU,
            RearIMU,
            FrontHeight,
            RearHeight
        }

        private class Led
        {
            public Leds led;
            public PictureBox UI;
            public LedState State;
            public bool On;

            public Led
                (
                Leds led,
                PictureBox UI,
                LedState State
                )
            {
                this.led = led;
                this.UI = UI;
                this.State = State;

                Update();
            }

            /// <summary>
            /// Updates the state of an LED
            /// </summary>
            public void Update
                (
                )
            {
                switch (State)
                {
                    case LedState.Disabled:
                        UI.Image = Resources.led_grey_24px;
                        break;

                    case LedState.Error:
                        if (On)
                        {
                            UI.Image = Resources.led_red_24px;
                        }
                        else
                        {
                            UI.Image = Resources.led_off_24px;
                        }
                        break;

                    case LedState.OK:
                        UI.Image = Resources.led_green_24px;
                        break;
                }
            }
        }

        private const int ERROR_FLASH_PERIOD_MS = 600;

        private Timer FlashTimer = new Timer();
        private Led TractorRTK;
        private Led FrontRTK;
        private Led RearRTK;
        private Led TractorIMU;
        private Led FrontIMU;
        private Led RearIMU;
        private Led FrontHeight;
        private Led RearHeight;
        private List<Led> SupportedLeds;

        public StatusBar()
        {
            InitializeComponent();

            TractorRTK = new Led(Leds.TractorRTK, TractorRTKLed, LedState.Disabled);
            FrontRTK = new Led(Leds.FrontRTK, FrontRTKLed, LedState.Disabled);
            RearRTK = new Led(Leds.RearRTK, RearRTKLed, LedState.Disabled);
            TractorIMU = new Led(Leds.TractorIMU, TractorIMULed, LedState.Disabled);
            FrontIMU = new Led(Leds.FrontIMU, FrontIMULed, LedState.Disabled);
            RearIMU = new Led(Leds.RearIMU, RearIMULed, LedState.Disabled);
            FrontHeight = new Led(Leds.FrontHeight, FrontHeightLed, LedState.Disabled);
            RearHeight = new Led(Leds.RearHeight, RearHeightLed, LedState.Disabled);

            SupportedLeds = new List<Led>();
            SupportedLeds.Add(TractorRTK);
            SupportedLeds.Add(FrontRTK);
            SupportedLeds.Add(RearRTK);
            SupportedLeds.Add(TractorIMU);
            SupportedLeds.Add(FrontIMU);
            SupportedLeds.Add(RearIMU);
            SupportedLeds.Add(FrontHeight);
            SupportedLeds.Add(RearHeight);

            FlashTimer.Interval = ERROR_FLASH_PERIOD_MS;
            FlashTimer.Elapsed += FlashTimer_Elapsed;
            FlashTimer.Start();
        }

        /// <summary>
        /// Sets the state of an LED
        /// </summary>
        /// <param name="led">LED to change</param>
        /// <param name="NewState">New state</param>
        public void SetLedState
            (
            Leds led,
            LedState NewState
            )
        {
            foreach (Led l in SupportedLeds)
            {
                if (l.led == led)
                {
                    l.State = NewState;
                    l.Update();
                    break;
                }
            }
        }

        /// <summary>
        /// Flash error LEDs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlashTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (Led l in SupportedLeds)
            {
                if (l.State == LedState.Error)
                {
                    if (l.On)
                        l.On = false;
                    else
                        l.On = true;

                    l.Update();
                }
            }
        }
    }
}
