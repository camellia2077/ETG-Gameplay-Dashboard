// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class KeyboardAimAssistTargetSelector
    {
        public bool TrySelectTarget(
            PlayerController player,
            Vector3 rawAimPoint,
            float assistDegrees,
            out Vector2 selectedAimPoint)
        {
            selectedAimPoint = rawAimPoint.XY();
            if (GameManager.Options == null || GameManager.Options.controllerAimAssistMultiplier <= 0f ||
                player.CurrentGun == null || player.CurrentRoom == null || player.specRigidbody == null ||
                assistDegrees <= 0f)
            {
                return false;
            }

            List<IAutoAimTarget> targets = player.CurrentRoom.GetAutoAimTargets();
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            Projectile projectile = player.CurrentGun.DefaultModule != null
                ? player.CurrentGun.DefaultModule.GetCurrentProjectile()
                : null;
            float projectileSpeed = projectile != null ? projectile.baseData.speed : float.MaxValue;
            if (projectileSpeed <= 0f)
            {
                projectileSpeed = float.MaxValue;
            }

            Vector2 origin = player.specRigidbody.UnitCenter;
            Vector2 rawDirection = rawAimPoint.XY() - origin;
            if (rawDirection == Vector2.zero)
            {
                return false;
            }

            float rawAngle = rawDirection.ToAngle();
            IAutoAimTarget bestTarget = null;
            Vector2 bestAimPoint = selectedAimPoint;
            float bestAngle = assistDegrees;
            for (int index = 0; index < targets.Count; index++)
            {
                IAutoAimTarget target = targets[index];
                if (target == null || !target.IsValid)
                {
                    continue;
                }

                Vector2 targetCenter = target.AimCenter;
                if (GameManager.Instance.MainCameraController != null &&
                    !GameManager.Instance.MainCameraController.PointIsVisible(targetCenter, 0.05f))
                {
                    continue;
                }

                float leadTime = Vector2.Distance(origin, targetCenter) / projectileSpeed;
                Vector2 predictedPoint = targetCenter + target.Velocity * leadTime;
                Vector2 targetDirection = predictedPoint - origin;
                if (targetDirection == Vector2.zero)
                {
                    continue;
                }

                float angle = Mathf.Abs(BraveMathCollege.ClampAngle180(targetDirection.ToAngle() - rawAngle));
                if (angle >= bestAngle || PhysicsEngine.Instance == null)
                {
                    continue;
                }

                float rayDistance = targetDirection.magnitude - 2f;
                if (rayDistance > 0f)
                {
                    RaycastResult raycast;
                    bool blocked = PhysicsEngine.Instance.Raycast(
                        origin,
                        targetDirection.normalized,
                        rayDistance,
                        out raycast,
                        collideWithTiles: true,
                        collideWithRigidbodies: false);
                    RaycastResult.Pool.Free(ref raycast);
                    if (blocked)
                    {
                        continue;
                    }
                }

                bestTarget = target;
                bestAimPoint = predictedPoint;
                bestAngle = angle;
            }

            if (bestTarget == null)
            {
                return false;
            }

            selectedAimPoint = bestAimPoint;
            return true;
        }
    }
}
