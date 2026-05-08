using UnityEngine;
using System.Collections.Generic;

// ВАРИАНТ ОТВЕТА ИГРОКА
[System.Serializable]
public class DialogueChoice
{
    public string choiceText; 
    public int nextNodeIndex = -1; 

    [Header("События (Сработают ПРИ КЛИКЕ на этот ответ)")]
    public bool takeQuest = false; 
    public bool turnInQuest = false; 
}

// УЗЕЛ ДИАЛОГА (Фраза NPC)
[System.Serializable]
public class DialogueNode
{
    [TextArea(3, 5)]
    public string npcText;

    [Header("События (Сработают ПРИ ПОЯВЛЕНИИ этого текста)")]
    public bool takeQuest = false;   
    public bool turnInQuest = false; 
    
    [Tooltip("ЛКМ по этому тексту завершит диалог, даже если есть следующий узел")]
    public bool forceEndDialogue = false;

    [Header("Варианты ответа")]
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

// САМ ФАЙЛ ДИАЛОГА
[CreateAssetMenu(fileName = "New Dialogue", menuName = "RPG/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string npcName = "Незнакомец";
    public QuestData quest;

    [Header("Настройки Голоса NPC")]
    [Tooltip("Задержка между буквами (0.05 - стандарт)")]
    public float typingSpeed = 0.05f; 
    [Tooltip("Звук, который проигрывается при каждой букве")]
    public AudioClip typingSound;
    [Range(0.1f, 2.0f)]
    public float voicePitch = 1.0f; // Высота голоса (0.5 - бас, 1.5 - пискля)

    [Header("Стартовые узлы (Индексы)")]
    public int startNodeIndex = 0;
    public int inProgressNodeIndex = 0;
    public int completedNodeIndex = 0;
    public int afterQuestNodeIndex = 0;

    [Header("Все узлы диалога")]
    public List<DialogueNode> nodes = new List<DialogueNode>();
}