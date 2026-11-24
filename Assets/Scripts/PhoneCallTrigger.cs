using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PhoneCallTrigger : MonoBehaviour
{
    [Header("전화기 울림 연출")]
    [SerializeField] private GameObject ringingIndicator; // 느낌표/발광 등 울림 표시 오브젝트
    // [SerializeField] private AudioSource phoneAudioSource;
    // [SerializeField] private AudioClip ringClip;
    [SerializeField] private float ringShakeAngle = 8f;
    [SerializeField] private float ringShakeSpeed = 28f;


    private Button Button;
    private string[] QueuedLines;
    private bool IsRinging;
    private Quaternion BaseRotation;

    private void Awake()
    {
        Button = GetComponent<Button>();
        BaseRotation = transform.localRotation;

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(OnPhoneClicked);

        SetRingingVisual(false);
    }

    private void Update()
    {
        if (!IsRinging) return;

        float angle = Mathf.Sin(Time.unscaledTime * ringShakeSpeed) * ringShakeAngle;
        transform.localRotation = BaseRotation * Quaternion.Euler(0f, 0f, angle);
    }

    // 새 컨텐츠가 추가될 때마다 호출
    public void RingPhone(string[] lines = null)
    {
        QueuedLines = (lines != null && lines.Length > 0) ? lines : null;
        IsRinging = true;
        SetRingingVisual(true);

        // if (phoneAudioSource != null && ringClip != null && !phoneAudioSource.isPlaying)
        // {
        //     phoneAudioSource.clip = ringClip;
        //     phoneAudioSource.loop = true;
        //     phoneAudioSource.Play();
        // }
    }

    public void StopRinging()
    {
        IsRinging = false;
        transform.localRotation = BaseRotation;
        SetRingingVisual(false);

        // if (phoneAudioSource != null && phoneAudioSource.isPlaying)
        // {
        //     phoneAudioSource.Stop();
        // }
    }

    private void OnPhoneClicked()
    {
        if (!IsRinging) return;

        string[] linesToPlay = (QueuedLines != null && QueuedLines.Length > 0) ? QueuedLines : null;

        StopRinging();

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialog(linesToPlay);
        }
    }

    private void SetRingingVisual(bool ringing)
    {
        if (ringingIndicator != null)
            ringingIndicator.SetActive(ringing);
    }
}