/// <summary>
/// Maps upgrade IDs (strings from PlayerUpgradeData.upgradeId) to
/// PlayerUpgrade behaviour instances.
///
/// IMPORTANT: Every behaviour upgrade you create needs one line here.
/// Pure stat upgrades (isPureStatUpgrade = true) do NOT need an entry —
/// their stats are applied directly by the UI without going through this factory.
///
/// Pattern: case "Your_Upgrade_Id": return new YourUpgradeClass();
///
/// The ID must exactly match PlayerUpgradeData.upgradeId AND PlayerUpgrade.Id.
/// </summary>
public static class UpgradeFactory
{
    public static PlayerUpgrade Create(string upgradeId)
    {
        switch (upgradeId)
        {
            // ── Bow / Arrow ──────────────────────────────────────────
          //  case "Bow_ExtraArrow": return new UpgradeBowExtraArrow();
            case "Arrow_Burn": return new UpgradeBurningArrow();
            case "Arrow_Pierce": return new UpgradeArrowPierce();
            case "Status_Burn": return new UpgradeStatusOnHitBurn();
            case "Status_Freeze": return new UpgradeStatusOnHitFreeze();
            case "Status_Holy": return new UpgradeStatusOnHitHoly();
            case "Status_Shock": return new UpgradeStatusOnHitShock();
            case "Enemy_Explosion": return new UpgradeEnemyExplosion();
            case "SoulBonus": return new UpgradeSoulBonus();
            case "Immunity": return new UpgradeImmunity();

            // ── Arrow Trails ─────────────────────────────────────────
            //  case "Arrow_Trail_Burn": return new UpgradeArrowTrailBurn();
            //  case "Arrow_Trail_Freeze": return new UpgradeArrowTrailFreeze();
            //  case "Arrow_Trail_Holy": return new UpgradeArrowTrailHoly();
            //  case "Arrow_Trail_Shock": return new UpgradeArrowTrailShock();

            // ── Status on Damage ─────────────────────────────────────
            //  case "Status_Burn": return new UpgradeStatusOnHitBurn();
            //  case "Status_Freeze": return new UpgradeStatusOnHitFreeze();
            //  case "Status_Holy": return new UpgradeStatusOnHitHoly();
            //  case "Status_Shock": return new UpgradeStatusOnHitShock();

            // ── Dash ─────────────────────────────────────────────────
            //   case "Dash_Pulse": return new UpgradeDashPulse();
            //   case "Dash_Trail_Burn": return new UpgradeDashTrailBurn();
            //   case "Dash_Trail_Freeze": return new UpgradeDashTrailFreeze();
            //   case "Dash_Trail_Holy": return new UpgradeDashTrailHoly();
            //   case "Dash_Trail_Shock": return new UpgradeDashTrailShock();

            // ── Hover ────────────────────────────────────────────────
            //  case "Hover_Regen": return new Upgrade_Hover_Regen();
            //  case "Hover_Damage": return new UpgradeHoverDamage();

            // ── Wall Slide ───────────────────────────────────────────
            //  case "WallSlide_Damage": return new Upgrade_WallSlide_Damage();

            // ── Passive ──────────────────────────────────────────────
            //   case "Enemy_Explosion": return new Upgrade_Enemy_Explosion();
            //   case "HomingProjectile": return new UpgradeHomingProjectile();
            //   case "Immunity": return new UpgradeImmunity();
            //    case "SoulBonus": return new UpgradeSoulBonus();

            default:
                UnityEngine.Debug.LogWarning(
                    $"[UpgradeFactory] Unknown upgrade id: '{upgradeId}'. " +
                    $"Add a case for it in UpgradeFactory.cs.");
                return null;
        }
    }
}