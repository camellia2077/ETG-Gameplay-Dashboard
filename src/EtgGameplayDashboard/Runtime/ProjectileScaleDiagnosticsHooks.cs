// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal static class ProjectileScaleDiagnosticsHooks
    {
        private static ManualLogSource _logger;
        private static int _loggedProjectileCount;
        private static readonly Dictionary<Projectile, float> OriginalSpriteLocalZ = new Dictionary<Projectile, float>();

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            _logger = logger;
            _loggedProjectileCount = 0;
            OriginalSpriteLocalZ.Clear();
            System.Reflection.MethodInfo startTarget = AccessTools.Method(typeof(Projectile), "Start", System.Type.EmptyTypes);
            System.Reflection.MethodInfo target = AccessTools.Method(typeof(Projectile), "PostprocessPlayerBullet", System.Type.EmptyTypes);
            System.Reflection.MethodInfo startPrefix = AccessTools.Method(typeof(ProjectileScaleDiagnosticsHooks), "ProjectileStartPrefix");
            System.Reflection.MethodInfo startPostfix = AccessTools.Method(typeof(ProjectileScaleDiagnosticsHooks), "ProjectileStartPostfix");
            System.Reflection.MethodInfo patch = AccessTools.Method(typeof(ProjectileScaleDiagnosticsHooks), "ProjectileOnSpawnedPostfix");
            if (startTarget == null || target == null || startPrefix == null || startPostfix == null || patch == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(EtgGameplayDashboardLog.Init("Projectile scale diagnostics hook skipped: Projectile.Start or Projectile.PostprocessPlayerBullet was unavailable."));
                }

                return;
            }

            harmony.Patch(startTarget, prefix: new HarmonyMethod(startPrefix), postfix: new HarmonyMethod(startPostfix));
            harmony.Patch(target, postfix: new HarmonyMethod(patch));
            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Init("Projectile scale diagnostics hook ready: Projectile.Start scale-depth fix and Projectile.PostprocessPlayerBullet diagnostics."));
            }
        }

        public static void Reset()
        {
            _logger = null;
            _loggedProjectileCount = 0;
            OriginalSpriteLocalZ.Clear();
        }

        private static void ProjectileStartPrefix(Projectile __instance)
        {
            if (__instance == null || __instance.sprite == null)
            {
                return;
            }

            OriginalSpriteLocalZ[__instance] = __instance.sprite.transform.localPosition.z;
        }

        private static void ProjectileStartPostfix(Projectile __instance)
        {
            float originalLocalZ;
            if (__instance == null || !OriginalSpriteLocalZ.TryGetValue(__instance, out originalLocalZ))
            {
                return;
            }

            OriginalSpriteLocalZ.Remove(__instance);
            if (__instance.Owner == null || !(__instance.Owner is PlayerController) || __instance.sprite == null)
            {
                return;
            }

            PlayerController player = __instance.Owner as PlayerController;
            if (player.stats == null || Mathf.Approximately(player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale), 1f))
            {
                return;
            }

            Vector3 localPosition = __instance.sprite.transform.localPosition;
            if (!Mathf.Approximately(localPosition.z, originalLocalZ))
            {
                localPosition.z = originalLocalZ;
                __instance.sprite.transform.localPosition = localPosition;
                __instance.sprite.UpdateZDepth();
                if (_logger != null)
                {
                    _logger.LogInfo(EtgGameplayDashboardLog.Damage("ProjectileScaleDiagnostic fixed sprite local Z for " + __instance.name + ": restored " + originalLocalZ.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ", actualLocalPosition=" + __instance.sprite.transform.localPosition + "."));
                }
            }
        }

        private static void ProjectileOnSpawnedPostfix(Projectile __instance)
        {
            if (__instance == null || __instance.Owner == null || !(__instance.Owner is PlayerController))
            {
                return;
            }

            PlayerController player = __instance.Owner as PlayerController;
            if (player.stats == null)
            {
                return;
            }

            float bulletScale = player.stats.GetStatValue(PlayerStats.StatType.PlayerBulletScale);
            if (Mathf.Approximately(bulletScale, 1f))
            {
                return;
            }

            if (_loggedProjectileCount >= 300)
            {
                if (_loggedProjectileCount == 300 && _logger != null)
                {
                    _logger.LogWarning(EtgGameplayDashboardLog.Damage("Projectile scale diagnostics reached the 300-entry cap; further projectile spawn logs are suppressed until reset."));
                }

                _loggedProjectileCount++;
                return;
            }

            _loggedProjectileCount++;
            tk2dBaseSprite sprite = __instance.sprite;
            tk2dSpriteAnimator animator = __instance.GetComponentInChildren<tk2dSpriteAnimator>();
            string sourceGun = __instance.PossibleSourceGun != null
                ? __instance.PossibleSourceGun.name + " (PickupId=" + __instance.PossibleSourceGun.PickupObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"
                : "<none>";
            string message =
                "ProjectileScaleDiagnostic #" + _loggedProjectileCount.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ": Player=" + player.name +
                ", Gun=" + sourceGun +
                ", Projectile=" + __instance.name +
                ", GameObjectActive=" + __instance.gameObject.activeInHierarchy +
                ", ComponentEnabled=" + __instance.enabled +
                ", StatScale=" + bulletScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", AdditionalScale=" + __instance.AdditionalScaleMultiplier.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) +
                ", Sprite=" + (sprite != null ? "present" : "null") +
                ", SpriteActive=" + (sprite != null && sprite.gameObject.activeInHierarchy) +
                ", SpriteEnabled=" + (sprite != null && sprite.enabled) +
                ", SpriteRenderer=" + (sprite != null && sprite.renderer != null ? "present" : "null") +
                ", SpriteRendererEnabled=" + (sprite != null && sprite.renderer != null && sprite.renderer.enabled) +
                ", RendererVisible=" + (sprite != null && sprite.renderer != null && sprite.renderer.isVisible) +
                ", SpriteId=" + (sprite != null ? sprite.spriteId.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1") +
                ", SpriteScale=" + (sprite != null ? sprite.scale.ToString() : "<null>") +
                ", SpriteLocalPosition=" + (sprite != null ? sprite.transform.localPosition.ToString() : "<null>") +
                ", SpriteWorldPosition=" + (sprite != null ? sprite.transform.position.ToString() : "<null>") +
                ", SpriteBounds=" + (sprite != null ? sprite.GetBounds().ToString() : "<null>") +
                ", HeightOffGround=" + (sprite != null ? sprite.HeightOffGround.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "<null>") +
                ", Animator=" + (animator != null ? "present" : "null") +
                ", AnimatorPlaying=" + (animator != null && animator.Playing) +
                ", AnimatorClip=" + (animator != null && animator.CurrentClip != null ? animator.CurrentClip.name : "<null>") +
                ", Rigidbody=" + (__instance.specRigidbody != null ? "present" : "null") +
                ", RigidbodyEnabled=" + (__instance.specRigidbody != null && __instance.specRigidbody.enabled) +
                ", PixelColliders=" + (__instance.specRigidbody != null && __instance.specRigidbody.PixelColliders != null ? __instance.specRigidbody.PixelColliders.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "0") + ".";
            if (_logger != null)
            {
                _logger.LogInfo(EtgGameplayDashboardLog.Damage(message));
            }
        }
    }
}
