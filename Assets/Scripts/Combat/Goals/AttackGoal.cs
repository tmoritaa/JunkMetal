﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AttackGoal : Goal
{
    public AttackGoal(AITankController controller) : base(controller) {}

    public override void ReInit() {
        // Do nothing.
    }

    private List<WeaponPart> weaponsThatCanHit = new List<WeaponPart>();

    public override void UpdateInsistence() {
        Insistence = 0;
        weaponsThatCanHit.Clear();

        if (controller.PrevManeuverBehaviour == ManeuverGoal.Behaviour.GoingforIt) {
            Tank aiTank = controller.SelfTank;

            Tank targetTank = controller.TargetTank;

            foreach (WeaponPart weapon in aiTank.Hull.GetAllWeapons()) {
                if (weapon.CalcTimeToReloaded() > 0) {
                    continue;
                }

                if (weapon.Schematic.BulletType != Bullet.BulletTypes.MissileCluster) {
                    Vector2 targetPos = AIUtility.CalculateTargetPosWithWeapon(weapon.Schematic.ShootImpulse, weapon.CalculateFirePos(), aiTank.transform.position, targetTank.transform.position, targetTank.Body.velocity);
                    Vector2 curFireVec = weapon.CalculateFireVec();
                    Ray ray = new Ray(weapon.CalculateFirePos(), curFireVec);
                    float shortestDist = Vector3.Cross(ray.direction, (Vector3)(targetPos) - ray.origin).magnitude;
                    bool canHitIfFired = shortestDist < targetTank.Hull.Schematic.Size.x / 2f;

                    Vector2 targetVec = targetPos - weapon.CalculateFirePos();

                    bool fireVecFacingTarget = Vector2.Angle(curFireVec, targetVec) < 90f;
                    bool inRange = targetVec.magnitude < weapon.Schematic.Range;
                    if (inRange && canHitIfFired && fireVecFacingTarget) {
                        weaponsThatCanHit.Add(weapon);
                    }
                } else {
                    float distFromTarget = (targetTank.transform.position - aiTank.transform.position).magnitude;

                    float checkDist = (float)weapon.Schematic.BulletInfos["shoot_impulse"] * (float)weapon.Schematic.BulletInfos["missile_shoot_time"];
                    const int WallBit = 8;
                    const int LayerMask = 1 << WallBit;

                    RaycastHit2D hit = Physics2D.Raycast(aiTank.transform.position, weapon.CalculateFireVec(), checkDist, LayerMask);
                    
                    if (hit.collider == null && distFromTarget <= weapon.Schematic.Range) {
                        weaponsThatCanHit.Add(weapon);
                    }
                }
            }

            if (weaponsThatCanHit.Count > 0) {
                Insistence = 100;
            }
        }
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        foreach (WeaponPart weapon in weaponsThatCanHit) {
            actions.Add(new FireWeaponAction(weapon.EquipIdx, controller));
        }

        return actions.ToArray();
    }
}
