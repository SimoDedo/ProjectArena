using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckboxQuestion : MonoBehaviour {

    [SerializeField] private int questionId;
    [SerializeField] private Option[] options;
    [SerializeField] private Button submit;
    [SerializeField] private bool exclusive;
    [SerializeField] private bool compulsory;

    private int activeCount = 0;

    private void Start() {
        if (!compulsory)
            submit.interactable = true;
    }

    // Updates the active answers.
    public void UpdateAnswer(int id) {
        bool activated = GetOptionById(id).toggle.isOn;

        if (exclusive && activated)
            foreach (Option o in options)
                if (o.id != id && o.toggle.isOn)
                    // Updating the isOn value calls again this method, so there is non need
                    // to decrease the active count now.
                    o.toggle.isOn = false;

        activeCount = activated ? activeCount + 1 : activeCount - 1;
        submit.interactable = activeCount > 0 || !compulsory ? true : false;
    }

    // Returns the oprion given its id.
    private Option GetOptionById(int id) {
        foreach (Option o in options)
            if (o.id == id)
                return o;
        return new Option();
    }

    // Returns the active answers as an array.
    private int[] GetActiveAnswers() {
        int[] activeAnswers = new int[activeCount];

        int j = 0;
        for (int i = 0; i < options.Length; i++)
            if (options[i].toggle.isOn) {
                activeAnswers[j] = options[i].id;
                j++;
            }

        return activeAnswers;
    }

    // Converts the answe in Json format.
    private string GetJsonAnwer() {
        return JsonUtility.ToJson(new JsonAnswer {
            questionId = questionId,
            anwers = GetActiveAnswers()
        });
    }

    // Converts the question and the options in Json format.
    private string GetJsonQuestion() {
        string jq = JsonUtility.ToJson(new JsonQuestion {
            questionId = questionId,
            questionText = transform.GetComponentInChildren<Text>().text,
            options = ""
        });
        return jq.Remove(jq.Length - 3) + GetJsonOptions() + "}";
    }

    // Converts the options in Json format.
    private string GetJsonOptions() {
        string jOptions = "[";

        for (int i = 0; i < options.Length; i++) {
            jOptions += JsonUtility.ToJson(new JsonOption {
                optionId = (int)options[i].id,
                optionText = options[i].toggle.transform.parent.GetComponentInChildren<Text>().text
            });
            if (i < options.Length - 1)
                jOptions += ", ";
        }

        return jOptions + "]";
    }

    private class JsonQuestion {
        public int questionId;
        public string questionText;
        public string options;
    }

    private class JsonOption {
        public int optionId;
        public string optionText;
    }

    private class JsonAnswer {
        public int questionId;
        public int[] anwers;
    }

    [Serializable]
    private struct Option {
        public Toggle toggle;
        public int id;
    }
}
