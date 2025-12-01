using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class BuyController : MonoBehaviour, IDetailedStoreListener
{
    public string _donateId = "com.htcpurchases.coinsmain";
    
    public GameObject loadingButton;
    public AudioClip buySound;
    public TextMeshProUGUI buttonText;
    public GameObject panel;
    public GameObject adPanel;

    private const string PurchaseKey = "AdPanelPurchased";
    
    private IStoreController _storeController;
    private IExtensionProvider _extensionProvider;

    private void Start()
    {
        bool purchased = PlayerPrefs.GetInt(PurchaseKey, 0) == 1;
        adPanel.SetActive(!purchased);
        
        InitializePurchasing();
    }

    private void InitializePurchasing()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(_donateId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _storeController = controller;
        _extensionProvider = extensions;
        
        var product = _storeController.products.WithID(_donateId);
        if (product != null && product.availableToPurchase)
        {
            OnProductFetched(product);
        }
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log($"IAP Init Failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log($"IAP Init Failed: {error} - {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == _donateId)
        {
            Debug.Log("Complete");
            
            MusicController.Instance.PlaySpecificSound(buySound);
            loadingButton.SetActive(false);
            panel.SetActive(true);
            
            PlayerPrefs.SetInt(PurchaseKey, 1);
            PlayerPrefs.Save();
            adPanel.SetActive(false);
        }
        
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
    {
        if (product.definition.id == _donateId)
        {
            loadingButton.SetActive(false);
            Debug.Log($"Failed: {reason}");
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
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            _extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(OnRestoreComplete);
        }
        else
        {
            Debug.Log("Android: покупки восстанавливаются автоматически");
        }
    }

    private void OnRestoreComplete(bool success, string error)
    {
        if (success)
        {
            Debug.Log("Restore successful");
        }
        else
        {
            Debug.Log($"Restore failed: {error}");
        }
    }
}