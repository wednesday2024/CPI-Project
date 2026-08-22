using ClubPenguin.Core.StaticGameData;
using ClubPenguin.Net;
using ClubPenguin.Net.Domain;
using ClubPenguin.Task;
using Disney.Kelowna.Common;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClubPenguin.DailyChallenge
{
    public class DailyChallengeService
    {
        public int NumberOfUpdates;

        private EventDispatcher dispatcher;

        private TaskService taskService;

        private List<ScriptableObject> loadedDailies = new List<ScriptableObject>();

        private DatedManifestMap datedManifestMap;

        private TaskProgressList serverTaskProgress;

        private Dictionary<string, int> savedTaskCounters = new Dictionary<string, int>();

        public bool HasUpdates
        {
            get
            {
                return NumberOfUpdates > 0;
            }
        }

        public bool IsDailiesLoaded
        {
            get
            {
                return loadedDailies.Count > 0;
            }
        }

        public DailyChallengeService(DatedManifestMap datedManifestMap)
        {
            dispatcher = Service.Get<EventDispatcher>();
            dispatcher.AddListener<TaskNetworkServiceEvents.TaskCounterChanged>(onTaskCounterChanged);
            dispatcher.AddListener<TaskNetworkServiceEvents.DailyTaskProgressRecieved>(onTaskProgressRecieved);
            dispatcher.AddListener<TaskServiceEvents.TasksLoaded>(onTasksLoaded, EventDispatcher.Priority.FIRST);
            dispatcher.AddListener<TaskEvents.TaskCompleted>(onTaskComplete);
            dispatcher.AddListener<TaskEvents.TaskUpdated>(onTaskUpdated);
            taskService = Service.Get<TaskService>();
            serverTaskProgress = new TaskProgressList();
            this.datedManifestMap = datedManifestMap;
        }

        public static string GetDateManifestMapPath()
        {
            return UriUtil.Combine(StaticGameDataUtils.GetPathFromResources(StaticGameDataUtils.GetDefinitionPath(typeof(DailyChallengeScheduleDefinition))), "Schedule");
        }

        public void ClearLoadedDailies()
        {
            loadedDailies.Clear();
        }

        public void ReloadChallenges(DateTime day)
        {
            CoroutineRunner.Start(reloadChallenges(day), this, "DailyChallengesService");
        }

        private IEnumerator reloadChallenges(DateTime day)
        {
            loadedDailies.Clear();
            savedTaskCounters.Clear();
            UnityEngine.Object manifest;
            DailyChallengeScheduleDefinition dailies = null;
            if (day != default(DateTime) && datedManifestMap.Map.TryGetValue(day.Date, out manifest))
            {
                dailies = manifest as DailyChallengeScheduleDefinition;
            }
            if (dailies != null)
            {
                yield return loadSchedule(dailies);
            }
            else
            {
                Log.LogError(this, "No Daily Tasks scheduled for today");
            }
            setupTasks();
        }

        private IEnumerator loadSchedule(DailyChallengeScheduleDefinition schedule)
        {
            for (int i = 0; i < schedule.Assets.Length; i++)
            {
                DailyChallengeDefinitionContentKey daily = schedule.Assets[i];
                yield return loadScheduleDaily(daily.Key);
            }
        }

        private IEnumerator loadScheduleDaily(string path)
        {
            AssetRequest<ScriptableObject> assetRequest = null;
            try
            {
                assetRequest = Content.LoadAsync<ScriptableObject>(path);
            }
            catch (ContentManifestException)
            {
            }
            yield return assetRequest;
            loadedDailies.Add(assetRequest.Asset);
        }

        private const string CLOTHING_CATALOG_TASK_NAME = "ClothingCatalogSubmission";

        private const int DAILY_CHALLENGE_TOTAL = 6;

        private bool isExcludedGroup(TaskDefinition.TaskGroup group)
        {
            return group == TaskDefinition.TaskGroup.Teamwork || group == TaskDefinition.TaskGroup.Community;
        }

        private void setupTasks()
        {
            int targetCount = DAILY_CHALLENGE_TOTAL;
            List<string> finalTaskNames = new List<string>();
            HashSet<string> usedTaskNames = new HashSet<string>();

            for (int i = 0; i < loadedDailies.Count; i++)
            {
                if (finalTaskNames.Count >= targetCount)
                {
                    break;
                }

                DailyChallengeDefinition dailyChallengeDefinition = (DailyChallengeDefinition)loadedDailies[i];
                string taskName = dailyChallengeDefinition.TaskName();

                if (usedTaskNames.Contains(taskName))
                {
                    continue;
                }

                if (taskName == CLOTHING_CATALOG_TASK_NAME)
                {
                    finalTaskNames.Add(taskName);
                    usedTaskNames.Add(taskName);
                    continue;
                }

                TaskDefinition definition;
                if (!taskService.TryGetDefinition(taskName, out definition) || isExcludedGroup(definition.Group))
                {
                    continue;
                }

                finalTaskNames.Add(taskName);
                usedTaskNames.Add(taskName);
            }

            if (finalTaskNames.Count < targetCount)
            {
                int shortfall = targetCount - finalTaskNames.Count;
                List<string> replacements = taskService.GetTaskNamesByGroup(TaskDefinition.TaskGroup.Individual, usedTaskNames, shortfall);
                finalTaskNames.AddRange(replacements);
            }

            string[] array = finalTaskNames.ToArray();
            NumberOfUpdates = array.Length;
            taskService.LoadTasks(array);
        }

        private bool onTasksLoaded(TaskServiceEvents.TasksLoaded evt)
        {
            for (int i = 0; i < serverTaskProgress.Count; i++)
            {
                if (evt.Tasks.ContainsKey(serverTaskProgress[i].taskId))
                {
                    applyStoredProgress(serverTaskProgress[i].taskId, serverTaskProgress[i].counter, serverTaskProgress[i].claimed);
                }
            }
            return false;
        }

        private bool onTaskComplete(TaskEvents.TaskCompleted evt)
        {
            NumberOfUpdates++;
            return false;
        }

        public void RecordShown()
        {
            NumberOfUpdates = 0;
        }

        public void ClaimTaskReward(ClubPenguin.Task.Task task)
        {
            Service.Get<INetworkServicesManager>().TaskService.ClaimReward(task.Id);
            taskService.SetRewardClaimed(task);
        }

        private bool onTaskCounterChanged(TaskNetworkServiceEvents.TaskCounterChanged evt)
        {
            applyStoredProgress(evt.TaskId, evt.Counter, null);
            return false;
        }

        private bool onTaskProgressRecieved(TaskNetworkServiceEvents.DailyTaskProgressRecieved evt)
        {
            serverTaskProgress = evt.DailyTaskProgress;
            for (int i = 0; i < evt.DailyTaskProgress.Count; i++)
            {
                TaskProgress taskProgress = evt.DailyTaskProgress[i];
                applyStoredProgress(taskProgress.taskId, taskProgress.counter, taskProgress.claimed);
            }
            return false;
        }

        private void applyStoredProgress(string taskId, int counter, bool? claimed)
        {
            savedTaskCounters[taskId] = counter;
            taskService.SetTaskProgress(taskId, counter, claimed);
        }

        private bool onTaskUpdated(TaskEvents.TaskUpdated evt)
        {
            ClubPenguin.Task.Task task = evt.Task;
            int savedCounter;
            if (savedTaskCounters.TryGetValue(task.Id, out savedCounter) && savedCounter == task.Counter)
            {
                return false;
            }
            savedTaskCounters[task.Id] = task.Counter;
            Service.Get<INetworkServicesManager>().TaskService.SetProgress(task.Id, task.Counter);
            return false;
        }
    }
}