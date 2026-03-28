using UnityEngine;
using UnityEngine.UI;
using TheScorpion.Player;

namespace TheScorpion.UI
{
    /// <summary>
    /// Drives the energy Slider in the Invector HUD.
    /// Reads ElementSystem.CurrentEnergy every frame and updates the slider value.
    /// Attach to the player GameObject (same as ElementSystem).
    /// </summary>
    public class EnergyBarController : MonoBehaviour
    {
        private Slider energySlider;
        private ElementSystem elementSystem;

        private void Start()
        {
            elementSystem = GetComponent<ElementSystem>();

            // Find the energy slider under the player's Invector HUD
            // Path: Invector Components/UI/HUD Melee/energy
            var energyTransform = transform.Find("Invector Components/UI/HUD Melee/energy");
            if (energyTransform != null)
            {
                energySlider = energyTransform.GetComponent<Slider>();
                if (energySlider != null)
                {
                    energySlider.maxValue = elementSystem != null ? elementSystem.MaxEnergy : 100f;
                    energySlider.value = energySlider.maxValue;
                    Debug.Log("[Scorpion] EnergyBarController: Found energy slider, linked to ElementSystem");
                }
            }
            else
            {
                Debug.LogWarning("[Scorpion] EnergyBarController: Could not find 'Invector Components/UI/HUD Melee/energy'");
            }
        }

        private void Update()
        {
            if (energySlider == null || elementSystem == null) return;

            energySlider.maxValue = elementSystem.MaxEnergy;
            energySlider.value = elementSystem.CurrentEnergy;
        }
    }
}
