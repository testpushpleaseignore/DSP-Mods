namespace PowerNetworkManager.Data {
	public class PowerGenData : PowerData {
		public long maxPower;
		public string maxPowerString;

		public long genPower;
		public string genPowerString;

		public long curPower;
		public string curPowerString;

		public override void generateStrings() {
			maxPowerString = HelperMethods.convertPowerToString(maxPower);
			genPowerString = HelperMethods.convertPowerToString(genPower);
			curPowerString = HelperMethods.convertPowerToString(curPower);
		}
	}
}
