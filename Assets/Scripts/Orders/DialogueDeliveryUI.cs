using System;
using System.Collections.Generic;
using System.Linq;
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

    // 默认对话组（当没有配置时使用）
    private static readonly DialogueGroup[] DefaultDialogueGroups = new DialogueGroup[]
    {
        new DialogueGroup()
        {
            round1CustomerLine = "你好，我要买花束！",
            option1Text = "好的，马上为你准备",
            option2Text = "请稍等片刻",
            round2CustomerLine = "太感谢了！"
        },
        new DialogueGroup()
        {
            round1CustomerLine = "我想取我的订单",
            option1Text = "好的，请稍等",
            option2Text = "马上就好",
            round2CustomerLine = "谢谢你！"
        }
    };

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

        Debug.Log($"[DialogueDeliveryUI] StartDialogue 调用，dialogueGroups数量={dialogueGroups?.Count ?? -1}");

        // 获取对话组
        List<DialogueGroup> groupsToUse = dialogueGroups;
        
        // 如果没有配置或所有对话组内容为空，使用默认对话
        if (groupsToUse == null || groupsToUse.Count == 0)
        {
            Debug.LogWarning("[DialogueDeliveryUI] 没有配置对话组，使用默认对话");
            groupsToUse = new List<DialogueGroup>(DefaultDialogueGroups);
        }
        else
        {
            // 检查第一个对话组是否有内容
            bool hasContent = groupsToUse.Any(g => !string.IsNullOrEmpty(g.round1CustomerLine));
            if (!hasContent)
            {
                Debug.LogWarning("[DialogueDeliveryUI] 对话组内容为空，使用默认对话");
                groupsToUse = new List<DialogueGroup>(DefaultDialogueGroups);
            }
        }

        _currentGroup = groupsToUse[UnityEngine.Random.Range(0, groupsToUse.Count)];
        int groupIndex = groupsToUse.IndexOf(_currentGroup);
        Debug.Log($"[DialogueDeliveryUI] 随机选择对话组: {groupIndex}, round1内容: {_currentGroup?.round1CustomerLine ?? "null"}");

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
        Debug.Log($"[DialogueDeliveryUI] ShowRound1 被调用，_currentGroup={( _currentGroup != null ? "非空" : "空")}");
        if (_currentGroup == null)
        {
            Debug.LogWarning("[DialogueDeliveryUI] ShowRound1: _currentGroup 为 null，无法显示");
            return;
        }

        Debug.Log($"[DialogueDeliveryUI] ShowRound1: 显示文本={_currentGroup.round1CustomerLine}");
        if (dialogueText != null)
            dialogueText.text = _currentGroup.round1CustomerLine;

        if (customerPortrait != null && _currentGroup.portrait != null)
            customerPortrait.sprite = _currentGroup.portrait;

        Debug.Log($"[DialogueDeliveryUI] ShowRound1: 选项1={_currentGroup.option1Text}, 选项2={_currentGroup.option2Text}");
        SetupTwoOptions(_currentGroup.option1Text, _currentGroup.option2Text);
    }

    void ShowRound2()
    {
        Debug.Log($"[DialogueDeliveryUI] ShowRound2 被调用，_currentGroup={( _currentGroup != null ? "非空" : "空")}");
        if (_currentGroup == null)
        {
            Debug.LogWarning("[DialogueDeliveryUI] ShowRound2: _currentGroup 为 null，无法显示");
            return;
        }

        Debug.Log($"[DialogueDeliveryUI] ShowRound2: 显示文本={_currentGroup.round2CustomerLine}");
        if (dialogueText != null)
            dialogueText.text = _currentGroup.round2CustomerLine;

        HideOptions();
        ShowCloseButton();
    }

    void SetupTwoOptions(string option1, string option2)
    {
        Debug.Log($"[DialogueDeliveryUI] SetupTwoOptions: option1={option1}, option2={option2}");
        
        HideAllButtons();
        closeButton?.gameObject.SetActive(false);

        if (optionButton0 != null)
        {
            optionButton0.gameObject.SetActive(true);
            if (optionText0 != null)
                optionText0.text = option1;
            else
                Debug.LogWarning("[DialogueDeliveryUI] optionText0 为 null");
            optionButton0.onClick.RemoveAllListeners();
            optionButton0.onClick.AddListener(OnOption1Selected);
        }
        else
        {
            Debug.LogWarning("[DialogueDeliveryUI] optionButton0 为 null");
        }

        if (optionButton1 != null)
        {
            optionButton1.gameObject.SetActive(true);
            if (optionText1 != null)
                optionText1.text = option2;
            else
                Debug.LogWarning("[DialogueDeliveryUI] optionText1 为 null");
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(OnOption2Selected);
        }
        else
        {
            Debug.LogWarning("[DialogueDeliveryUI] optionButton1 为 null");
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
        Debug.Log("[DialogueDeliveryUI] OnCloseClicked 被调用，调用 OnDialogueComplete");
        OnDialogueComplete();
    }

    void OnDialogueComplete()
    {
        Debug.Log("[DialogueDeliveryUI] OnDialogueComplete 被调用");
        
        // 先保存回调引用，再关闭UI
        var callback = _onDialogueComplete;
        Close();
        
        Debug.Log($"[DialogueDeliveryUI] 调用回调，callback={callback != null}");
        callback?.Invoke();
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
