﻿using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    ///     This class allows to fade an image.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FadeUI : MonoBehaviour
    {
        [SerializeField] private float minimum;
        [SerializeField] private float maximum = 0.8f;
        [SerializeField] private Image fadeImage;

        private float duration = 1f;

        // Do I have to fade?
        private bool mustFade;

        // Do I have to ligthen?
        private bool mustLigthen = true;
        private float startTime;

        private void Update()
        {
            if (mustFade)
            {
                var t = (Time.time - startTime) / duration;

                if (mustLigthen == false)
                    SetAlpha(fadeImage, Mathf.SmoothStep(minimum, maximum, t));
                else
                    SetAlpha(fadeImage, Mathf.SmoothStep(maximum, minimum, t));
            }
        }

        // Sets the fade to the maximum value.
        public void SetDark()
        {
            mustLigthen = false;
            SetAlpha(fadeImage, 1f);
        }

        // Fades.
        public void StartFade(bool ml)
        {
            startTime = Time.time;

            mustLigthen = ml;

            if (mustLigthen == false)
                SetAlpha(fadeImage, minimum);
            else
                SetAlpha(fadeImage, maximum);

            mustFade = true;
        }

        // Sets the maximum and minimum values and fades.
        public void StartFade(float min, float max, bool ml, float d)
        {
            minimum = min;
            maximum = max;
            duration = d;

            StartFade(ml);
        }

        public bool HasFaded()
        {
            if (mustLigthen)
            {
                if (fadeImage.color.a == minimum)
                    return true;
                return false;
            }

            if (fadeImage.color.a == maximum)
                return true;
            return false;
        }

        public void SetMinMax(float min, float max)
        {
            minimum = min;
            maximum = max;
        }

        public void ResetAlpha(bool max)
        {
            if (max)
            {
                SetAlpha(fadeImage, maximum);
                mustLigthen = false;
            }
            else
            {
                SetAlpha(fadeImage, minimum);
                mustLigthen = true;
            }
        }

        // Sets the alpha of the image passed as parameter.
        private void SetAlpha(Image image, float alpha)
        {
            var temp = image.color;
            temp.a = alpha;
            image.color = temp;
        }
    }
}