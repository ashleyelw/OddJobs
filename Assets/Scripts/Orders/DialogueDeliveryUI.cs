using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueDeliveryUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] Text dialogueText;
    [SerializeField] Image customerPortrait;

    [Header("选项按钮（只需要2个）")]
    [SerializeField] Button optionButton0;
    [SerializeField] Button optionButton1;
    [SerializeField] Text optionText0;
    [SerializeField] Text optionText1;

    [Header("关闭按钮（第二轮显示）")]
    [SerializeField] Button closeButton;

    [Header("对话组配置（每次随机选择一组）")]
    [SerializeField] List<DialogueGroup> dialogueGroups = new List<DialogueGroup>();

    private int _currentRound = 0;
    private DialogueGroup _currentGroup;
    private Action _onDialogueComplete;
    private Action _onDialogueCancelled;

    [Serializable]
    public class DialogueGroup
    {
        [Header("第一轮 - 客户开场白")]
        [TextArea(2, 4)]
        public string round1CustomerLine;
        [Header("第一轮选项")]
        public string option1Text;
        public string option2Text;

        [Header("第二轮 - 客户回应")]
        [TextArea(2, 4)]
        public string round2CustomerLine;

        [Header("头像（可选）")]
        public Sprite portrait;
    }

    public void StartDialogue(Action onComplete, Action onCancelled = null)
    {
        _onDialogueComplete = onComplete;
        _onDialogueCancelled = onCancelled;
        _currentRound = 0;

        if (dialogueGroups == null || dialogueGroups.Count == 0)
        {
            Debug.LogWarning("[DialogueDeliveryUI] 没有配置对话组");
            OnDialogueComplete();
            return;
        }

        _currentGroup = dialogueGroups[UnityEngine.Random.Range(0, dialogueGroups.Count)];
        Debug.Log($"[DialogueDeliveryUI] 随机选择对话组: {dialogueGroups.IndexOf(_currentGroup)}");

        gameObject.SetActive(true);
        ShowRound(1);
    }

    void ShowRound(int round)
    {
        _currentRound = round;

        if (round == 1)
        {
            ShowRound1();
        }
        else if (round == 2)
        {
            ShowRound2();
        }
    }

    void ShowRound1()
    {
        if (_currentGroup == null) return;

        if (dialogueText != null)
            dialogueText.text = _currentGroup.round1CustomerLine;

        if (customerPortrait != null && _currentGroup.portrait != null)
            customerPortrait.sprite = _currentGroup.portrait;

        SetupTwoOptions(_currentGroup.option1Text, _currentGroup.option2Text);
    }

    void ShowRound2()
    {
        if (_currentGroup == null) return;

        if (dialogueText != null)
            dialogueText.text = _currentGroup.round2CustomerLine;

        HideOptions();
        ShowCloseButton();
    }

    void SetupTwoOptions(string option1, string option2)
    {
        HideAllButtons();
        closeButton?.gameObject.SetActive(false);

        if (optionButton0 != null)
        {
            optionButton0.gameObject.SetActive(true);
            if (optionText0 != null)
                optionText0.text = option1;
            optionButton0.onClick.RemoveAllListeners();
            optionButton0.onClick.AddListener(OnOption1Selected);
        }

        if (optionButton1 != null)
        {
            optionButton1.gameObject.SetActive(true);
            if (optionText1 != null)
                optionText1.text = option2;
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(OnOption2Selected);
        }
    }

    void HideOptions()
    {
        if (optionButton0 != null) optionButton0.gameObject.SetActive(false);
        if (optionButton1 != null) optionButton1.gameObject.SetActive(false);
    }

    void HideAllButtons()
    {
        HideOptions();
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    void ShowCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    void OnOption1Selected()
    {
        Debug.Log("[DialogueDeliveryUI] 玩家选择选项1");
        ShowRound(2);
    }

    void OnOption2Selected()
    {
        Debug.Log("[DialogueDeliveryUI] 玩家选择选项2");
        ShowRound(2);
    }

    void OnCloseClicked()
    {
        Debug.Log("[DialogueDeliveryUI] 关闭对话");
        OnDialogueComplete();
    }

    void OnDialogueComplete()
    {
        Close();
        _onDialogueComplete?.Invoke();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        _onDialogueComplete = null;
        _onDialogueCancelled = null;
        _currentGroup = null;
    }

    public static DialogueDeliveryUI CreateInScene(Transform canvasParent, DialogueDeliveryUI prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[DialogueDeliveryUI] Prefab is null!");
            return null;
        }

        var instance = UnityEngine.Object.Instantiate(prefab, canvasParent);
        instance.gameObject.SetActive(false);
        return instance;
    }
}
