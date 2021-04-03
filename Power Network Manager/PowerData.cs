using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerNetworkManager.UI {
	public class PowerData {
        static long lastTime = 0;
        const long refreshRateSec = 2;

        public GameDesc lastGameDesc = null;

        //power info members
        public static int currentPowerNetworkID = 1;

		#region stats
        //summary
		public static long maxNetworkPowerUsage;
        public static long maxNetworkPowerUsageSansTransports;
        public static double consumerRatio;
        public static double generatorRatio;
        public static long powerDemand;

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

        //Consumer stats
        public static long consumerMaxPower;
        public static Dictionary<int, PowerConsData> curConsPerType = new Dictionary<int, PowerConsData>();
        #endregion

        public void onGameData_GameTick(long time, GameData __instance) {
            if (IsDifferentGame()) {
                Reset();
            }

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
                    curDischExchangersPerType[excProtoID].maxPower += exchanger.capacityCurrentTick * GameMain.tickPerSecI;
                    curDischExchangersPerType[excProtoID].curPower += exchanger.curEnergyPerTick * GameMain.tickPerSecI;
                } else if (exchanger.state >= 1f) {
                    if (!curChargingExchangersPerType.ContainsKey(excProtoID))
                        curChargingExchangersPerType.Add(excProtoID, new PowerExcData());
                    curChargingExchangersPerType[excProtoID].maxPower += exchanger.capacityCurrentTick * GameMain.tickPerSecI;
                    curChargingExchangersPerType[excProtoID].curPower += exchanger.curEnergyPerTick * GameMain.tickPerSecI;
                }
            }
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
                    curGenerationData[genProtoID].maxPower += generator.capacityCurrentTick * GameMain.tickPerSecI;
                    long genPower = (long)Math.Round(generator.genEnergyPerTick * (double)generator.currentStrength) * GameMain.tickPerSecI;
                    curGenerationData[genProtoID].genPower += genPower;
                    curGenerationData[genProtoID].curPower += powerDemand > exchangerMaxPower ? (long) Math.Round(genPower * powerNetwork.generaterRatio) : 0L;
                }
            }
			#endregion

			#region accumulators
			curAccPerType.Clear();
            foreach (int acc in powerNetwork.accumulators) {
                PowerAccumulatorComponent accumulator = powerSystem.accPool[acc];
                int accProtoID = factory.entityPool[accumulator.entityId].protoId;

                if (!curAccPerType.ContainsKey(accProtoID))
                    curAccPerType.Add(accProtoID, new PowerAccData());

                curAccPerType[accProtoID].maxDiscPower += accumulator.outputEnergyPerTick * GameMain.tickPerSecI;
                curAccPerType[accProtoID].maxChgPower += accumulator.inputEnergyPerTick * GameMain.tickPerSecI;
                curAccPerType[accProtoID].curPower += accumulator.curPower * GameMain.tickPerSecI;
            }
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
            #endregion
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
