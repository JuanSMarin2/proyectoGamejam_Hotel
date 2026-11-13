using UnityEngine;

public class OpenExternalLink : MonoBehaviour
{
    [Tooltip("URL que se abrira en el navegador predeterminado")]
    [SerializeField] private string url = "https://";

    /// <summary>
    /// Abre el link configurado en el Inspector en el navegador predeterminado.
    /// </summary>
    public void OpenURL()
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("OpenExternalLink: No se ha configurado ninguna URL.", this);
            return;
        }

        Application.OpenURL(url.Trim());
    }

    /// <summary>
    /// Abre una URL especifica pasada por parametro (util para llamarlo con parametros desde eventos de UI).
    /// </summary>
    public void OpenCustomURL(string customUrl)
    {
        if (string.IsNullOrWhiteSpace(customUrl))
        {
            Debug.LogWarning("OpenExternalLink: La URL pasada esta vacia.", this);
            return;
        }

        Application.OpenURL(customUrl.Trim());
    }

    /// <summary>
    /// Permite cambiar la URL serializada desde codigo.
    /// </summary>
    public void SetURL(string newUrl)
    {
        url = newUrl;
    }
}
