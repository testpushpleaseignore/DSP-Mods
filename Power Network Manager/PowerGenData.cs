namespace PowerNetworkManager.UI {
	public class PowerGenData {
		public long maxPower;
		public long genPower;
		public long curPower;

		public PowerGenData() {
			Reset();
		}

		public void Reset() {
			genPower = 0;
		}
	}
}
