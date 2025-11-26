using System.Xml.Linq;

namespace Controller
{
    public class BladeConfiguration
    {
        public uint PWMGainUp;
        public uint PWMGainDown;
        public uint PWMMinUp;
        public uint PWMMinDown;
        public uint PWMMaxUp;
        public uint PWMMaxDown;
        public uint IntegralMultiplier;
        public uint Deadband;

        public BladeConfiguration()
        {
            PWMGainUp = 4;
            PWMGainDown = 3;
            PWMMinUp = 50;
            PWMMinDown = 50;
            PWMMaxUp = 180;
            PWMMaxDown = 180;
            IntegralMultiplier = 20;
            Deadband = 3;
        }

        public XElement ToXml
            (
            )
        {
            return new XElement("BladeConfiguration",
                new XElement("PWMGainUp", PWMGainUp),
                new XElement("PWMGainDown", PWMGainDown),
                new XElement("PWMMinUp", PWMMinUp),
                new XElement("PWMMinDown", PWMMinDown),
                new XElement("PWMMaxUp", PWMMaxUp),
                new XElement("PWMMaxDown", PWMMaxDown),
                new XElement("IntegralMultiplier", IntegralMultiplier),
                new XElement("Deadband", Deadband)
            );
        }

        public void FromXml
            (
            XElement xml
            )
        {
            if (xml == null)
            {
                return;
            }

            // Parse PWMGainUp
            XElement? pwmGainUpElement = xml.Element("PWMGainUp");
            if (pwmGainUpElement != null && uint.TryParse(pwmGainUpElement.Value, out uint pwmGainUp))
            {
                PWMGainUp = pwmGainUp;
            }

            // Parse PWMGainDown
            XElement? pwmGainDownElement = xml.Element("PWMGainDown");
            if (pwmGainDownElement != null && uint.TryParse(pwmGainDownElement.Value, out uint pwmGainDown))
            {
                PWMGainDown = pwmGainDown;
            }

            // Parse PWMMinUp
            XElement? pwmMinUpElement = xml.Element("PWMMinUp");
            if (pwmMinUpElement != null && uint.TryParse(pwmMinUpElement.Value, out uint pwmMinUp))
            {
                PWMMinUp = pwmMinUp;
            }

            // Parse PWMMinDown
            XElement? pwmMinDownElement = xml.Element("PWMMinDown");
            if (pwmMinDownElement != null && uint.TryParse(pwmMinDownElement.Value, out uint pwmMinDown))
            {
                PWMMinDown = pwmMinDown;
            }

            // Parse PWMMaxUp
            XElement? pwmMaxUpElement = xml.Element("PWMMaxUp");
            if (pwmMaxUpElement != null && uint.TryParse(pwmMaxUpElement.Value, out uint pwmMaxUp))
            {
                PWMMaxUp = pwmMaxUp;
            }

            // Parse PWMMaxDown
            XElement? pwmMaxDownElement = xml.Element("PWMMaxDown");
            if (pwmMaxDownElement != null && uint.TryParse(pwmMaxDownElement.Value, out uint pwmMaxDown))
            {
                PWMMaxDown = pwmMaxDown;
            }

            // Parse IntegralMultiplier
            XElement? integralMultiplierElement = xml.Element("IntegralMultiplier");
            if (integralMultiplierElement != null && uint.TryParse(integralMultiplierElement.Value, out uint integralMultiplier))
            {
                IntegralMultiplier = integralMultiplier;
            }

            // Parse Deadband
            XElement? deadbandElement = xml.Element("Deadband");
            if (deadbandElement != null && uint.TryParse(deadbandElement.Value, out uint deadband))
            {
                Deadband = deadband;
            }
        }
    }
}

