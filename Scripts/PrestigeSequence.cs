using System.Collections;
using DiskCardGame;
using InscryptionAPI.Nodes;
using InscryptionAPI.Saves;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace EndlessMode
{
    public class PrestigeSequencer : MonoBehaviour, ICustomNodeSequencer
    {
        private CardMergeSequencer sequencer = null;
        private CardSingleChoicesSequencer cardChoiceSequencer = null;
        private Texture eyeTexture = null;
        
        public static NewNodeManager.FullNode fullNode;

        public static void Initialize()
        {
	        string guid = Plugin.PluginGuid;
	        string sequenceName = "Endless Prestige Sequence";
	        fullNode = NewNodeManager.New<PrestigeSequencer>(guid, sequenceName, GenerationType.SpecialEvent,
		        new List<Texture2D>()
		        {
			        null
		        },
		        new List<NodeData.SelectionCondition>()
		        {
			        new CustomPreviousNodesContent(guid, sequenceName, false),
			        new NodeData.WithinRegionIndexRange(1, int.MaxValue),
			        new NodeData.WithinGridYRange(2, int.MaxValue)
		        },
		        new List<NodeData.SelectionCondition>()
		        {
			        
		        }
	        );
        }

        private CardMergeSequencer CloneCardMergeSequence()
        {
	        CardMergeSequencer clone = Instantiate(GameObject.FindObjectOfType<CardMergeSequencer>(), transform);
	        GameObject backRock = StarCraftCore.Utils.FindObjectInChildren(clone.gameObject, "Back Rock");
	        GameObject quad = StarCraftCore.Utils.FindObjectInChildren(backRock, "Quad");

	        Texture t = Utils.GetTextureFromPath("Artwork/Sequencers/card_slot_host_evolve.png");
	        Renderer renderer = quad.GetComponent<Renderer>();
	        renderer.material.mainTexture = t;

	        return clone;
        }
        
        public PrestigeSequencer()
        {
	        sequencer = CloneCardMergeSequence();

	        DuplicateMergeSequencer duplicateMergeSequencer = GameObject.FindObjectOfType<DuplicateMergeSequencer>();
	        cardChoiceSequencer = Instantiate(duplicateMergeSequencer.cardChoiceSequencer, transform);
	        cardChoiceSequencer.deckPile = this.sequencer.pile;
		        
	        DeckTrialSequencer deckTrialSequencer = GameObject.FindObjectOfType<DeckTrialSequencer>();
	        eyeTexture = deckTrialSequencer.snakeEyeTexture;
        }

        public IEnumerator DoCustomSequence(CustomSpecialNodeData nodeData)
        {
            sequencer.hostSlot.Disable();
            sequencer.sacrificeSlot.Disable();
            Singleton<TableRuleBook>.Instance.SetOnBoard(true);
            Singleton<ViewManager>.Instance.Controller.SwitchToControlMode(ViewController.ControlMode.CardMerging, false);
            Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;

            yield return new WaitForSeconds(0.5f);
            if (!HasLearnedSequence())
            {
	            yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(
		            "A large green slug like looking creature slithers towards you.",
		            -2.5f, 0.5f, Emotion.Neutral, TextDisplayer.LetterAnimation.WavyJitter);
            }

            LeshyAnimationController.Instance.PutOnMask(AbatharMask.ID, true);
            yield return new WaitForSeconds(0.3f);
            //Singleton<ExplorableAreaManager>.Instance.TweenHangingLightColors(new Color(0.3f, 0.6f, 0, 1), new Color(0.3f, 0.4f, 0, 1), 0.1f);
            LeshyAnimationController.Instance.SetEyesTexture(eyeTexture);
            yield return new WaitForSeconds(0.3f);
            
            if (!HasLearnedSequence())
            {
	            yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(
		            "It's the Evolution Master",
		            -2.5f, 0.5f, Emotion.Neutral, TextDisplayer.LetterAnimation.WavyJitter);
            }
            
            yield return sequencer.pile.SpawnCards(RunState.DeckList.Count, 0.5f);
            
            List<CardInfo> validHosts = this.GetValidCardsForHost(null);
            List<CardInfo> validSacrifices = this.GetValidCardsForSacrifice(null);
            bool singleValidHostIsSameAsSacrifice = validHosts.Count == 1 && validSacrifices.Count == 1 && validHosts[0] == validSacrifices[0];
            bool hasNoValidOptions = validHosts.Count == 0 || validSacrifices.Count == 0 || singleValidHostIsSameAsSacrifice;
            if (hasNoValidOptions)
            {
                yield return InvalidCardsSequence();
            }
            else
            {
	            yield return AbathurMessage("Primitive Army");
	            yield return AbathurMessage("Potential hidden evolution available", false);
	            yield return AbathurMessage("Require additional Biomass extraction");
	            yield return ValidSequence();
	            yield return AbathurMessage("Army now more efficient. Victory is inevitable.", false);
	            
	            if (RunState.Run.consumables.Count < RunState.Run.MaxConsumables)
	            {
		            yield return new WaitForSeconds(0.4f);
		            Singleton<ViewManager>.Instance.SwitchToView(View.Consumables, false, false);
		            yield return new WaitForSeconds(0.2f);
		            
		            int currentRandomSeed = SaveManager.SaveFile.GetCurrentRandomSeed();
		            bool attackBioMass = SeededRandom.Range(0, 100, currentRandomSeed++) <= 50;
		            string itemName = attackBioMass ? BiomassAttackerInABottle.Data.name : BiomassInABottle.Data.name;
		            RunState.Run.consumables.Add(itemName);
		            Singleton<ItemsManager>.Instance.UpdateItems(false);
		            yield return new WaitForSeconds(0.5f);
		            yield return AbathurMessage("Excess Biomass will help on battlefield.", false);
	            }
            }
            
            yield return LeshyAnimationController.Instance.TakeOffMask();
            //Singleton<ExplorableAreaManager>.Instance.ResetHangingLightsToZoneColors(0.25f);
            Singleton<ViewManager>.Instance.SwitchToView(View.Default, false, false);
			yield return sequencer.pile.DestroyCards(0.5f);
			sequencer.stoneCircleAnim.SetTrigger("exit");
			yield return new WaitForSeconds(0.25f);
			LeshyAnimationController.Instance.ResetEyesTexture();
			sequencer.confirmStone.Exit();
			yield return new WaitForSeconds(0.75f);
			sequencer.stoneCircleAnim.gameObject.SetActive(false);
			sequencer.confirmStone.SetStoneInactive();
			sequencer.sacrificeSlot.DestroyCard();
			sequencer.hostSlot.DestroyCard();
			
			if (!HasLearnedSequence())
			{
				SetSequenceLearned();
			}
			
			if (Singleton<GameFlowManager>.Instance != null)
			{
				Singleton<GameFlowManager>.Instance.TransitionToGameState(GameState.Map, null);
			}
        }

        private IEnumerator ValidSequence()
        {
	        sequencer.stoneCircleAnim.gameObject.SetActive(true);
	        yield return new WaitForSeconds(0.5f);
	        
	        Singleton<ViewManager>.Instance.SwitchToView(View.CardMergeSlots, false, false);
	        sequencer.sacrificeSlot.RevealAndEnable();
	        sequencer.sacrificeSlot.ClearDelegates();
	        SelectCardFromDeckSlot selectCardFromDeckSlot = sequencer.sacrificeSlot;
	        selectCardFromDeckSlot.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(
		        selectCardFromDeckSlot.CursorSelectStarted, new Action<MainInputInteractable>(this.OnSlotSelected));

	        sequencer.hostSlot.RevealAndEnable();
	        sequencer.hostSlot.ClearDelegates();
	        SelectCardFromDeckSlot selectCardFromDeckSlot2 = sequencer.hostSlot;
	        selectCardFromDeckSlot2.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(
		        selectCardFromDeckSlot2.CursorSelectStarted, new Action<MainInputInteractable>(this.OnSlotSelected));

	        yield return sequencer.confirmStone.WaitUntilConfirmation();
	        sequencer.hostSlot.Disable();
	        sequencer.sacrificeSlot.Disable();
	        yield return new WaitForSeconds(0.25f);
	        Singleton<ViewManager>.Instance.SwitchToView(View.CardMergeSlots, false, true);
	        Singleton<RuleBookController>.Instance.SetShown(false, true);
	        yield return new WaitForSeconds(1f);
	        foreach (SpecialCardBehaviour cardBehaviour in sequencer.hostSlot.Card.GetComponents<SpecialCardBehaviour>())
	        {
		        yield return cardBehaviour.OnSelectedForCardRemoval();
	        }

	        // Remove sacrifice
	        CardInfo sacrificedInfo = sequencer.sacrificeSlot.Card.Info;
	        RunState.Run.playerDeck.RemoveCard(sacrificedInfo);
	        sequencer.sacrificeSlot.Card.Anim.SetSacrificeHoverMarkerShown(false);
	        sequencer.sacrificeSlot.Card.Anim.PlayDeathAnimation(false);
	        AudioController.Instance.PlaySound3D("sacrifice_default", MixerGroup.TableObjectsSFX, base.transform.position, 1f,
		        0f, null, null, null, null, false);
	        yield return new WaitForSeconds(0.5f);
	        AudioController.Instance.PlaySound3D("card_blessing", MixerGroup.TableObjectsSFX,
		        sequencer.hostSlot.transform.position, 1f, 0f, new AudioParams.Pitch(0.6f), null, null, null, false);
	        sequencer.hostSlot.Card.Anim.PlayTransformAnimation();
	        yield return new WaitForSeconds(0.15f);
	        
	        // Evolve current card
	        CardInfo evolvedInfo = (CardInfo)sequencer.hostSlot.Card.Info.evolveParams.evolution.Clone();
	        evolvedInfo.Mods.AddRange(sequencer.hostSlot.Card.Info.Mods);
	        RunState.Run.playerDeck.RemoveCard(sequencer.hostSlot.Card.Info);
	        sequencer.hostSlot.Card.SetInfo(evolvedInfo);
	        sequencer.hostSlot.Card.SetInteractionEnabled(false);
	        RunState.Run.playerDeck.AddCard(evolvedInfo);
	        sequencer.transformParticles.gameObject.SetActive(false);
	        sequencer.transformParticles.gameObject.SetActive(true);
	        yield return null;
	        sequencer.hostSlot.Card.RenderCard();
	        
	        yield return new WaitForSeconds(1.5f);
	        sequencer.hostSlot.FlyOffCard();
        }

        private List<CardInfo> GetValidCardsForHost(CardInfo sacrifice = null)
        {
            List<CardInfo> list = new List<CardInfo>(RunState.DeckList);
            list.RemoveAll((a) => a.evolveParams == null);
            return list;
        }

        private List<CardInfo> GetValidCardsForSacrifice(CardInfo host = null)
        {
            List<CardInfo> list = new List<CardInfo>(RunState.DeckList);
            return list;
        }

        private IEnumerator AbathurMessage(string message, bool skipable=true)
        {
	        if (!skipable || !HasLearnedSequence())
	        {
		        yield return Singleton<TextDisplayer>.Instance.ShowUntilInput(
			        "[c:light_green]" + message + "[c:]",
			        -2.5f, 0.5f, Emotion.Neutral, TextDisplayer.LetterAnimation.Jitter);
	        }
        }

        private bool HasLearnedSequence()
        {
	        bool learned = ModdedSaveManager.SaveData.GetValueAsBoolean(Plugin.PluginGuid, "LearnedEvolveSequence");
	        return learned;
        }

        private void SetSequenceLearned()
        {
	        ModdedSaveManager.SaveData.SetValue(Plugin.PluginGuid, "LearnedEvolveSequence", true);
        }
        
        private IEnumerator InvalidCardsSequence()
        {
	        yield return AbathurMessage("Army strands not capable of evolution.", false);
	        yield return AbathurMessage("Choose new strand to join Army.", false);
	        
	        CardChoicesNodeData nodeData = new CardChoicesNodeData();
	        nodeData.overrideChoices = this.GetEvolvingCardChoices();
	        if (nodeData.overrideChoices.Count > 0)
	        {
		        Singleton<ViewManager>.Instance.SwitchToView(View.CardMergeSlots, false, false);
		        yield return new WaitForSeconds(0.1f);
		        yield return cardChoiceSequencer.CardSelectionSequence(nodeData);
	        }
	        else
	        {
		        yield return AbathurMessage("Army strands not capable of improving. Need more samples!", false);
	        }
	        yield return new WaitForSeconds(0.1f);
        }
        
        private List<CardChoice> GetEvolvingCardChoices()
        {
	        List<CardInfo> allCards = new List<CardInfo>(CardLoader.AllData);
	        allCards.RemoveAll((CardInfo x) => x.evolveParams == null || 
	                                           x.temple != CardTemple.Nature || 
	                                           x.metaCategories.Count == 0 || 
	                                           x.metaCategories.Contains(CardMetaCategory.Rare) ||
	                                           !(x.metaCategories.Contains(CardMetaCategory.TraderOffer) && x.metaCategories.Contains(CardMetaCategory.ChoiceNode)));
	        
	        List<CardChoice> finalList = new List<CardChoice>();
	        int currentRandomSeed = SaveManager.SaveFile.GetCurrentRandomSeed();
	        while (allCards.Count > 0 && finalList.Count < 3)
	        {
		        CardInfo cardInfo = allCards[SeededRandom.Range(0, allCards.Count, currentRandomSeed++)];
		        finalList.Add(new CardChoice
		        {
			        CardInfo = CardLoader.GetCardByName(cardInfo.name)
		        });
		        allCards.Remove(cardInfo);
	        }
	        return finalList;
        }
        
        private void OnSlotSelected(MainInputInteractable slot)
        {
	        Singleton<RuleBookController>.Instance.SetShown(false, true);
	        sequencer.sacrificeSlot.SetEnabled(false);
	        sequencer.hostSlot.SetEnabled(false);
	        sequencer.hostSlot.ShowState(HighlightedInteractable.State.NonInteractable, false, 0.15f);
	        sequencer.sacrificeSlot.ShowState(HighlightedInteractable.State.NonInteractable, false, 0.15f);
	        sequencer.confirmStone.Exit();
	        bool flag = slot == sequencer.hostSlot;
	        List<CardInfo> list;
	        if (flag)
	        {
		        list = this.GetValidCardsForHost((sequencer.sacrificeSlot.Card == null) ? null : sequencer.sacrificeSlot.Card.Info);
	        }
	        else
	        {
		        bool flag2 = sequencer.sacrificeSlot.Card != null;
		        if (flag2)
		        {
			        sequencer.sacrificeSlot.Card.Anim.SetSacrificeHoverMarkerShown(false);
		        }
		        list = this.GetValidCardsForSacrifice((sequencer.hostSlot.Card == null) ? null : sequencer.hostSlot.Card.Info);
	        }
	        bool flag3 = sequencer.hostSlot.Card != null && slot == sequencer.sacrificeSlot;
	        if (flag3)
	        {
		        list.Remove(sequencer.hostSlot.Card.Info);
	        }
	        bool flag4 = sequencer.sacrificeSlot.Card != null && slot == sequencer.hostSlot;
	        if (flag4)
	        {
		        list.Remove(sequencer.sacrificeSlot.Card.Info);
	        }
	        bool flag5 = list.Count == 0;
	        if (flag5)
	        {
		        bool flag6 = sequencer.noValidChoicesDialogueEvent != null;
		        if (flag6)
		        {
			        base.StopCoroutine(sequencer.noValidChoicesDialogueEvent);
		        }
		        sequencer.noValidChoicesDialogueEvent = base.StartCoroutine(Singleton<TextDisplayer>.Instance.ShowThenClear("There are no compatible sacrifices... It won't work.", 3.5f, 0f, Emotion.Neutral, TextDisplayer.LetterAnimation.Jitter, DialogueEvent.Speaker.Single, null));
	        }
	        SelectCardFromDeckSlot selectFromDeckSlot = slot as SelectCardFromDeckSlot;
	        selectFromDeckSlot.SelectFromCards(list, delegate
	        {
		        this.OnSelectionEnded(selectFromDeckSlot);
	        }, true);
        }
        
        private void OnSelectionEnded(SelectCardFromDeckSlot slot)
        {
	        Singleton<RuleBookController>.Instance.SetShown(false, true);
	        sequencer.sacrificeSlot.SetShown(true, false);
	        sequencer.sacrificeSlot.ShowState(HighlightedInteractable.State.Interactable, false, 0.15f);
	        sequencer.hostSlot.SetShown(true, false);
	        sequencer.hostSlot.ShowState(HighlightedInteractable.State.Interactable, false, 0.15f);
	        Singleton<ViewManager>.Instance.SwitchToView(View.CardMergeSlots, false, true);
	        bool flag = slot == sequencer.sacrificeSlot && sequencer.sacrificeSlot.Card != null;
	        if (flag)
	        {
		        sequencer.sacrificeSlot.Card.Anim.SetSacrificeHoverMarkerShown(true);
	        }
	        bool flag2 = sequencer.hostSlot.Card != null && sequencer.sacrificeSlot.Card != null;
	        if (flag2)
	        {
		        sequencer.confirmStone.Enter();
	        }
        }
    }
}