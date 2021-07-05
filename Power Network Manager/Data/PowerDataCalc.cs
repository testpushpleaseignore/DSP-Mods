using PowerNetworkStructures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerNetworkManager.Data {
	public class PowerDataCalc {
		static long lastTime = 0;
		const long refreshRateSec = 2;

		public const int sigFigs = 3;

		public GameDesc lastGameDesc = null;

		//lists of game assets in specific sort orders
		public static SortedList<string, ItemProto> protosInAlphaOrder;

		//power info members
		public static int currentPowerNetworkID = 1;

		#region stats
		//summary
		public static long maxNetworkPowerUsage;
		public static string maxNetworkPowerUsageString;
		public static long maxNetworkPowerUsageSansTransports;
		public static string maxNetworkPowerUsageSansTransportsString;
		public static long powerDemand;
		public static string powerDemandString;
		public static double consumerRatio;
		public static double generatorRatio;

		//Network detail
		//Generation stats
		public static long generatorOutputCapacity;
		public static int generatorCount;
		public static Dictionary<int, PowerGenData> curGenerationData = new Dictionary<int, PowerGenData>();

		//Exchange stats
		public static long exchangerMaxPower;
		public static Dictionary<int, PowerExcData> curDischExchangersPerType = new Dictionary<int, PowerExcData>();
		public static Dictionary<int, PowerExcData> curChargingExchangersPerType = new Dictionary<int, PowerExcData>();

		//Accumulator stats
		public static Dictionary<int, PowerAccData> curAccPerType = new Dictionary<int, PowerAccData>();
		public static long currentAccumulatedEnergy;

		//Consumer stats
		public static long consumerMaxPower;
		public static Dictionary<int, PowerConsData> curConsPerType = new Dictionary<int, PowerConsData>();
		#endregion

		public static void Init() {
			protosInAlphaOrder = new SortedList<string, ItemProto>();
			foreach (ItemProto item in LDB.items.dataArray) {
				protosInAlphaOrder.Add(item.name.Translate(), item);
			}
		}

		public void onGameData_GameTick(long time, GameData __instance) {
			if (IsDifferentGame())
				Reset();

			if (__instance == null || __instance.localPlanet == null)
				return;

			if (time - lastTime < (GameMain.tickPerSecI * refreshRateSec))
				return;

			lastTime = time;

			updatePowerUsage(__instance);
		}

		private void updatePowerUsage(GameData __instance) {
			PlanetFactory factory = __instance.localPlanet.factory;
			PowerSystem powerSystem = factory.powerSystem;
			
			PowerNetwork powerNetwork = powerSystem.netPool[currentPowerNetworkID];

			#region summary
			maxNetworkPowerUsage = powerNetwork.consumers.Sum(x => powerSystem.consumerPool[x].workEnergyPerTick * GameMain.tickPerSecI);
			maxNetworkPowerUsageSansTransports = powerNetwork.consumers.Where(x => factory.entityPool[powerSystem.consumerPool[x].entityId].stationId == 0).Sum(x => powerSystem.consumerPool[x].workEnergyPerTick * GameMain.tickPerSecI);
			consumerRatio = powerNetwork.consumerRatio;
			generatorRatio = powerNetwork.generaterRatio;
			powerDemand = powerNetwork.energyRequired * GameMain.tickPerSecI;
			#endregion

			#region exchangers
			exchangerMaxPower = powerNetwork.energyExchanged * -1 * GameMain.tickPerSecI;
			curDischExchangersPerType.Clear();
			curChargingExchangersPerType.Clear();
			foreach (int ex in powerNetwork.exchangers) {
				PowerExchangerComponent exchanger = powerSystem.excPool[ex];
				int excProtoID = factory.entityPool[exchanger.entityId].protoId;

				/* exchanger states:
				*   Charge: PowerExchangerComponent.state >= 1
				*   Idle: PowerExchangerComponent.state == 0
				*   Discharge: PowerExchangerComponent.state <= -1
				*/
				if (exchanger.state <= -1f) {
					if (!curDischExchangersPerType.ContainsKey(excProtoID))
						curDischExchangersPerType.Add(excProtoID, new PowerExcData());

					curDischExchangersPerType[excProtoID].ratedPower += exchanger.energyPerTick * GameMain.tickPerSecI;

					curDischExchangersPerType[excProtoID].maxPower += exchanger.capsCurrentTick * GameMain.tickPerSecI;

					curDischExchangersPerType[excProtoID].curPower += exchanger.currEnergyPerTick * GameMain.tickPerSecI;
				} else if (exchanger.state >= 1f) {
					if (!curChargingExchangersPerType.ContainsKey(excProtoID))
						curChargingExchangersPerType.Add(excProtoID, new PowerExcData());

					curChargingExchangersPerType[excProtoID].ratedPower += exchanger.energyPerTick * GameMain.tickPerSecI;
					maxNetworkPowerUsage += exchanger.energyPerTick * GameMain.tickPerSecI;
					maxNetworkPowerUsageSansTransports += exchanger.energyPerTick * GameMain.tickPerSecI;

					curChargingExchangersPerType[excProtoID].maxPower += exchanger.capsCurrentTick * GameMain.tickPerSecI;

					curChargingExchangersPerType[excProtoID].curPower += exchanger.currEnergyPerTick * GameMain.tickPerSecI;
					powerDemand += exchanger.currEnergyPerTick * GameMain.tickPerSecI;
				}
			}

			foreach (PowerExcData val in curDischExchangersPerType.Values)
				val.generateStrings();
			foreach (PowerExcData val in curChargingExchangersPerType.Values)
				val.generateStrings();
			#endregion

			#region generators
			generatorOutputCapacity = powerNetwork.energyCapacity * GameMain.tickPerSecI;
			generatorCount = powerNetwork.generators.Count;
			curGenerationData.Clear();
			foreach (int gen in powerNetwork.generators) {
				PowerGeneratorComponent generator = powerSystem.genPool[gen];
				int genProtoID = factory.entityPool[generator.entityId].protoId;

				//if a ray receiver (protoId 2208) is in Photon Generation mode, then its productId will not equal to 0
				//makes sense since it will then output product (critical photons), and therefore will not be generating power
				if (!(genProtoID == 2208 && generator.productId != 0)) {
					if (!curGenerationData.ContainsKey(genProtoID))
						curGenerationData.Add(genProtoID, new PowerGenData());

					curGenerationData[genProtoID].maxPower += generator.gamma ? maxGammaPower(generator) : (generator.capacityCurrentTick * GameMain.tickPerSecI);

					long genPower = (generator.gamma ? generator.MaxOutputCurrent_Gamma() : (long)Math.Round(generator.genEnergyPerTick * (double)generator.currentStrength)) * GameMain.tickPerSecI;
					curGenerationData[genProtoID].genPower += genPower;
					curGenerationData[genProtoID].curPower += powerDemand > exchangerMaxPower ? (long) Math.Round(genPower * powerNetwork.generaterRatio) : 0L;
				}
			}

			foreach (PowerGenData val in curGenerationData.Values)
				val.generateStrings();
			#endregion

			#region accumulators
			curAccPerType.Clear();
			currentAccumulatedEnergy = 0;
			foreach (int acc in powerNetwork.accumulators) {
				PowerAccumulatorComponent accumulator = powerSystem.accPool[acc];
				int accProtoID = factory.entityPool[accumulator.entityId].protoId;

				if (!curAccPerType.ContainsKey(accProtoID))
					curAccPerType.Add(accProtoID, new PowerAccData());

				curAccPerType[accProtoID].maxDiscPower += accumulator.outputEnergyPerTick * GameMain.tickPerSecI;
				curAccPerType[accProtoID].maxChgPower += accumulator.inputEnergyPerTick * GameMain.tickPerSecI;
				curAccPerType[accProtoID].curPower += accumulator.curPower * GameMain.tickPerSecI;
				curAccPerType[accProtoID].curPowerAbs += Math.Abs(accumulator.curPower * GameMain.tickPerSecI);
				currentAccumulatedEnergy += accumulator.curEnergy;
			}

			foreach (PowerAccData val in curAccPerType.Values)
				val.generateStrings();
			#endregion

			#region consumers
			curConsPerType.Clear();
			foreach (int cons in powerNetwork.consumers) {
				PowerConsumerComponent consumer = powerSystem.consumerPool[cons];
				int consProtoID = factory.entityPool[consumer.entityId].protoId;

				if (!curConsPerType.ContainsKey(consProtoID))
					curConsPerType.Add(consProtoID, new PowerConsData());
				curConsPerType[consProtoID].idlePower += consumer.idleEnergyPerTick * GameMain.tickPerSecI;
				curConsPerType[consProtoID].maxPower += consumer.workEnergyPerTick * GameMain.tickPerSecI;
				curConsPerType[consProtoID].currPower += consumer.requiredEnergy * GameMain.tickPerSecI;
			}

			foreach (Node node in powerNetwork.nodes) {
				int nodeID = node.id;
				PowerNodeComponent nodeC = powerSystem.nodePool[nodeID];
				int nodeProtoID = factory.entityPool[nodeC.entityId].protoId;

				if (nodeC.id == nodeID && nodeC.isCharger) {
					if (!curConsPerType.ContainsKey(nodeProtoID))
						curConsPerType.Add(nodeProtoID, new PowerConsData());
					curConsPerType[nodeProtoID].idlePower += nodeC.idleEnergyPerTick * GameMain.tickPerSecI;
					curConsPerType[nodeProtoID].maxPower += nodeC.workEnergyPerTick * GameMain.tickPerSecI;
					curConsPerType[nodeProtoID].currPower += nodeC.requiredEnergy * GameMain.tickPerSecI;
				}
			}

			foreach (PowerConsData val in curConsPerType.Values)
				val.generateStrings();
			#endregion

			maxNetworkPowerUsageString = HelperMethods.convertPowerToString(maxNetworkPowerUsage);
			maxNetworkPowerUsageSansTransportsString = HelperMethods.convertPowerToString(maxNetworkPowerUsageSansTransports);
			powerDemandString = HelperMethods.convertPowerToString(powerDemand);
		}

		public bool IsDifferentGame() {
			if (DSPGame.GameDesc != lastGameDesc) {
				lastGameDesc = DSPGame.GameDesc;
				return true;
			}
			return false;
		}

		public void Reset() {
			lastTime = 0;
		}

		public long maxGammaPower(PowerGeneratorComponent receiver) {
			if (!receiver.gamma)
				return -1;

			long maxPower = receiver.genEnergyPerTick * GameMain.tickPerSecI;
			maxPower = (long) (maxPower * 2.5f); //a ray receiver at 100% continuous power produces 2.5 times more power than at 0% (which is what genEnergyPerTick is assuming)
			maxPower *= ((receiver.catalystPoint <= 0) ? 1l : 2l); //For ray receivers, this is greater than 0 if there is a graviton lens in it. So ray receivers with graviton lenses run at double power
			maxPower *= ((receiver.productId <= 0) ? 1l : 5l); //if productId is greater than 0, then it's producing something (e.g. critical photons), and therefore run at 5x power
			return maxPower;
		}
	}
}
