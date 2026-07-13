using System.Collections.Generic;
using NUnit.Framework;
using Orpaits.Enemies;
using UnityEditor;
using UnityEngine;

namespace Orpaits.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for BossVirus phase transitions, events, and death lifecycle.
    ///
    /// Design reference: level-design-260712_2153.md (Boss Fight Details),
    /// game-loop-260712_2137.md (Combat Phase),
    /// lose-and-win-conditions-260712_2137.md
    /// </summary>
    public class BossPhaseTests
    {
        private GameObject go;
        private BossVirus boss;
        private int phaseTransitionCount;
        private BossVirus.BossPhase lastPhase;

        [SetUp]
        public void Setup()
        {
            go = new GameObject("BossTest", typeof(BoxCollider2D), typeof(SpriteRenderer));
            boss = go.AddComponent<BossVirus>();

            // Override serialized defaults (field initializers ran already in Awake)
            var so = new SerializedObject(boss);
            so.FindProperty("maxHealth").floatValue = 100f;
            so.FindProperty("phase2Threshold").floatValue = 0.66f;
            so.FindProperty("phase3Threshold").floatValue = 0.33f;
            so.ApplyModifiedProperties();

            // Awake set CurrentHealth = maxHealth = 1 (field default).
            // SerializedObject updated maxHealth to 100. Heal to match.
            boss.Heal(100f);

            phaseTransitionCount = 0;
            lastPhase = BossVirus.BossPhase.Phase1;
            boss.OnPhaseTransition += phase =>
            {
                phaseTransitionCount++;
                lastPhase = phase;
            };
        }

        [TearDown]
        public void Teardown()
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }

        // ── Initial State ─────────────────────────────────────────────────

        [Test]
        public void Boss_Starts_At_Phase1_And_Alive()
        {
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase1));
            Assert.That(boss.IsDead, Is.False);
            Assert.That(boss.IsBossActive, Is.False);
            Assert.That(boss.CurrentHealth, Is.EqualTo(100f));
        }

        // ── Start Boss Fight ──────────────────────────────────────────────

        [Test]
        public void StartBossFight_Activates_Boss()
        {
            boss.StartBossFight();
            Assert.That(boss.IsBossActive, Is.True);
        }

        [Test]
        public void StartBossFight_Is_Idempotent()
        {
            boss.StartBossFight();
            boss.StartBossFight();
            Assert.That(boss.IsBossActive, Is.True);
        }

        // ── Phase 1 → Phase 2 ─────────────────────────────────────────────

        [Test]
        public void Damage_Below_66_Percent_Triggers_Phase2()
        {
            boss.TakeDamage(35f);  // 100→65 HP (35% remaining < 66%)

            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase2));
            Assert.That(phaseTransitionCount, Is.EqualTo(1));
            Assert.That(lastPhase, Is.EqualTo(BossVirus.BossPhase.Phase2));
        }

        [Test]
        public void Damage_Above_66_Percent_Stays_Phase1()
        {
            boss.TakeDamage(30f);  // 100→70 HP (70% remaining > 66%)

            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase1));
            Assert.That(phaseTransitionCount, Is.EqualTo(0));
        }

        // ── Phase 2 → Phase 3 ─────────────────────────────────────────────

        [Test]
        public void Damage_Below_33_Percent_Triggers_Phase3()
        {
            boss.TakeDamage(68f);  // 100→32 HP (32% remaining < 33%)

            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
            Assert.That(phaseTransitionCount, Is.EqualTo(1));
            Assert.That(lastPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
        }

        [Test]
        public void Damage_Through_Both_Thresholds_Fires_Two_Transitions()
        {
            boss.TakeDamage(80f);  // 100→20 HP, skips past Phase 2 threshold

            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
            Assert.That(phaseTransitionCount, Is.EqualTo(2)); // P1→P2→P3
        }

        [Test]
        public void Damage_Exactly_At_Threshold_Triggers_Phase()
        {
            boss.TakeDamage(34f);  // 66% threshold: 66 HP → Phase 2
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase2));

            boss.TakeDamage(33f);  // 33% threshold: 33 HP → Phase 3
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
            Assert.That(phaseTransitionCount, Is.EqualTo(2));
        }

        [Test]
        public void Phase2_Stays_In_Phase2_After_Moderate_Damage()
        {
            boss.TakeDamage(35f);  // → Phase 2 at 65 HP
            boss.TakeDamage(20f);  // → 45 HP, still Phase 2
            boss.TakeDamage(15f);  // → 30 HP, now Phase 3

            Assert.That(phaseTransitionCount, Is.EqualTo(2));
            Assert.That(lastPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
        }

        // ── Events ────────────────────────────────────────────────────────

        [Test]
        public void OnPhaseTransition_Fires_Each_Phase_In_Order()
        {
            var phases = new List<BossVirus.BossPhase>();
            boss.OnPhaseTransition += p => phases.Add(p);

            boss.TakeDamage(35f);  // → Phase 2
            boss.TakeDamage(33f);  // → Phase 3

            Assert.That(phases.Count, Is.EqualTo(2));
            Assert.That(phases[0], Is.EqualTo(BossVirus.BossPhase.Phase2));
            Assert.That(phases[1], Is.EqualTo(BossVirus.BossPhase.Phase3));
        }

        [Test]
        public void OnPlatformDeletion_Fires_On_Phase2_Entry()
        {
            int deletionCalls = 0;
            float lastPercentage = 0f;
            boss.OnPlatformDeletion += p =>
            {
                deletionCalls++;
                lastPercentage = p;
            };

            boss.TakeDamage(35f);  // → Phase 2

            Assert.That(deletionCalls, Is.EqualTo(1));
            Assert.That(lastPercentage, Is.EqualTo(0.30f).Within(0.001f));
        }

        [Test]
        public void OnHealthChanged_Fires_Current_And_Max()
        {
            float reportedCurrent = 0f, reportedMax = 0f;
            boss.OnHealthChanged += (c, m) =>
            {
                reportedCurrent = c;
                reportedMax = m;
            };

            boss.TakeDamage(30f);

            Assert.That(reportedCurrent, Is.EqualTo(70f));
            Assert.That(reportedMax, Is.EqualTo(100f));
        }

        [Test]
        public void OnDamageTaken_Fires_Exact_Amount()
        {
            float lastDamage = 0f;
            boss.OnDamageTaken += d => lastDamage = d;

            boss.TakeDamage(27.5f);

            Assert.That(lastDamage, Is.EqualTo(27.5f).Within(0.001f));
        }

        // ── Death ─────────────────────────────────────────────────────────

        [Test]
        public void Death_At_Zero_HP()
        {
            int deathCalls = 0;
            boss.OnDeath += () => deathCalls++;

            boss.TakeDamage(100f);

            Assert.That(boss.IsDead, Is.True);
            Assert.That(boss.CurrentHealth, Is.EqualTo(0f));
            Assert.That(deathCalls, Is.EqualTo(1));
            Assert.That(boss.IsBossActive, Is.False);
        }

        [Test]
        public void OnBossDefeated_Fires_On_Death()
        {
            int defeatedCalls = 0;
            boss.OnBossDefeated += () => defeatedCalls++;

            boss.TakeDamage(100f);

            Assert.That(defeatedCalls, Is.EqualTo(1));
        }

        [Test]
        public void TakeDamage_Returns_False_After_Death()
        {
            boss.TakeDamage(100f);
            bool result = boss.TakeDamage(10f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Death_Does_Not_Double_Trigger()
        {
            int deathCalls = 0;
            boss.OnDeath += () => deathCalls++;

            boss.TakeDamage(50f);
            boss.TakeDamage(50f);

            Assert.That(deathCalls, Is.EqualTo(1));
            Assert.That(boss.IsDead, Is.True);
        }

        // ── Reset ─────────────────────────────────────────────────────────

        [Test]
        public void ResetEnemy_Restores_Phase1_And_Full_Health()
        {
            boss.TakeDamage(50f);  // → Phase 2
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase2));

            boss.ResetEnemy();

            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase1));
            Assert.That(boss.IsDead, Is.False);
            Assert.That(boss.CurrentHealth, Is.EqualTo(100f));
            Assert.That(boss.IsBossActive, Is.False);
        }

        // ── Edge Cases ────────────────────────────────────────────────────

        [Test]
        public void Zero_Damage_Does_Nothing()
        {
            boss.TakeDamage(0f);

            Assert.That(boss.CurrentHealth, Is.EqualTo(100f));
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase1));
            Assert.That(phaseTransitionCount, Is.EqualTo(0));
        }

        [Test]
        public void Negative_Damage_Does_Nothing()
        {
            boss.TakeDamage(-10f);

            Assert.That(boss.CurrentHealth, Is.EqualTo(100f));
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase1));
        }

        [Test]
        public void Overkill_Damage_Stops_At_Zero_And_Triggers_Phase3()
        {
            boss.TakeDamage(200f);

            Assert.That(boss.CurrentHealth, Is.EqualTo(0f));
            Assert.That(boss.CurrentPhase, Is.EqualTo(BossVirus.BossPhase.Phase3));
            Assert.That(boss.IsDead, Is.True);
        }
    }
}
