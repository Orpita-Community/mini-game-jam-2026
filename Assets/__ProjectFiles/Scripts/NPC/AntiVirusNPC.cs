using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Orpaits.Collectibles;

namespace Orpaits.NPC
{
    [RequireComponent(typeof(Collider2D))]
    public class AntiVirusNPC : MonoBehaviour
    {
        [Header("UI References (Drag from Hierarchy)")]
        [SerializeField] private GameObject npcDialogUI; // The root canvas/object
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button tradeButton;
        [SerializeField] private Button cancelButton;

        [Header("Trade Settings")]
        [SerializeField] private int standardTradeCost = 20;
        [SerializeField] private int bonusTradeCost = 25;
        [SerializeField] private string bossArenaSceneName = "BossArena";
        [SerializeField] private string tradePrefsKey = "Antivirus_Trade_Completed";

        private bool hasTraded;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            
            // Check if we already did this in a previous run
            hasTraded = PlayerPrefs.GetInt(tradePrefsKey, 0) == 1;

            if (npcDialogUI != null)
            {
                npcDialogUI.SetActive(false); // Hide UI at start
            }
        }

        private void OnEnable()
        {
            if (tradeButton != null) tradeButton.onClick.AddListener(OnTradeClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(HideDialog);
        }

        private void OnDisable()
        {
            if (tradeButton != null) tradeButton.onClick.RemoveListener(OnTradeClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(HideDialog);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTraded || !other.CompareTag("Player")) return;
            
            ShowDialog();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            
            HideDialog();
        }

        private void ShowDialog()
        {
            if (npcDialogUI == null) return;
            
            npcDialogUI.SetActive(true);

            int iconCount = IconCollectionManager.Instance != null ? IconCollectionManager.Instance.Count : 0;

            if (iconCount < standardTradeCost)
            {
                messageText.text = $"You have {iconCount} icons. I need at least {standardTradeCost} to forge your gear. Come back later!";
                tradeButton.interactable = false;
            }
            else if (iconCount < bonusTradeCost)
            {
                messageText.text = $"You have {iconCount} icons. I can trade {standardTradeCost} for a Shield + Data Discs. Deal?";
                tradeButton.interactable = true;
            }
            else
            {
                messageText.text = $"You have {iconCount} icons! Trading {standardTradeCost} for Shield + Data Discs, plus BONUS gear. Deal?";
                tradeButton.interactable = true;
            }
        }

        private void HideDialog()
        {
            if (npcDialogUI != null)
            {
                npcDialogUI.SetActive(false);
            }
        }

        public void OnTradeClicked()
        {
            if (hasTraded) return;

            var icons = IconCollectionManager.Instance;
            if (icons != null && icons.SpendIcons(standardTradeCost))
            {
                // Give the player their ammo
                bool getsBonus = icons.Count >= (bonusTradeCost - standardTradeCost);
                int discsAwarded = getsBonus ? 25 : 10;
                PlayerPrefs.SetInt("Player_DataDiscs", discsAwarded);
                
                // Lock the NPC out forever
                hasTraded = true;
                PlayerPrefs.SetInt(tradePrefsKey, 1);
                PlayerPrefs.Save();

                HideDialog();
                SceneManager.LoadScene(bossArenaSceneName);
            }
        }

        [ContextMenu("🎮 Debug: Reset Trade State")]
        public void DebugResetTrade()
        {
            PlayerPrefs.DeleteKey(tradePrefsKey);
            PlayerPrefs.Save();
            hasTraded = false;
            Debug.Log("Trade state reset. You can interact with the NPC again.");
        }
    }
}