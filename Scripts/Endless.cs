using System.Collections;
using EndlessMode;
using DiskCardGame;
using UnityEngine;
using Random = System.Random;

public static class Endless
{
    public static int EndlessSeed => SaveManager.SaveFile.GetCurrentRandomSeed() + m_EndlessSeed++;

    private static int m_EndlessSeed = 0;
    
    public static IEnumerator FinalBossKilled(Part1BossOpponent opponent)
    {
        Plugin.Log.LogInfo($"Killed Final Boss: {opponent.OpponentType}");
        RunStats.FinalBossKilled(opponent);
        yield return opponent.BossDefeatedSequence(); // Give back flames and record boss kill
    }

    public static void SetupNewRun()
    {
        RunStats.SetupNewRun();
    }
    
    public static void SetupNextRun()
    {
        RunStats.SetupNextRun();
        
        RunState.Run.regionTier = 0;
        AscensionSaveData.Data.RollCurrentRunRegionOrder();        
        RunState.Run.GenerateMapDataForCurrentRegion();
    }

    public static void NextRegionStarted()
    {
        RunStats.NewFloorStarted();
    }

    public static void BossKilled(Part1BossOpponent opponent)
    {
        RunStats.BossKilled(opponent);
        Plugin.Log.LogInfo($"Killed {opponent.OpponentType}");
    }

    public static bool EndlessRunFinished()
    {
        return false;
    }

    public static void AdjustDifficulty(PlayableCard card, Opponent opponent)
    {
        int totalChance = RunStats.TotalResets * Configs.Instance.DifficultyIncreaseChancePerReset; // eg: 5 / 115
        if (totalChance <= 0)
        {
            return;
        }

        int rolls = Mathf.CeilToInt(totalChance / 100f); // eg: 1 / 2
        while (rolls > 0)
        {
            int chance = SeededRandom.Range(0, 100, EndlessSeed); // eg: 2
            if (chance < totalChance)
            {
                BuffCard(card);
            }

            totalChance -= 100;
            rolls--;
        }
    }

    private static void BuffCard(PlayableCard card)
    {
        int healthChance = Configs.Instance.DifficultyHealthChance;
        int attackChance = Configs.Instance.DifficultyAttackChance;
        int sigilChance = Configs.Instance.DifficultySigilChance;
        int totalChance = healthChance + attackChance + sigilChance;

        CardModificationInfo modificationInfo = new CardModificationInfo();
        modificationInfo.fromCardMerge = true;
        int chance = SeededRandom.Range(0, totalChance, EndlessSeed); // eg: 2
        if (chance < healthChance)
        {
            modificationInfo.healthAdjustment = Configs.Instance.DifficultyHealthBuff;
        }
        else if (chance < healthChance + attackChance)
        {
            modificationInfo.attackAdjustment = Configs.Instance.DifficultyAttackBuff;
        }
        else
        {
            modificationInfo.abilities = new List<Ability>();
            modificationInfo.abilities.Add(AbilitiesUtil.GetRandomLearnedAbility(EndlessSeed));
        }
        
        card.AddTemporaryMod(modificationInfo);
    }

    public static void Prestige()
    {
        RunStats.Prestige();
    }
}