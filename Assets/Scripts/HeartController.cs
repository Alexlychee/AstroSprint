
using UnityEngine;
using UnityEngine.UI;

public class HeartController : MonoBehaviour {
    private GameObject[] heartContainers;
    private Image[] heartFills;
    public Transform heartsParent;
    public GameObject heartContainerPrefab;
    // Start is called before the first frame update
    void Start() {
        heartContainers = new GameObject[playerController.Instance.maxHealth];
        heartFills = new Image[playerController.Instance.maxHealth];

        playerController.Instance.onHealthChangedCallback += UpdateHeartsHUD;
        InstantiateHeartContainers();
        UpdateHeartsHUD();
    }

    // Update is called once per frame
    void Update() {

    }
    void SetHeartContainers() {
        for (int i = 0; i < heartContainers.Length; i++) {
            if (i < playerController.Instance.maxHealth) {
                heartContainers[i].SetActive(true);
            } else {
                heartContainers[i].SetActive(false);
            }
        }
    }
    void SetFilledHearts() {
        for (int i = 0; i < heartFills.Length; i++) {
            if (i < playerController.Instance.Health) {
                heartFills[i].fillAmount = 1;
            } else {
                heartFills[i].fillAmount = 0;
            }
        }
    }
    void InstantiateHeartContainers() {
        for (int i = 0; i < playerController.Instance.maxHealth; i++) {
            GameObject temp = Instantiate(heartContainerPrefab);
            temp.transform.SetParent(heartsParent, false);
            heartContainers[i] = temp;
            heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }
    void UpdateHeartsHUD() {
        SetHeartContainers();
        SetFilledHearts();
    }
}
