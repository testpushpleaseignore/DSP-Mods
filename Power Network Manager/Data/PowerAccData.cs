namespace PowerNetworkManager.Data {
	public class PowerAccData : PowerData {
		public long maxDiscPower;
		public string maxDiscPowerString;

		public long maxChgPower;
		public string maxChgPowerString;

		public long curPower;
		public string curPowerString;
		public long curPowerAbs;
		public string curPowerAbsString;

		public override void generateStrings() {
			maxDiscPowerString = HelperMethods.convertPowerToString(maxDiscPower);
			maxChgPowerString = HelperMethods.convertPowerToString(maxChgPower);
			curPowerString = HelperMethods.convertPowerToString(curPower);
			curPowerAbsString = HelperMethods.convertPowerToString(curPowerAbs);
		}
	}
}
