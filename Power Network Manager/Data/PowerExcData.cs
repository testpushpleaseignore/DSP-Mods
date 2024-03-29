﻿namespace PowerNetworkManager.Data {
	public class PowerExcData : PowerData {
		public long maxPower;
		public string maxPowerString;

		public long curPower;
		public string curPowerString;

		public long ratedPower;
		public string ratedPowerString;

		public override void generateStrings() {
			maxPowerString = HelperMethods.convertPowerToString(maxPower);
			curPowerString = HelperMethods.convertPowerToString(curPower);
			ratedPowerString = HelperMethods.convertPowerToString(ratedPower);
		}
	}
}
