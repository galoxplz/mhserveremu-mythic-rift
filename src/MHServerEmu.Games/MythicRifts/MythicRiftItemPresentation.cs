using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftItemPresentation
    {
        public const string PresentationPrototypeName = "DangerRoomScenarioCrateUniqueCableFight";
        public const string PresentationPrototypePath = "Entity/Items/Consumables/Prototypes/DangerRoom/DangerRoomScenarioCrateUniqueCableFight.prototype";
        public const string PresentationDisplayName = "Mythic Rift Scenario";
        public const ulong PresentationPrototypeId = 17067585073904428862UL;
        public const ulong PresentationVisualRarityPrototypeId = 6033964048325414744UL;

        public static ItemSpec ApplyLauncherPresentation(ItemSpec itemSpec)
        {
            if (itemSpec == null)
                return null;

            PrototypeId presentationProtoRef = ResolvePresentationPrototypeRef();
            if (presentationProtoRef.As<ItemPrototype>() == null)
                return itemSpec;

            PrototypeId visualRarityProtoRef = ResolvePresentationVisualRarityRef();
            if (visualRarityProtoRef.As<RarityPrototype>() == null)
                return itemSpec;

            return new ItemSpec(
                presentationProtoRef,
                visualRarityProtoRef,
                itemSpec.ItemLevel,
                itemSpec.CreditsAmount,
                itemSpec.AffixSpecs,
                itemSpec.Seed,
                itemSpec.EquippableBy)
            {
                StackCount = itemSpec.StackCount
            };
        }

        public static PrototypeId ResolvePresentationPrototypeRef()
        {
            PrototypeId prototypeRef = GameDatabase.GetPrototypeRefByName(PresentationPrototypePath);
            if (prototypeRef.As<ItemPrototype>() != null)
                return prototypeRef;

            prototypeRef = GameDatabase.GetPrototypeRefByName(PresentationPrototypeName);
            if (prototypeRef.As<ItemPrototype>() != null)
                return prototypeRef;

            prototypeRef = (PrototypeId)PresentationPrototypeId;
            return prototypeRef.As<ItemPrototype>() != null
                ? prototypeRef
                : PrototypeId.Invalid;
        }

        public static PrototypeId ResolvePresentationVisualRarityRef()
        {
            PrototypeId rarityProtoRef = (PrototypeId)PresentationVisualRarityPrototypeId;
            return rarityProtoRef.As<RarityPrototype>() != null
                ? rarityProtoRef
                : PrototypeId.Invalid;
        }
    }
}
