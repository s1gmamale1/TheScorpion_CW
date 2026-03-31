using UnityEngine;
using UnityEditor;
using Invector.vCharacterController;
using Invector.vMelee;

public class PlayerSetupTool
{
    [MenuItem("Tools/Scorpion/Setup Player")]
    public static void SetupPlayer()
    {
        var meleeInput = Object.FindAnyObjectByType<vMeleeCombatInput>();
        if (meleeInput == null)
        {
            Debug.LogError("[Scorpion] No vMeleeCombatInput found in scene! Create a melee controller first.");
            return;
        }
        var player = meleeInput.gameObject;

        // Remap roll input from Q to LeftAlt
        meleeInput.rollInput = new GenericInput("LeftAlt", "B", "B");
        // Disable crouch (we don't need it, C is used for projectiles)
        meleeInput.crouchInput = new GenericInput("", "", "");
        Debug.Log("[Scorpion] Remapped rollInput Q→LeftAlt, disabled crouch");
        EditorUtility.SetDirty(meleeInput);

        // Add components
        var elementSystem = GetOrAdd<TheScorpion.Player.ElementSystem>(player);
        var ultimateSystem = GetOrAdd<TheScorpion.Player.UltimateSystem>(player);
        var styleMeter = GetOrAdd<TheScorpion.Player.StyleMeter>(player);
        var damageInterceptor = GetOrAdd<TheScorpion.Combat.DamageInterceptor>(player);
        var deathHandler = GetOrAdd<TheScorpion.Player.PlayerDeathHandler>(player);
        var inputHandler = GetOrAdd<TheScorpion.Player.ScorpionInputHandler>(player);

        // Wire serialized references using SerializedObject
        // ScorpionInputHandler -> ElementSystem, UltimateSystem
        var inputSO = new SerializedObject(inputHandler);
        inputSO.FindProperty("elementSystem").objectReferenceValue = elementSystem;
        inputSO.FindProperty("ultimateSystem").objectReferenceValue = ultimateSystem;
        inputSO.ApplyModifiedProperties();

        // DamageInterceptor -> ElementSystem, UltimateSystem
        var dmgSO = new SerializedObject(damageInterceptor);
        dmgSO.FindProperty("elementSystem").objectReferenceValue = elementSystem;
        dmgSO.FindProperty("ultimateSystem").objectReferenceValue = ultimateSystem;
        dmgSO.ApplyModifiedProperties();

        // UltimateSystem -> ElementSystem
        var ultSO = new SerializedObject(ultimateSystem);
        ultSO.FindProperty("elementSystem").objectReferenceValue = elementSystem;
        ultSO.ApplyModifiedProperties();

        EditorUtility.SetDirty(player);
        Debug.Log("[Scorpion] Player setup complete! All references wired.");
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
        {
            comp = go.AddComponent<T>();
            Debug.Log($"[Scorpion] Added {typeof(T).Name}");
        }
        return comp;
    }
}
