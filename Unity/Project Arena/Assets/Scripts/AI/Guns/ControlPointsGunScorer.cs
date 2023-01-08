using System;
using System.Collections.Generic;
using Guns;
using UnityEngine;

namespace AI.Guns
{
    /// <summary>
    /// GunScorer based on control points to evaluate the score itself.
    /// </summary>
    public class ControlPointsGunScorer : GunScorer
    {
        [SerializeField] public float minRangeBest;
        [SerializeField] public float maxRangeBest;

        [SerializeField] private List<ControlPoint> controlPoints;
        private readonly AnimationCurve scoreCurve = new AnimationCurve();
        private Gun gun;
        private Tuple<float, float> rangeTuple;

        private void Awake()
        {
            gun = GetComponent<Gun>();
            foreach (var point in controlPoints) scoreCurve.AddKey(point.distance, point.score);

            rangeTuple = new Tuple<float, float>(minRangeBest, maxRangeBest);
        }

        public override float GetGunScore(float distance, bool fakeEmptyCharger = false)
        {
            var currentAmmo = gun.GetCurrentAmmo();
            if (fakeEmptyCharger) currentAmmo -= gun.GetLoadedAmmo();
            if (currentAmmo == 0)
                return 0.0f;

            var score = scoreCurve.Evaluate(distance);

            if (gun.GetLoadedAmmo() == 0 || fakeEmptyCharger)
                score *= 0.7f;

            return score;
        }

        public override Tuple<float, float> GetOptimalRange()
        {
            return rangeTuple;
        }

        [Serializable]
        private struct ControlPoint
        {
            public float distance;
            public float score;
        }
    }
}