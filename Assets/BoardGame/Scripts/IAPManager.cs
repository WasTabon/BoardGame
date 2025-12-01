using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    public static IAPManager Instance { get; private set; }
    
    [SerializeField] private string multiplayerProductId = "com.yourcompany.711route.multiplayer";
    
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    
    public bool IsInitialized => storeController != null && extensionProvider != null;
    public bool IsMultiplayerUnlocked { get; private set; }
    
    public event Action<bool> OnPurchaseCompleted;
    public event Action<string> OnPurchaseError;
    public event Action OnRestoreCompleted;
    
    private const string MULTIPLAYER_UNLOCKED_KEY = "MultiplayerUnlocked";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadPurchaseState();
            InitializePurchasing();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadPurchaseState()
    {
        IsMultiplayerUnlocked = PlayerPrefs.GetInt(MULTIPLAYER_UNLOCKED_KEY, 0) == 1;
    }
    
    private void SavePurchaseState()
    {
        PlayerPrefs.SetInt(MULTIPLAYER_UNLOCKED_KEY, IsMultiplayerUnlocked ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void InitializePurchasing()
    {
        if (IsInitialized) return;
        
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(multiplayerProductId, ProductType.NonConsumable);
        
        UnityPurchasing.Initialize(this, builder);
    }
    
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
        
        CheckExistingPurchases();
        
        Debug.Log("IAP initialized successfully");
    }
    
    private void CheckExistingPurchases()
    {
        Product product = storeController.products.WithID(multiplayerProductId);
        if (product != null && product.hasReceipt)
        {
            IsMultiplayerUnlocked = true;
            SavePurchaseState();
        }
    }
    
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"IAP initialization failed: {error}");
    }
    
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"IAP initialization failed: {error} - {message}");
    }
    
    public void PurchaseMultiplayer()
    {
        if (!IsInitialized)
        {
            OnPurchaseError?.Invoke("Store not initialized");
            return;
        }
        
        Product product = storeController.products.WithID(multiplayerProductId);
        
        if (product != null && product.availableToPurchase)
        {
            storeController.InitiatePurchase(product);
        }
        else
        {
            OnPurchaseError?.Invoke("Product not available");
        }
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (string.Equals(args.purchasedProduct.definition.id, multiplayerProductId, StringComparison.Ordinal))
        {
            IsMultiplayerUnlocked = true;
            SavePurchaseState();
            OnPurchaseCompleted?.Invoke(true);
            Debug.Log("Multiplayer mode unlocked!");
        }
        
        return PurchaseProcessingResult.Complete;
    }
    
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogError($"Purchase failed: {product.definition.id} - {failureReason}");
        OnPurchaseError?.Invoke(failureReason.ToString());
    }
    
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
    {
        Debug.LogError($"Purchase failed: {product.definition.id} - {failureDescription.message}");
        OnPurchaseError?.Invoke(failureDescription.message);
    }
    
    public void RestorePurchases()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("Store not initialized");
            return;
        }
        
#if UNITY_IOS
        extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result, error) =>
        {
            if (result)
            {
                CheckExistingPurchases();
                OnRestoreCompleted?.Invoke();
                Debug.Log("Restore purchases completed");
            }
            else
            {
                Debug.LogError($"Restore failed: {error}");
            }
        });
#else
        CheckExistingPurchases();
        OnRestoreCompleted?.Invoke();
#endif
    }
    
    public string GetMultiplayerPrice()
    {
        if (!IsInitialized) return "";
        
        Product product = storeController.products.WithID(multiplayerProductId);
        if (product != null)
        {
            return product.metadata.localizedPriceString;
        }
        
        return "";
    }
}