using UnityEngine;
using UnityEngine.UI;

namespace Game.Webhook
{
    /// <summary>
    /// G1 smoke proof: owns the BumpListener lifecycle on the main thread and drives a visible
    /// diagnostic counter for every accepted /bump request. Session-state acceptance rules
    /// (menu/paused/won/lost -> 409, dispatch into gameplay) land in G2 on top of this transport.
    /// </summary>
    public sealed class BumpRunner : MonoBehaviour
    {
        [SerializeField] private Text counterText;

        private BumpListener _listener;
        private int _bumpCount;

        private void Awake()
        {
            _listener = new BumpListener();
            _listener.Faulted += OnListenerFaulted;
        }

        private void OnEnable()
        {
            _listener.Start();
            RefreshCounterText();
        }

        private void OnDisable()
        {
            _listener.Stop();
        }

        private void OnDestroy()
        {
            _listener.Faulted -= OnListenerFaulted;
            _listener.Dispose();
        }

        private void Update()
        {
            BumpRequest request;
            bool changed = false;
            while (_listener.TryDequeue(out request))
            {
                _bumpCount++;
                changed = true;
                Debug.Log($"[Bump] accepted requestId={request.RequestId} method={request.Method} count={_bumpCount}");
            }

            if (changed)
            {
                RefreshCounterText();
            }
        }

        private void RefreshCounterText()
        {
            if (counterText != null)
            {
                counterText.text = "Bumps: " + _bumpCount;
            }
        }

        private void OnListenerFaulted(string message)
        {
            Debug.LogWarning("[Bump] listener error: " + message);
        }
    }
}
