using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class AttackGoal : Goal
{
    private class WeaponMoveResults
    {
        public float timeEstimate = 99999;
        public AIAction moveAction = null;
        public Vector2 targetPos = new Vector2();
        public WeaponPart weapon;
    }

    public AttackGoal(AITankController controller) : base(controller) {}

    public override void ReInit() {
        // Do nothing.
    }

    private List<WeaponPart> weaponsThatCanHit = new List<WeaponPart>();

    public override void UpdateInsistence() {
        Insistence = 0;
        weaponsThatCanHit.Clear();

        Tank aiTank = controller.SelfTank;

        Tank targetTank = controller.TargetTank;

        foreach (WeaponPart weapon in aiTank.Turret.GetAllWeapons()) {
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
                break;
            }
        }

        if (weaponsThatCanHit.Count > 0) {
            Insistence = 100;
        }
    }

    public override AIAction[] CalcActionsToPerform() {
        List<AIAction> actions = new List<AIAction>();

        foreach (WeaponPart weapon in weaponsThatCanHit) {
            actions.Add(new FireWeaponAction(weapon.TurretIdx, controller));
        }

        return actions.ToArray();
    }
}
