using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Player")]
    public float speed = 5f;
    public float rotationSpeed = 10f;

    [Header("Components")]
    public TMP_Text nameText;

    [Header("Camera")]
    public Vector3 cameraOffsetPos = new Vector3(0, 6, -7);
    public Vector3 cameraOffsetRot = new Vector3(30, 0, 0);

    private Camera _mainCam;
    private ChangeDetector _changeDetector;

    // ✅ Sintassi corretta Fusion 2 (senza OnChanged)
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        // ✅ Inizializza la UI con il valore sincronizzato
        UpdateNameOnUI();

        if (Object.HasInputAuthority)
        {
            string name = $"Player_{Object.InputAuthority.PlayerId}";
            RpcSetPlayerName(name);

            _mainCam = Camera.main;
            if (_mainCam == null) return;
            _mainCam.transform.rotation = Quaternion.Euler(cameraOffsetRot);

            nameText.enabled = false;
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcSetPlayerName(string name)
    {
        PlayerName = name;

        // ✅ Aggiorna subito la UI locale (per chi ha InputAuthority)
        if (Object.HasInputAuthority)
            UpdateNameOnUI();
    }


    // ✅ Fusion 2: si usa Render() per intercettare i cambiamenti
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(PlayerName))
            {
                UpdateNameOnUI();
            }
        }
    }

    private void UpdateNameOnUI()
    {
        if (nameText != null)
        {
            nameText.text = PlayerName.Value;
        }
    }

    private void LateUpdate()
    {
        if (nameText != null)
            nameText.transform.rotation = Quaternion.Euler(transform.rotation.x, -transform.rotation.y, transform.rotation.z);

        if (Object.HasInputAuthority && _mainCam != null)
        {
            _mainCam.transform.position = transform.position + cameraOffsetPos;
            _mainCam.transform.rotation = Quaternion.Euler(cameraOffsetRot);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.TryGetInputForPlayer(Object.InputAuthority, out NetworkInputData input))
        {
            // Movimento orizzontale
            Vector3 move = new Vector3(input.movementInput.x, 0, input.movementInput.y).normalized * speed;

            transform.position += move;

            // Rotazione solo se ti muovi
            if (move.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Fall"))
        {
            transform.position = GameObject.FindGameObjectWithTag("Respawn").transform.position;
        }
    }
}