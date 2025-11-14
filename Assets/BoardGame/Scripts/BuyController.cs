using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;

public class BuyController : MonoBehaviour
{
    public string _donateId = "com.htcpurchases.coinsmain";
    
    public GameObject loadingButton;
    public AudioClip buySound;
    public TextMeshProUGUI buttonText;
    public GameObject panel;
    
    public void OnPurchaseComlete(Product product)
    {
        if (product.definition.id == _donateId)
        {
            Debug.Log("Complete");
            
            MusicController.Instance.PlaySpecificSound(buySound);
            loadingButton.SetActive(false);
            panel.SetActive(true);
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
}
