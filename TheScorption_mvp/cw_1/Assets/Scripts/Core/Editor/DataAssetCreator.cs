using UnityEngine;
using UnityEditor;
using TheScorpion.Core;
using TheScorpion.Data;

public class DataAssetCreator
{
    [MenuItem("Tools/Scorpion/Create All Data Assets")]
    public static void CreateAllDataAssets()
    {
        CreateElementData();
        CreateEnemyData();
        CreateWaveData();
        CreateEventChannels();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Scorpion] All data assets created!");
    }

    [MenuItem("Tools/Scorpion/Wire VFX to Element Data")]
    public static void WireVFXToElements()
    {
        var fireData = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Fire_Data.asset");
        var lightningData = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Lightning_Data.asset");

        if (fireData == null || lightningData == null)
        {
            Debug.LogError("[Scorpion] Element data assets not found!");
            return;
        }

        // Fire VFX
        var fireTornado = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/FlamesParticleEffect.prefab");
        var fireAura = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/FlamesEffects.prefab");
        var fireBurst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/BigExplosionEffect.prefab");

        if (fireTornado != null) fireData.ability1VFXPrefab = fireTornado;
        if (fireAura != null) fireData.ability2VFXPrefab = fireAura;
        if (fireBurst != null) fireData.burstVFXPrefab = fireBurst;
        EditorUtility.SetDirty(fireData);

        // Lightning VFX
        var lightningBurst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/PlasmaExplosionEffect.prefab");
        var lightningSpeed = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/SmallExplosionEffect.prefab");

        if (lightningBurst != null) lightningData.ability1VFXPrefab = lightningBurst;
        if (lightningSpeed != null) lightningData.ability2VFXPrefab = lightningSpeed;
        if (lightningBurst != null) lightningData.burstVFXPrefab = lightningBurst;
        EditorUtility.SetDirty(lightningData);

        AssetDatabase.SaveAssets();
        Debug.Log($"[Scorpion] VFX wired! Fire: tornado={fireTornado != null}, aura={fireAura != null}, burst={fireBurst != null} | Lightning: burst={lightningBurst != null}, speed={lightningSpeed != null}");
    }

    [MenuItem("Tools/Scorpion/Wire Player Data References")]
    public static void WirePlayerReferences()
    {
        var elementSystem = Object.FindAnyObjectByType<TheScorpion.Player.ElementSystem>();
        if (elementSystem == null)
        {
            Debug.LogError("[Scorpion] No ElementSystem found! Run Setup Player first.");
            return;
        }

        var fireData = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Fire_Data.asset");
        var lightningData = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Lightning_Data.asset");

        if (fireData == null || lightningData == null)
        {
            Debug.LogError("[Scorpion] Element data assets not found! Run Create All Data Assets first.");
            return;
        }

        var so = new SerializedObject(elementSystem);
        so.FindProperty("fireData").objectReferenceValue = fireData;
        so.FindProperty("lightningData").objectReferenceValue = lightningData;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(elementSystem);

        // Wire event channels
        var onElementChanged = AssetDatabase.LoadAssetAtPath<IntEventChannelSO>("Assets/ScriptableObjects/Events/OnElementChanged.asset");
        if (onElementChanged != null)
        {
            so = new SerializedObject(elementSystem);
            so.FindProperty("onElementChangedEvent").objectReferenceValue = onElementChanged;
            so.ApplyModifiedProperties();
        }

        // Wire projectile VFX
        var fireballVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/FlameThrowerEffect.prefab");
        var lightningVFX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Particles/EffectExamples/FireExplosionEffects/Prefabs/PlasmaExplosionEffect.prefab");
        var elSO = new SerializedObject(elementSystem);
        if (fireballVFX != null)
            elSO.FindProperty("fireProjectileVFX").objectReferenceValue = fireballVFX;
        if (lightningVFX != null)
            elSO.FindProperty("lightningProjectileVFX").objectReferenceValue = lightningVFX;
        elSO.ApplyModifiedProperties();

        // Wire UltimateSystem events
        var ultimateSystem = Object.FindAnyObjectByType<TheScorpion.Player.UltimateSystem>();
        var onEnemyKilled = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/ScriptableObjects/Events/OnEnemyKilled.asset");
        if (ultimateSystem != null && onEnemyKilled != null)
        {
            var ultSO = new SerializedObject(ultimateSystem);
            ultSO.FindProperty("onEnemyKilledEvent").objectReferenceValue = onEnemyKilled;
            ultSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(ultimateSystem);
        }

        // Wire DamageInterceptor events
        var damageInterceptor = Object.FindAnyObjectByType<TheScorpion.Combat.DamageInterceptor>();
        var onDamageDealt = AssetDatabase.LoadAssetAtPath<DamageEventChannelSO>("Assets/ScriptableObjects/Events/OnDamageDealt.asset");
        if (damageInterceptor != null)
        {
            var dmgSO = new SerializedObject(damageInterceptor);
            if (onDamageDealt != null)
                dmgSO.FindProperty("onDamageDealtEvent").objectReferenceValue = onDamageDealt;
            if (onEnemyKilled != null)
                dmgSO.FindProperty("onEnemyKilledEvent").objectReferenceValue = onEnemyKilled;
            dmgSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(damageInterceptor);
        }

        // Wire GameManager events
        var gameManager = Object.FindAnyObjectByType<TheScorpion.Core.GameManager>();
        var onPlayerDied = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/ScriptableObjects/Events/OnPlayerDied.asset");
        var onVictory = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/ScriptableObjects/Events/OnVictory.asset");
        if (gameManager != null)
        {
            var gmSO = new SerializedObject(gameManager);
            if (onPlayerDied != null)
                gmSO.FindProperty("onPlayerDiedEvent").objectReferenceValue = onPlayerDied;
            if (onVictory != null)
                gmSO.FindProperty("onVictoryEvent").objectReferenceValue = onVictory;
            gmSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(gameManager);
        }

        Debug.Log("[Scorpion] All data references wired!");
    }

    private static void CreateElementData()
    {
        // Fire
        var fire = ScriptableObject.CreateInstance<ElementDataSO>();
        fire.elementType = ElementType.Fire;
        fire.elementName = "Fire";
        fire.elementColor = new Color(1f, 0.4f, 0.1f);
        fire.ability1Name = "Fire Tornado";
        fire.ability1Damage = 15f;
        fire.ability1Radius = 4f;
        fire.ability1Cooldown = 8f;
        fire.ability1Cost = 40f;
        fire.ability1Duration = 3f;
        fire.ability2Name = "Fire Aura";
        fire.ability2Duration = 6f;
        fire.ability2Cooldown = 12f;
        fire.ability2Cost = 30f;
        fire.ability2BurnDamagePerTick = 5f;
        fire.burstDamage = 60f;
        fire.burstRadius = 8f;
        fire.burstStunDuration = 0f;
        fire.weaponTrailColor = new Color(1f, 0.5f, 0f);
        CreateAsset(fire, "Assets/ScriptableObjects/ElementData/Fire_Data.asset");

        // Lightning
        var lightning = ScriptableObject.CreateInstance<ElementDataSO>();
        lightning.elementType = ElementType.Lightning;
        lightning.elementName = "Lightning";
        lightning.elementColor = new Color(0.3f, 0.7f, 1f);
        lightning.ability1Name = "Lightning Burst";
        lightning.ability1Damage = 20f;
        lightning.ability1Radius = 3f;
        lightning.ability1Cooldown = 6f;
        lightning.ability1Cost = 35f;
        lightning.ability1Duration = 0f;
        lightning.ability2Name = "Lightning Speed";
        lightning.ability2Duration = 5f;
        lightning.ability2Cooldown = 15f;
        lightning.ability2Cost = 50f;
        lightning.ability2AttackSpeedBonus = 0.4f;
        lightning.ability2MoveSpeedBonus = 0.25f;
        lightning.burstDamage = 40f;
        lightning.burstRadius = 15f;
        lightning.burstStunDuration = 2f;
        lightning.weaponTrailColor = new Color(0.4f, 0.8f, 1f);
        CreateAsset(lightning, "Assets/ScriptableObjects/ElementData/Lightning_Data.asset");

        Debug.Log("[Scorpion] Created Fire_Data and Lightning_Data");
    }

    private static void CreateEnemyData()
    {
        // Hollow Monk (Basic)
        var monk = ScriptableObject.CreateInstance<EnemyDataSO>();
        monk.enemyName = "Hollow Monk";
        monk.enemyType = EnemyType.Basic;
        monk.maxHealth = 30;
        monk.moveSpeed = 3f;
        monk.attackDamage = 8;
        monk.attackRange = 1.5f;
        monk.attackWindup = 1f;
        monk.attackRecovery = 0.8f;
        monk.poiseMax = 20f;
        monk.staggerDuration = 1.5f;
        monk.fireResistance = 0f;
        monk.lightningResistance = 0f;
        monk.burnSlowMultiplier = 1f;
        monk.stunDurationMultiplier = 1f;
        monk.maxAttackCount = 1;
        monk.adrenalineOnKill = 5;
        CreateAsset(monk, "Assets/ScriptableObjects/EnemyData/HollowMonk_Data.asset");

        // Shadow Acolyte (Fast)
        var acolyte = ScriptableObject.CreateInstance<EnemyDataSO>();
        acolyte.enemyName = "Shadow Acolyte";
        acolyte.enemyType = EnemyType.Fast;
        acolyte.maxHealth = 20;
        acolyte.moveSpeed = 8f;
        acolyte.attackDamage = 12;
        acolyte.attackRange = 1.2f;
        acolyte.attackWindup = 0.4f;
        acolyte.attackRecovery = 0.5f;
        acolyte.poiseMax = 10f;
        acolyte.staggerDuration = 1f;
        acolyte.fireResistance = 0f;
        acolyte.lightningResistance = 0f;
        acolyte.burnSlowMultiplier = 0.5f; // Burns slow them significantly
        acolyte.stunDurationMultiplier = 0.33f; // Short stun only (0.5s)
        acolyte.maxAttackCount = 2;
        acolyte.adrenalineOnKill = 5;
        CreateAsset(acolyte, "Assets/ScriptableObjects/EnemyData/ShadowAcolyte_Data.asset");

        // Stone Sentinel (Heavy)
        var sentinel = ScriptableObject.CreateInstance<EnemyDataSO>();
        sentinel.enemyName = "Stone Sentinel";
        sentinel.enemyType = EnemyType.Heavy;
        sentinel.maxHealth = 80;
        sentinel.moveSpeed = 2f;
        sentinel.attackDamage = 20;
        sentinel.attackRange = 2f;
        sentinel.attackWindup = 1.5f;
        sentinel.attackRecovery = 1f;
        sentinel.poiseMax = 50f;
        sentinel.staggerDuration = 1.5f;
        sentinel.fireResistance = 0.5f; // 50% burn resist
        sentinel.lightningResistance = 0f;
        sentinel.burnSlowMultiplier = 1f; // No slow
        sentinel.stunDurationMultiplier = 1f; // Full stun but no knockback
        sentinel.lightAttackReduction = 0.5f; // 50% damage from light attacks
        sentinel.maxAttackCount = 1;
        sentinel.chanceToBlock = 0.3f;
        sentinel.adrenalineOnKill = 10;
        CreateAsset(sentinel, "Assets/ScriptableObjects/EnemyData/StoneSentinel_Data.asset");

        // The Fallen Guardian (Boss)
        var boss = ScriptableObject.CreateInstance<EnemyDataSO>();
        boss.enemyName = "The Fallen Guardian";
        boss.enemyType = EnemyType.Boss;
        boss.maxHealth = 300;
        boss.moveSpeed = 4f;
        boss.attackDamage = 15;
        boss.attackRange = 2.5f;
        boss.attackWindup = 1f;
        boss.attackRecovery = 0.8f;
        boss.poiseMax = 100f;
        boss.staggerDuration = 1f;
        boss.fireResistance = 0.2f;
        boss.lightningResistance = 0.2f;
        boss.burnSlowMultiplier = 0.9f;
        boss.stunDurationMultiplier = 0.5f;
        boss.maxAttackCount = 3;
        boss.adrenalineOnKill = 50;
        CreateAsset(boss, "Assets/ScriptableObjects/EnemyData/FallenGuardian_Data.asset");

        Debug.Log("[Scorpion] Created all enemy data assets");
    }

    private static void CreateWaveData()
    {
        var waves = ScriptableObject.CreateInstance<WaveDataSO>();
        waves.waves = new System.Collections.Generic.List<WaveDefinition>
        {
            new WaveDefinition { waveNumber = 1, basicEnemyCount = 3, fastEnemyCount = 0, heavyEnemyCount = 0, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 2, basicEnemyCount = 5, fastEnemyCount = 0, heavyEnemyCount = 0, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 3, basicEnemyCount = 3, fastEnemyCount = 1, heavyEnemyCount = 0, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 4, basicEnemyCount = 4, fastEnemyCount = 2, heavyEnemyCount = 0, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 5, basicEnemyCount = 3, fastEnemyCount = 1, heavyEnemyCount = 1, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 6, basicEnemyCount = 4, fastEnemyCount = 2, heavyEnemyCount = 1, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 7, basicEnemyCount = 2, fastEnemyCount = 3, heavyEnemyCount = 1, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 8, basicEnemyCount = 4, fastEnemyCount = 2, heavyEnemyCount = 2, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 9, basicEnemyCount = 0, fastEnemyCount = 4, heavyEnemyCount = 2, delayBeforeWave = 3f, spawnInterval = 0.5f },
            new WaveDefinition { waveNumber = 10, basicEnemyCount = 0, fastEnemyCount = 0, heavyEnemyCount = 0, isBossWave = true, delayBeforeWave = 5f, spawnInterval = 0f },
        };
        CreateAsset(waves, "Assets/ScriptableObjects/WaveData/Level1_Waves.asset");
        Debug.Log("[Scorpion] Created Level1_Waves data");
    }

    private static void CreateEventChannels()
    {
        CreateAsset(ScriptableObject.CreateInstance<VoidEventChannelSO>(), "Assets/ScriptableObjects/Events/OnEnemyKilled.asset");
        CreateAsset(ScriptableObject.CreateInstance<VoidEventChannelSO>(), "Assets/ScriptableObjects/Events/OnPlayerDied.asset");
        CreateAsset(ScriptableObject.CreateInstance<VoidEventChannelSO>(), "Assets/ScriptableObjects/Events/OnVictory.asset");
        CreateAsset(ScriptableObject.CreateInstance<IntEventChannelSO>(), "Assets/ScriptableObjects/Events/OnWaveStart.asset");
        CreateAsset(ScriptableObject.CreateInstance<VoidEventChannelSO>(), "Assets/ScriptableObjects/Events/OnWaveClear.asset");
        CreateAsset(ScriptableObject.CreateInstance<IntEventChannelSO>(), "Assets/ScriptableObjects/Events/OnElementChanged.asset");
        CreateAsset(ScriptableObject.CreateInstance<DamageEventChannelSO>(), "Assets/ScriptableObjects/Events/OnDamageDealt.asset");
        Debug.Log("[Scorpion] Created all event channel assets");
    }

    private static void CreateAsset(Object asset, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (existing != null)
        {
            Debug.Log($"[Scorpion] Asset already exists: {path}");
            return;
        }
        AssetDatabase.CreateAsset(asset, path);
    }
}
