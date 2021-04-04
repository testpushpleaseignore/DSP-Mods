using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerNetworkManager.Data {
	public class HelperMethods {
        public static string convertPowerToString(long power) {
            string val;

            if (power / 1e9 >= 1.0)
                val = $"{Math.Round(power / 1e9, 3)} GW";
            else if (power / 1e6 >= 1.0)
                val = $"{Math.Round(power / 1e6, 3)} MW";
            else if (power / 1e3 >= 1.0)
                val = $"{Math.Round(power / 1e3, 3)} kW";
            else
                val = $"{power} W";

            return val;
        }
    }
}
