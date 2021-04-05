using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerNetworkManager.Data {
	public class HelperMethods {
		public static string convertPowerToString(long power) {
			string val;

			if (power / 1e9 >= 1.0)
				val = $"{SetSigFigs(power / 1e9, PowerDataCalc.sigFigs)} GW";
			else if (power / 1e6 >= 1.0)
				val = $"{SetSigFigs(power / 1e6, PowerDataCalc.sigFigs)} MW";
			else if (power / 1e3 >= 1.0)
				val = $"{SetSigFigs(power / 1e3, PowerDataCalc.sigFigs)} kW";
			else
				val = $"{power} W";

			return val;
		}

		public static double SetSigFigs(double d, int digits) {
			if (d == 0)
				return 0;

			decimal scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

			return (double)(scale * Math.Round((decimal)d / scale, digits));
		}
	}
}
