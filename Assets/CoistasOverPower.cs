using UnityEngine;

public class CoistasOverPower : MonoBehaviour
{
    [SerializeField] private string pageURL = "https://linktr.ee/opstudiosmed?utm_source=linktree_profile_share&ltsid=a12bc161-b81c-4a61-a982-0b54d094155a";

    /// <summary>
    /// Abre una página web en el navegador.
    /// Conecta este método al evento OnClick del botón.
    /// </summary>
    public void OpenPage()
    {
        Application.OpenURL(pageURL);
        Debug.Log("Abriendo: " + pageURL);
    }
}
