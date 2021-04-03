using System;
using BepInEx;
using BepInEx.Logging;
using xiaoye97;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using System.Collections.Generic;

namespace SuperBeltFix {
    [BepInDependency("me.xiaoye97.plugin.Dyson.LDBTool", "1.7.0")]
    [BepInDependency("me.xiaoye97.plugin.Dyson.SuperBelt", "1.3.0")]
    [BepInPlugin("testpostplease.ignore.dsp.SuperBeltFix", "SuperBeltFix", "0.0.1")]
    public class SuperBeltFix : BaseUnityPlugin {

        static ManualLogSource logger;

        static Dictionary<int, float> beltPathLastUpdateTime = new Dictionary<int, float>();

        void Start() {
            logger = base.Logger;

            Harmony.CreateAndPatchAll(typeof(SuperBeltFix));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), "Begin")]
        public static void Begin() {
            
        }

        #region Speed bug fix
        [HarmonyBefore(new string[] { "me.xiaoye97.plugin.Dyson.SuperBelt" }), HarmonyPrefix, HarmonyPatch(typeof(CargoPath), "Update")]
        public static bool CargoPathPatch(CargoPath __instance) {
            var _this = __instance;

            #region DEBUG code todo remove
            
           if (_this.id == 3) {
               if (!beltPathLastUpdateTime.ContainsKey(_this.id) || Time.realtimeSinceStartup - beltPathLastUpdateTime[_this.id] > 1.0f) {
                   try {
                       PlanetFactory factory = GameMain.mainPlayer.factory;
                       CargoTraffic cargoTraffic = factory.cargoTraffic;

                       logger.LogDebug($"planet:{cargoTraffic.planet.name};  id:{_this.id};  belt count:{_this.belts.Count};  closed:{_this.closed};  Num Cargo:{_this.cargoContainer.cargoPool.Length};  buffer len: {_this.buffer.Length}");
                       foreach (int belt in _this.belts) {
                           logger.LogDebug($"    belt:{cargoTraffic.beltPool[belt].id}");
                       }
                   } catch (Exception ex) {

                   }

                    beltPathLastUpdateTime[_this.id] = Time.realtimeSinceStartup;
               }
           }

            #endregion

            if (_this.outputPath != null) {
                int Sign = _this.bufferLength - 5 - 1;
                if (_this.buffer[Sign] == 250) {
                    int cargoId = (int)(_this.buffer[Sign + 1] - 1 + (_this.buffer[Sign + 2] - 1) * 100) + (int)(_this.buffer[Sign + 3] - 1) * 10000 + (int)(_this.buffer[Sign + 4] - 1) * 1000000;
                    if (_this.closed) // 线路闭合
                    {
                        if (_this.outputPath.TryInsertCargoNoSqueeze(_this.outputIndex, cargoId)) {
                            Array.Clear(_this.buffer, Sign - 4, 10);
                            _this.updateLen = _this.bufferLength;
                        }
                    }
                    else if (_this.outputPath.TryInsertCargo(_this.outputIndex, cargoId)) {
                        Array.Clear(_this.buffer, Sign - 4, 10);
                        _this.updateLen = _this.bufferLength;
                    }
                }
            }
            else if (_this.bufferLength <= 10) return false;
            if (!_this.closed) {
                int Rear = _this.bufferLength - 1;
                if (_this.buffer[Rear] != 255 && _this.buffer[Rear] != 0) {
                    Debug.Log($"传送带末尾异常! {_this.id} {Rear}");
                    // 清空异常数据
                    for (int i = Rear; i >= 0; i--) {
                        if (_this.buffer[i] == 246) {
                            _this.buffer[i] = 0;
                            break;
                        }
                        _this.buffer[i] = 0;
                    }
                    _this.updateLen = _this.bufferLength;
                }
            }
            for (int j = _this.updateLen - 1; j >= 0; j--) {
                if (_this.buffer[j] == 0) break;
                _this.updateLen--;
            }
            if (_this.updateLen == 0) return false;
            int len = _this.updateLen;
            for (int k = _this.chunkCount - 1; k >= 0; k--) {
                int begin = _this.chunks[k * 3];
                int speed = _this.chunks[k * 3 + 2];
                if (begin < len) {
                    if (_this.buffer[begin] != 0) {
                        for (int l = begin - 5; l < begin + 4; l++) {
                            if (l >= 0) {
                                if (_this.buffer[l] == 250) {
                                    if (l < begin) begin = l + 5 + 1;
                                    else begin = l - 4;
                                    break;
                                }
                            }
                        }
                    }
                    if (speed > 10) // 如果速度大于10，则进行长度判断处理,防止越界
                    {
                        for (int i = 10; i <= speed; i++) {
                            if (begin + i + 10 >= _this.bufferLength) // 即将离开传送带尽头
                            {
                                speed = i;
                                break;
                            }
                            else {
                                if (_this.buffer[begin + i] != 0) // 速度范围内不为空
                                {
                                    speed = i;
                                    break;
                                }
                            }
                        }
                        if (speed < 10) {
                            speed = 10; // 如果速度减速到安全速度以内，设定为安全速度
                        }
                    }
                    int m = 0;
                    while (m < speed) {
                        int num8 = len - begin;
                        if (num8 < 10) // 移动结束
                        {
                            break;
                        }
                        int num9 = 0;
                        for (int n = 0; n < speed - m; n++) {
                            if (_this.buffer[len - 1 - n] != 0) break;
                            num9++;
                        }
                        if (num9 > 0) {
                            Array.Copy(_this.buffer, begin, _this.buffer, begin + num9, num8 - num9);
                            Array.Clear(_this.buffer, begin, num9);
                            m += num9;
                        }
                        for (int num11 = len - 1; num11 >= 0; num11--) {
                            if (_this.buffer[num11] == 0) break;
                            len--;
                        }
                    }
                    int num12 = begin + ((m != 0) ? m : 1);
                    if (len > num12) {
                        len = num12;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}