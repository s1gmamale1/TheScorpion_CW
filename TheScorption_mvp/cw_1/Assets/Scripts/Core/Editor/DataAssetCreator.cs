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

    [MenuItem("Tools/Scorpion/Wire Wave System")]
    public static void WireWaveSystem()
    {
        // Wire SpawnPointManager spawn points
        var spawnMgr = Object.FindAnyObjectByType<TheScorpion.Systems.SpawnPointManager>();
        if (spawnMgr != null)
        {
            var spawnSO = new SerializedObject(spawnMgr);
            var spawnProp = spawnSO.FindProperty("spawnPoints");
            var north = GameObject.Find("SpawnPoint_North");
            var south = GameObject.Find("SpawnPoint_South");
            var east = GameObject.Find("SpawnPoint_East");
            var west = GameObject.Find("SpawnPoint_West");

            if (north != null && south != null && east != null && west != null)
            {
                spawnProp.arraySize = 4;
                spawnProp.GetArrayElementAtIndex(0).objectReferenceValue = north.transform;
                spawnProp.GetArrayElementAtIndex(1).objectReferenceValue = south.transform;
                spawnProp.GetArrayElementAtIndex(2).objectReferenceValue = east.transform;
                spawnProp.GetArrayElementAtIndex(3).objectReferenceValue = west.transform;
                spawnSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawnMgr);
                Debug.Log("[Scorpion] SpawnPointManager: 4 spawn points wired");
            }
            else
            {
                Debug.LogError("[Scorpion] SpawnPointManager: Could not find all spawn points!");
            }
        }

        // Wire WaveManager
        var waveMgr = Object.FindAnyObjectByType<TheScorpion.Systems.WaveManager>();
        if (waveMgr != null)
        {
            var waveSO = new SerializedObject(waveMgr);

            // Wave data
            var waveData = AssetDatabase.LoadAssetAtPath<WaveDataSO>("Assets/ScriptableObjects/WaveData/Level1_Waves.asset");
            if (waveData != null)
                waveSO.FindProperty("waveData").objectReferenceValue = waveData;

            // Enemy data SOs
            var monkData = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/HollowMonk_Data.asset");
            var acolyteData = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/ShadowAcolyte_Data.asset");
            var sentinelData = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/StoneSentinel_Data.asset");
            var bossData = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/FallenGuardian_Data.asset");

            if (monkData != null) waveSO.FindProperty("basicEnemyData").objectReferenceValue = monkData;
            if (acolyteData != null) waveSO.FindProperty("fastEnemyData").objectReferenceValue = acolyteData;
            if (sentinelData != null) waveSO.FindProperty("heavyEnemyData").objectReferenceValue = sentinelData;
            if (bossData != null) waveSO.FindProperty("bossData").objectReferenceValue = bossData;

            // Event channels
            var onEnemyKilled = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/ScriptableObjects/Events/OnEnemyKilled.asset");
            var onWaveChanged = AssetDatabase.LoadAssetAtPath<IntEventChannelSO>("Assets/ScriptableObjects/Events/OnWaveStart.asset");
            var onAllWavesCleared = AssetDatabase.LoadAssetAtPath<VoidEventChannelSO>("Assets/ScriptableObjects/Events/OnVictory.asset");
            var onAdrenalineGain = AssetDatabase.LoadAssetAtPath<IntEventChannelSO>("Assets/ScriptableObjects/Events/OnElementChanged.asset");

            if (onEnemyKilled != null) waveSO.FindProperty("onEnemyKilledEvent").objectReferenceValue = onEnemyKilled;
            if (onWaveChanged != null) waveSO.FindProperty("onWaveChangedEvent").objectReferenceValue = onWaveChanged;
            if (onAllWavesCleared != null) waveSO.FindProperty("onAllWavesClearedEvent").objectReferenceValue = onAllWavesCleared;

            // Enemy prefabs — use existing scene enemies as template
            // For now, reference them directly; will convert to prefabs later
            var enemyA = GameObject.Find("EnemyAI_A");
            var enemyB = GameObject.Find("EnemyAI_B");
            var enemyC = GameObject.Find("EnemyAI_C");

            // Disable auto-start until prefabs are ready
            waveSO.FindProperty("autoStartWaves").boolValue = false;

            waveSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(waveMgr);
            Debug.Log("[Scorpion] WaveManager: data + events wired (prefabs need manual assignment)");
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Scorpion/Create Enemy Prefabs")]
    public static void CreateEnemyPrefabs()
    {
        // Ensure prefab folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Enemies"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Enemies");

        // Create prefabs from scene enemies
        var enemyA = GameObject.Find("EnemyAI_A");
        var enemyB = GameObject.Find("EnemyAI_B");
        var enemyC = GameObject.Find("EnemyAI_C");

        string basicPath = "Assets/Prefabs/Enemies/HollowMonk_Prefab.prefab";
        string fastPath = "Assets/Prefabs/Enemies/ShadowAcolyte_Prefab.prefab";
        string heavyPath = "Assets/Prefabs/Enemies/StoneSentinel_Prefab.prefab";

        if (enemyA != null && !AssetDatabase.LoadAssetAtPath<GameObject>(basicPath))
        {
            PrefabUtility.SaveAsPrefabAsset(enemyA, basicPath);
            Debug.Log("[Scorpion] Created HollowMonk prefab from EnemyAI_A");
        }
        if (enemyB != null && !AssetDatabase.LoadAssetAtPath<GameObject>(fastPath))
        {
            PrefabUtility.SaveAsPrefabAsset(enemyB, fastPath);
            Debug.Log("[Scorpion] Created ShadowAcolyte prefab from EnemyAI_B");
        }
        if (enemyC != null && !AssetDatabase.LoadAssetAtPath<GameObject>(heavyPath))
        {
            PrefabUtility.SaveAsPrefabAsset(enemyC, heavyPath);
            Debug.Log("[Scorpion] Created StoneSentinel prefab from EnemyAI_C");
        }

        AssetDatabase.SaveAssets();

        // Now wire them to WaveManager
        var waveMgr = Object.FindAnyObjectByType<TheScorpion.Systems.WaveManager>();
        if (waveMgr != null)
        {
            var waveSO = new SerializedObject(waveMgr);
            var basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basicPath);
            var fastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fastPath);
            var heavyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(heavyPath);

            if (basicPrefab != null) waveSO.FindProperty("basicEnemyPrefab").objectReferenceValue = basicPrefab;
            if (fastPrefab != null) waveSO.FindProperty("fastEnemyPrefab").objectReferenceValue = fastPrefab;
            if (heavyPrefab != null) waveSO.FindProperty("heavyEnemyPrefab").objectReferenceValue = heavyPrefab;

            waveSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(waveMgr);
            Debug.Log("[Scorpion] WaveManager: enemy prefabs assigned");
        }

        Debug.Log("[Scorpion] Enemy prefabs created and wired!");
    }

    [MenuItem("Tools/Scorpion/Rebalance All Data")]
    public static void RebalanceAllData()
    {
        // === ELEMENT DATA ===
        var fire = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Fire_Data.asset");
        if (fire != null)
        {
            fire.ability1Damage = 8f;      // Fire Tornado: 8/tick × 3s = 24 total (was 15/tick = 45)
            fire.ability1Radius = 4f;
            fire.ability2BurnDamagePerTick = 3f; // Aura burn: 3/tick (was 5)
            fire.burstDamage = 30f;         // Ultimate burst (was 60)
            fire.burstRadius = 8f;
            EditorUtility.SetDirty(fire);
        }

        var lightning = AssetDatabase.LoadAssetAtPath<ElementDataSO>("Assets/ScriptableObjects/ElementData/Lightning_Data.asset");
        if (lightning != null)
        {
            lightning.ability1Damage = 10f;  // Lightning Burst: 10 instant (was 20)
            lightning.ability1Radius = 3f;
            lightning.burstDamage = 20f;     // Ultimate burst (was 40)
            lightning.burstRadius = 12f;
            lightning.burstStunDuration = 2f;
            EditorUtility.SetDirty(lightning);
        }

        // === ENEMY DATA — increase HP for longer fights ===
        var monk = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/HollowMonk_Data.asset");
        if (monk != null)
        {
            monk.maxHealth = 60;     // Was 30 — needs ~6 melee hits to kill
            monk.attackDamage = 8;
            EditorUtility.SetDirty(monk);
        }

        var acolyte = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/ShadowAcolyte_Data.asset");
        if (acolyte != null)
        {
            acolyte.maxHealth = 40;  // Was 20
            acolyte.attackDamage = 10;
            EditorUtility.SetDirty(acolyte);
        }

        var sentinel = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/StoneSentinel_Data.asset");
        if (sentinel != null)
        {
            sentinel.maxHealth = 120; // Was 80
            sentinel.attackDamage = 18;
            EditorUtility.SetDirty(sentinel);
        }

        var boss = AssetDatabase.LoadAssetAtPath<EnemyDataSO>("Assets/ScriptableObjects/EnemyData/FallenGuardian_Data.asset");
        if (boss != null)
        {
            boss.maxHealth = 500;    // Was 300
            boss.attackDamage = 15;
            EditorUtility.SetDirty(boss);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Scorpion] REBALANCED: Monk HP=60, Acolyte HP=40, Sentinel HP=120, Boss HP=500 | Projectile=5, FireTornado=8/tick, LBurst=10, FireBurst=30, LBurst=20");
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
