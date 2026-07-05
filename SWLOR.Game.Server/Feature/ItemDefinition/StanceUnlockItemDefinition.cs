using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Stance unlock items - form holocrons (FS) and combat datacrons (Standard), the
    /// Phase-2 event loot fabricated by the WorldEvent service. Using one unlocks the
    /// higher levels (4-6) of the stance perk written on the item; the levels themselves
    /// are still bought with SP behind their Phase-2 skill gates.
    /// </summary>
    public class StanceUnlockItemDefinition : IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            StanceUnlock();

            return _builder.Build();
        }

        private void StanceUnlock()
        {
            _builder.Create(WorldEvent.StanceUnlockItemTag)
                .Delay(3f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                    {
                        return "Only players may use this item.";
                    }

                    var perkType = (PerkType)GetLocalInt(item, WorldEvent.StanceUnlockPerkVariable);
                    if (!System.Enum.IsDefined(typeof(PerkType), perkType) || perkType == PerkType.Invalid)
                    {
                        return "This item has a configuration problem. Please inform a DM.";
                    }

                    var perkDetail = Perk.GetPerkDetails(perkType);
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (perkDetail.Category == PerkCategoryType.LightsaberForms &&
                        dbPlayer.CharacterType != CharacterType.ForceSensitive)
                    {
                        return "Only Force-Sensitive characters can absorb a holocron's teachings.";
                    }

                    if (perkDetail.Category == PerkCategoryType.CombatDoctrines &&
                        dbPlayer.CharacterType != CharacterType.Standard)
                    {
                        return "The Force offers deeper paths than this datacron's regimented drills.";
                    }

                    if (dbPlayer.UnlockedPerks.ContainsKey(perkType))
                    {
                        return $"You have already mastered the teachings of {perkDetail.Name}.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var perkType = (PerkType)GetLocalInt(item, WorldEvent.StanceUnlockPerkVariable);
                    var perkDetail = Perk.GetPerkDetails(perkType);

                    Perk.UnlockPerkForPlayer(user, perkType);

                    SendMessageToPC(user, ColorToken.Green($"The higher teachings of {perkDetail.Name} are open to you. Its advanced levels may now be learned."));
                    Item.ReduceItemStack(item, 1);
                });
        }
    }
}
