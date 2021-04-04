namespace PowerNetworkManager.Data {
	public class PowerConsData : PowerData {
		public long idlePower;
		public string idlePowerString;

		public long maxPower;
		public string maxPowerString;

		public long currPower;
		public string currPowerString;

		public override void generateStrings() {
			idlePowerString = HelperMethods.convertPowerToString(idlePower);
			maxPowerString = HelperMethods.convertPowerToString(maxPower);
			currPowerString = HelperMethods.convertPowerToString(currPower);
		}
	}
}
