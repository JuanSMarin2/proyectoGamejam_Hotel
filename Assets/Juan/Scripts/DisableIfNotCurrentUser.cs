using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Desactiva los GameObjects de la lista si no hay un usuario registrado.
/// El checkeo se hace UNA sola vez en Start.
/// Usa CurrentUser.Instance (singleton persistente), no requiere asignar
/// ninguna referencia al CurrentUser en el Inspector.
/// </summary>
public class DisableIfNotCurrentUser : MonoBehaviour
{
    [Header("Objetos a desactivar si no hay usuario registrado")]
    [SerializeField] private List<GameObject> objectsToDisable = new List<GameObject>();

    private void Start()
    {
        // Si hay un usuario registrado, no se hace nada
        if (CurrentUser.Instance.HasUserName)
            return;

        // Sin usuario registrado: desactivar todos los objetos de la lista
        foreach (GameObject target in objectsToDisable)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }
}