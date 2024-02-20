using DiskCardGame;
using EndlessMode;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Nodes;

[HarmonyPatch]
public static class Prestige_Patches
{
    private static MenuAction m_prestigeAction = GuidManager.GetEnumValue<MenuAction>(Plugin.PluginGuid, "PrestigeAction");
    
    [HarmonyPatch(typeof(MenuController), "Start"), HarmonyPostfix]
    public static void MenuController_Start_Postfix(MenuController __instance)
    {
        // Add a new card so we can prestige
        List<MenuCard> cards = __instance.cards;
        MenuCard card = cards[0];
        MenuCard newCard = UnityEngine.Object.Instantiate<MenuCard>(card, card.transform.parent);
        newCard.menuAction = m_prestigeAction;
        cards.Add(newCard);
    }
    
    [HarmonyPatch(typeof(MenuController), "OnCardReachedSlot"), HarmonyPostfix]
    public static void MenuController_OnCardReachedSlot_Postfix(MenuController __instance, MenuCard card, bool skipTween)
    {
        if (card.menuAction == m_prestigeAction)
        {
            Prestige(__instance);
        }
    }
    
    private static void Prestige(MenuController controller)
    {
        // Prestige
        Endless.Prestige();
        
        PauseMenu.instance.UnPause();


        CustomSpecialNodeData node = new CustomSpecialNodeData(PrestigeSequencer.fullNode);
        Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.SpecialCardSequence, node);
    }
}