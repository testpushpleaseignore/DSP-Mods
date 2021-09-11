
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;

namespace IcarusMovementFixes {

	[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
	[BepInProcess("DSPGame.exe")]
	public class IcarusMovementFixes : BaseUnityPlugin {
		public const string pluginGuid = "testpostpleaseignore.dsp.icarusmovementfixes";
		public const string pluginName = "Icarus_Movement_Fixes";
		public const string pluginVersion = "0.0.1";

		//CONSTANTS OVERRIDES
		private const float WALK_SLOWDOWN_MULTIPLIER = 50.0f;
		private const float DRIFT_SLOWDOWN_MULTIPLIER = 3.0f;

		private Harmony harmony;

		public static IcarusMovementFixes instance;

		internal static ManualLogSource logger;
		new internal static BepInEx.Configuration.ConfigFile Config;

		private void Awake() {
			logger = base.Logger;
			Config = base.Config;

			Assert.Null(instance, $"An instance of {nameof(IcarusMovementFixes)} has already been created!");
			instance = this;

			harmony = new Harmony(pluginGuid);

			try { harmony.PatchAll(typeof(IcarusMovementFixes)); }
			catch (Exception e) { Logger.LogError($"Harmony patching failed: {e.Message}"); }

			logger.LogInfo("Load Complete");
		}

		private void OnDestroy() {
			harmony.UnpatchSelf();
			instance = null;
		}

		void Update() {

		}

		void OnGUI() {
		}

		[HarmonyPrefix, HarmonyPatch(typeof(PlayerMove_Walk), "GameTick")]
		public static bool PlayerMove_Walk_GameTick_Prefix(long timei, PlayerMove_Walk __instance) {
			float num = 0.016666668f;
			__instance.AlwaysUpdate(num);
			if (__instance.player.movementState == EMovementState.Walk) {
				Vector3 vector = __instance.controller.mainCamera.transform.forward;
				Vector3 normalized = __instance.player.position.normalized;
				Vector3 normalized2 = Vector3.Cross(normalized, vector).normalized;
				vector = Vector3.Cross(normalized2, normalized);
				Vector3 vector2 = vector * __instance.controller.input0.y + normalized2 * __instance.controller.input0.x;
				Vector3 vector3 = __instance.player.position + normalized * 0.15f;
				float num2 = 0.35f;
				if (Physics.CheckSphere(vector3, num2, 15873)) {
					__instance.isGrounded = true;
				}
				else {
					__instance.isGrounded = false;
				}
				bool flag = __instance.UpdateJump();
				if ((__instance.controller.cmd.type == ECommand.Build && !VFInput._godModeMechaMove && !PlayerController.operationWhenBuild) || __instance.navigation.navigating) {
					vector2 = Vector3.zero;
					flag = false;
				}
				if (__instance.controller.actionBuild.blueprintMode > EBlueprintMode.None && !VFInput._godModeMechaMove) {
					vector2 = Vector3.zero;
					flag = false;
				}
				float num3 = __instance.controller.softLandingRecover;
				num3 *= num3;

				float num4 = 0.2f * num3;

				float num5 = __instance.player.mecha.walkSpeed;
				if (__instance.controller.overridePlayerSpeed > 0.01f) {
					num5 = __instance.controller.overridePlayerSpeed;
				}
				OrderNode currentOrder = __instance.player.currentOrder;
				if (currentOrder != null && !currentOrder.targetReached) {
					Vector3 vector4 = currentOrder.target - __instance.player.position;
					vector4 = Vector3.Cross(Vector3.Cross(normalized, vector4).normalized, normalized).normalized;
					__instance.rtsVelocity = Vector3.Slerp(__instance.rtsVelocity, vector4 * num5, num4);
				}
				else {
					__instance.rtsVelocity = Vector3.MoveTowards(__instance.rtsVelocity, Vector3.zero, num * 6f * num5);
				}
				if (__instance.navigation.navigating) {
					bool flag2 = false;
					__instance.navigation.DetermineLowVelocity(num5, num4, ref __instance.moveVelocity, ref flag2);
					if (flag2) {
						__instance.SwitchToFly();
					}
				}
				else {
					// Walk Slowdown changes made here
					if (__instance.isGrounded)
						num4 *= WALK_SLOWDOWN_MULTIPLIER;
					__instance.moveVelocity = Vector3.Slerp(__instance.moveVelocity, vector2 * num5, num4);
				}
				Vector3 vector5 = __instance.moveVelocity + __instance.rtsVelocity;
				if ((double)num3 > 0.9) {
					vector5 = Vector3.ClampMagnitude(vector5, num5);
				}
				__instance.UseWalkEnergy(ref vector5, __instance.mecha.walkPower * (double)num * (double)__instance.controller.softLandingRecover);
				Vector3 b = Vector3.Dot(vector5, normalized) * normalized;
				vector5 -= b;
				float num6 = __instance.controller.vertSpeed;
				float num7 = 0.6f;
				float num8 = 1f;
				num7 = Mathf.Lerp(1f, num7, Mathf.Clamp01(__instance.jumpedTime * 1f));
				if (num6 > 0f) {
					num6 *= num7;
				}
				else if (num6 < 0f) {
					num6 *= num8;
				}
				if (flag && __instance.UseJumpEnergy()) {
					num6 += __instance.mecha.jumpSpeed;
				}
				if (__instance.isGrounded && vector5.sqrMagnitude < 0.005f && __instance.controller.input0.sqrMagnitude == 0f) {
					__instance.controller.SleepRigidBody();
				}
				__instance.controller.velocity = num6 * normalized + vector5;
				if (vector2.sqrMagnitude > 0.25f) {
					__instance.controller.turning = Vector3.SignedAngle(vector5, vector2, normalized);
				}
				else {
					__instance.controller.turning = 0f;
				}
				__instance.controller.actionDrift.rtsVelocity = __instance.rtsVelocity;
				__instance.controller.actionDrift.moveVelocity = __instance.moveVelocity;
				__instance.controller.actionFly.rtsVelocity = __instance.rtsVelocity;
				__instance.controller.actionFly.moveVelocity = __instance.moveVelocity;
			}

			return false;
		}

		[HarmonyPrefix, HarmonyPatch(typeof(PlayerMove_Drift), "GameTick")]
		public static bool PlayerMove_Drift_GameTick_Prefix(long timei, PlayerMove_Drift __instance) {
			float num = 0.016666668f;
			if (__instance.player.movementState == EMovementState.Drift) {
				Vector3 vector = __instance.controller.mainCamera.transform.forward;
				Vector3 normalized = __instance.player.position.normalized;
				Vector3 normalized2 = Vector3.Cross(normalized, vector).normalized;
				vector = Vector3.Cross(normalized2, normalized);
				Vector3 vector2 = vector * __instance.controller.input0.y + normalized2 * __instance.controller.input0.x;
				if ((__instance.controller.cmd.type == ECommand.Build && !VFInput._godModeMechaMove && !PlayerController.operationWhenBuild) || __instance.navigation.navigating) {
					vector2 = Vector3.zero;
				}
				if (__instance.controller.actionBuild.blueprintMode > EBlueprintMode.None && !VFInput._godModeMechaMove) {
					vector2 = Vector3.zero;
				}
				float realRadius = __instance.player.planetData.realRadius;
				float num2 = Mathf.Max(__instance.player.position.magnitude, realRadius * 0.9f);
				__instance.currentAltitude = num2 - realRadius;
				__instance.targetAltitude = 1f;
				float num3 = __instance.targetAltitude - __instance.currentAltitude;
				__instance.verticalThrusterForce = 0f;
				float num4 = Mathf.Clamp(num3 * 0.5f, -10f, 10f) * 100f + (float)__instance.controller.universalGravity.magnitude;
				num4 = Mathf.Max(0f, num4);
				__instance.verticalThrusterForce += num4;
				__instance.UseThrustEnergy(ref __instance.verticalThrusterForce, 0.016666666666666666);
				float num5 = (float)(Math.Sin(GlobalObject.timeSinceStart * 2.0) * 0.1 + 1.0);
				if (Mathf.Abs(__instance.verticalThrusterForce) > 0.001f) {
					__instance.controller.AddLocalForce(normalized * (__instance.verticalThrusterForce * num5));
				}
				__instance.UpdateJump();
				float num6 = __instance.controller.softLandingRecover;
				num6 *= num6;
				float num7 = 0.055f * num6;
				float walkSpeed = __instance.player.mecha.walkSpeed;
				OrderNode currentOrder = __instance.player.currentOrder;
				if (currentOrder != null && !currentOrder.targetReached) {
					Vector3 vector3 = currentOrder.target - __instance.player.position;
					vector3 = Vector3.Cross(Vector3.Cross(normalized, vector3).normalized, normalized).normalized;
					__instance.rtsVelocity = Vector3.Slerp(__instance.rtsVelocity, vector3 * walkSpeed, num7);
				}
				else {
					__instance.rtsVelocity = Vector3.MoveTowards(__instance.rtsVelocity, Vector3.zero, num * 6f * walkSpeed);
				}
				if (__instance.navigation.navigating) {
					bool flag = false;
					__instance.navigation.DetermineLowVelocity(walkSpeed, num7, ref __instance.moveVelocity, ref flag);
					if (flag) {
						__instance.SwitchToFly();
					}
				}
				else {
					//Drift slowdown changes made here
					//__instance.moveVelocity = Vector3.Slerp(__instance.moveVelocity, vector2 * walkSpeed, num7);
					__instance.moveVelocity = Vector3.Slerp(__instance.moveVelocity, vector2 * walkSpeed, num7 * DRIFT_SLOWDOWN_MULTIPLIER);
				}
				Vector3 vector4 = __instance.moveVelocity + __instance.rtsVelocity;
				if ((double)num6 > 0.9) {
					vector4 = Vector3.ClampMagnitude(vector4, walkSpeed);
				}
				__instance.UseDriftEnergy(ref vector4, __instance.mecha.walkPower * (double)num * (double)__instance.controller.softLandingRecover);
				Vector3 b = Vector3.Dot(vector4, normalized) * normalized;
				vector4 -= b;
				float num8 = __instance.controller.vertSpeed;
				float num9 = Mathf.Lerp(0.95f, 0.8f, Mathf.Abs(num3) * 0.3f);
				float num10 = num9;
				num9 = Mathf.Lerp(1f, num9, Mathf.Clamp01(__instance.verticalThrusterForce));
				num10 = Mathf.Lerp(1f, num10, Mathf.Clamp01(__instance.verticalThrusterForce) * Mathf.Clamp01((float)(__instance.mecha.coreEnergy - 5000.0) * 0.0001f));
				if (num8 > 0f) {
					num8 *= num9;
				}
				else if (num8 < 0f) {
					num8 *= num10;
				}
				__instance.controller.velocity = num8 * normalized + vector4;
				if (vector2.sqrMagnitude > 0.25f) {
					__instance.controller.turning = Vector3.SignedAngle(vector4, vector2, normalized);
				}
				else {
					__instance.controller.turning = 0f;
				}
				if (__instance.mecha.coreEnergy < 10000.0) {
					__instance.controller.movementStateInFrame = EMovementState.Walk;
				}
				__instance.controller.actionWalk.rtsVelocity = __instance.rtsVelocity;
				__instance.controller.actionWalk.moveVelocity = __instance.moveVelocity;
				__instance.controller.actionFly.rtsVelocity = __instance.rtsVelocity;
				__instance.controller.actionFly.moveVelocity = __instance.moveVelocity;
			}
			DetermineDrift(__instance);

			return false;
		}

		private static void DetermineDrift(PlayerMove_Drift __instance) {
			PlanetData planetData = __instance.player.planetData;
			bool flag = false;
			if (planetData != null) {
				float realRadius = planetData.realRadius;
				float num = Mathf.Max(__instance.player.position.magnitude, realRadius * 0.9f);
				__instance.currentAltitude = num - realRadius;
				Vector3 normalized = __instance.player.position.normalized;
				Vector3 origin = __instance.player.position + normalized * 10f;
				Vector3 direction = -normalized;
				float num2 = 0f;
				float num3 = 0f;
				bool flag2 = false;
				float num4 = 0f;
				RaycastHit raycastHit;
				if (Physics.Raycast(new Ray(origin, direction), out raycastHit, 30f, 8704, QueryTriggerInteraction.Collide)) {
					num2 = raycastHit.distance;
					num4 = raycastHit.point.magnitude - planetData.realRadius;
				}
				else {
					flag2 = true;
				}
				if (Physics.Raycast(new Ray(origin, direction), out raycastHit, 30f, 16, QueryTriggerInteraction.Collide)) {
					num3 = raycastHit.distance;
				}
				else {
					flag2 = true;
				}
				__instance.inWater = false;
				float num5 = 0f;
				if (planetData != null) {
					num5 = planetData.waterHeight;
				}
				if (!flag2 && __instance.currentAltitude > -2.3f + num5) {
					if (num2 - num3 > 0.7f && __instance.currentAltitude < -0.6f) {
						__instance.inWater = true;
					}
					if (__instance.player.movementState < EMovementState.Fly) {
						flag = (num2 - num3 > 0.4f || num4 < -0.8f);
					}
				}
				else {
					flag = false;
				}
			}
			if (__instance.player.movementState == EMovementState.Walk || __instance.player.movementState == EMovementState.Drift) {
				if (flag && __instance.player.movementState != EMovementState.Drift && __instance.mecha.coreEnergy > 500000.0 && GameMain.gameTime > 2.0) {
					__instance.controller.movementStateInFrame = EMovementState.Drift;
					__instance.driftDownPrepareTime = 0f;
				}
				if (__instance.player.movementState == EMovementState.Drift) {
					__instance.driftDownPrepareTime += 0.01666667f;
					if (flag && __instance.driftDownPrepareTime > 0.3f) {
						__instance.driftDownPrepareTime = 0.3f;
					}
				}
				if (!flag && __instance.driftDownPrepareTime > 0.8f) {
					__instance.controller.movementStateInFrame = EMovementState.Walk;
					__instance.driftDownPrepareTime = 0f;
					return;
				}
			}
			else {
				__instance.driftDownPrepareTime = 0f;
			}
		}
	}
}
