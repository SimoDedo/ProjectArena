using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AI.Guns
{
    public class ControlPointsScorer : GunScorer
    {
        [Serializable]
        private struct ControlPoint
        {
            public float distance;
            public float score;
        }

        [SerializeField] public float minRangeBest;
        [SerializeField] public float maxRangeBest;
        private Tuple<float, float> rangeTuple;

        [SerializeField] private List<ControlPoint> controlPoints;
        private Gun gun;

        private void Awake()
        {
            gun = GetComponent<Gun>();
            controlPoints.Sort((point, controlPoint) => (int) (point.distance - controlPoint.distance));
            rangeTuple = new Tuple<float, float>(minRangeBest, maxRangeBest);
        }

        public override float GetGunScore(float distance, bool fakeEmptyCharger = false)
        {
            var currentAmmo = gun.GetCurrentAmmo();
            if (fakeEmptyCharger) {currentAmmo -= gun.GetLoadedAmmo();}
            if (currentAmmo == 0)
                return 0.0f;

            var score = 0.0f;
            if (distance < controlPoints.First().distance)
                score = controlPoints.First().score;
            if (distance > controlPoints.Last().distance)
                score = controlPoints.Last().score;
            for (var i = 0; i < controlPoints.Count - 1; i++)
            {
                var startPoint = controlPoints[i];
                var endPoint = controlPoints[i + 1];

                if (distance >= startPoint.distance && distance <= endPoint.distance)
                {
                    // interpolate
                    var percent = (distance - startPoint.distance) / (endPoint.distance - startPoint.distance);
                    score = startPoint.score + (endPoint.score - startPoint.score) * percent;
                    break;
                }
            }

            if (gun.GetLoadedAmmo() == 0 || fakeEmptyCharger)
                score *= 0.7f;

            return score;
        }

        public override Tuple<float, float> GetOptimalRange()
        {
            return rangeTuple;
            // var bestScore = 0f;
            // var bestScoreBegin = 0f;
            // var bestScoreEnd = 0f;
            // // Find best score
            // var max = controlPoints.Max(it => it.score);
            // // Find interval containing best score
            // for (var i = 0; i < controlPoints.Count - 1; i++)
            // {
            //     if (controlPoints[i].score == bestScore && controlPoints[i + 1].score == bestScore)
            //     {
            //         var startPoint = controlPoints[i].distance;
            //         var endPoint = controlPoints[i+1].distance;
            //         return new Tuple<float, float>(startPoint, endPoint);
            //     }
            // }
            // // best score is not an interval, return best range +-10%? Or maybe tolerance of best score?
            // for (var i = 1; i < controlPoints.Count-1; i++)
            // {
            //     if (controlPoints[i].score)
            // }
            //
        }
    }
}