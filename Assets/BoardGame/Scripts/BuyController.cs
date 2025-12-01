using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class BuyController : MonoBehaviour
{
    public string _donateId = "com.upgarde.dualmode";
    
    public GameObject loadingButton;
    public AudioClip buySound;
    public TextMeshProUGUI buttonText;
    public GameObject panel;
    public GameObject adPanel;

    private const string PurchaseKey = "AdPanelPurchased";

    private void Start()
    {
        bool purchased = PlayerPrefs.GetInt(PurchaseKey, 0) == 1;
        adPanel.SetActive(!purchased);
    }

    public void OnPurchaseComplete(Product product)
    {
        if (product.definition.id == _donateId)
        {
            Debug.Log("Complete");
            
            MusicController.Instance.PlaySpecificSound(buySound);
            loadingButton.SetActive(false);
            panel.SetActive(true);
            
            PlayerPrefs.SetInt(PurchaseKey, 1);
            PlayerPrefs.Save();
            adPanel.SetActive(false);
        }
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
    {
        if (product.definition.id == _donateId)
        {
            loadingButton.SetActive(false);
            Debug.Log($"Failed: {description.message}");
        }
    }
    
    public void OnProductFetched(Product product)
    {
        Debug.Log("Fetched");
        buttonText.text = product.metadata.localizedPriceString;
    }

    public void RestorePurchases()
    {
        var storeListener = CodelessIAPStoreListener.Instance;
        if (storeListener == null || !storeListener.HasProductInCatalog(_donateId))
        {
            Debug.Log("Store not initialized");
            return;
        }

        storeListener.GetStoreExtensions<IAppleExtensions>()?.RestoreTransactions(OnRestoreComplete);
    }

    private void OnRestoreComplete(bool success, string error)
    {
        Debug.Log(success ? "Restore successful" : $"Restore failed: {error}");
    }
}