using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI sentenceText;

    private Queue<string> sentences;

    void Start()
    {
        sentences = new Queue<string>();
    }

    void Update()
    {
        if (dialoguePanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Space))
        {
            DisplayNextSentence();
        }
    }

    public void StartDialogue(Dialogue dialogue)
    {
        FindObjectOfType<PlayerMove>().canMove = false;

        dialoguePanel.SetActive(true);
        nameText.text = dialogue.name;

        sentences.Clear();

        foreach (string sentence in dialogue.sentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        sentenceText.text = sentence;
    }

    void EndDialogue()
    {
        FindObjectOfType<PlayerMove>().canMove = true;

        dialoguePanel.SetActive(false);
    }
}