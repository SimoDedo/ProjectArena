using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Logging;
using UnityEngine;

namespace AI.Layers.Statistics
{
    public class LoggingComponent
    {
        // Di cosa devo mettermi in ascolto qui?
        // Entrata e uscita da fight di mia entity
        // Respawn 

        private readonly AIEntity entity;

        private bool loggedFirstRespawn;

        private float lastRespawnTime;
        private float currentFightStartTime;
        private float previousFightEndTime;

        private int numberOfFights;
        private int numberOfRetreats;

        private float totalTimeBetweenSights;

        private float totalTimeInFight;

        private float totalTimeToEngage;

        private int numberOfSights;

        private float totalTimeToSurrender;

        // Focusing status of the current frame.
        // It might change any time and any amount of time between calls to Update()
        private bool isFocusingOnEnemy;

        // Focusing status of the previous frame
        private bool previousFocusingStatus;

        private bool wasEnemyVisibleLastFrame;
        private float enemySightLossTime;

        public LoggingComponent(AIEntity entity)
        {
            this.entity = entity;
            SpawnInfoGameEvent.Instance.AddListener(RespawnEvent);
            FocusingOnEnemyGameEvent.Instance.AddListener(FocusingEvent);
        }


        public void Update()
        {
            // if (!entity.IsAlive) return;
            if (previousFocusingStatus != isFocusingOnEnemy)
            {
                // Debug.Log(entity.name + " changed focusing to " + isFocusingOnEnemy);
                if (isFocusingOnEnemy)
                {
                    // I am now in combat
                    currentFightStartTime = Time.time;
                    totalTimeToEngage += currentFightStartTime - Mathf.Max(lastRespawnTime, previousFightEndTime);
                    numberOfFights++;
                    wasEnemyVisibleLastFrame = true;
                }
                else
                {
                    // I am no longer in combat
                    previousFightEndTime = Time.time;
                    totalTimeInFight += previousFightEndTime - currentFightStartTime;
                    if (entity.GetEnemy().IsAlive)
                    {
                        // I am no longer in combat but the enemy is alive. Did I gave up on searching?
                        // TODO Avoid the need to use max here (I never detected the enemy in the first place).
                        totalTimeToSurrender += Mathf.Max(0f, Time.time - Math.Max(0f, entity.TargetKnowledgeBase.LastTimeDetected));
                        numberOfRetreats++; 
                    }
                }

                previousFocusingStatus = isFocusingOnEnemy;
            }

            if (isFocusingOnEnemy)
            {
                var isCurrentlyVisible = entity.TargetMemory.GetEnemyInfo().Last().isVisible;
                if (wasEnemyVisibleLastFrame != isCurrentlyVisible)
                {
                    // Debug.Log(entity.name + " changed target sight to " + isCurrentlyVisible);
                    if (!isCurrentlyVisible)
                    {
                        // Enemy sight lost
                        enemySightLossTime = Time.time;
                    }
                    else
                    {
                        // Enemy sight reestablished
                        totalTimeBetweenSights = Time.time - enemySightLossTime;
                        numberOfSights++;
                    }

                    wasEnemyVisibleLastFrame = isCurrentlyVisible;
                }
            }
        }

        public void PublishAndRelease()
        {
            // TODO update last time with focusing on enemy false
            EntityGameMetricsGameEvent.Instance.Raise(
                new GameMetrics
                {
                    entityId = entity.GetID(),
                    timeBetweenSights = totalTimeBetweenSights,
                    timeInFights = totalTimeInFight,
                    timeToSurrender = totalTimeToSurrender,
                    timeToEngage = totalTimeToEngage,
                    numberOfRetreats = numberOfRetreats,
                    numberOfFights = numberOfFights,
                    numberOfSights = numberOfSights
                }
            );

            SpawnInfoGameEvent.Instance.RemoveListener(RespawnEvent);
            FocusingOnEnemyGameEvent.Instance.RemoveListener(FocusingEvent);
        }

        private void RespawnEvent(SpawnInfo info)
        {
            if (info.entityId != entity.GetID()) return;
            lastRespawnTime = Time.time;
        }

        private void FocusingEvent(FocusOnEnemyInfo info)
        {
            if (info.entityID != entity.GetID()) return;
            isFocusingOnEnemy = info.isFocusing;
        }
    }
}