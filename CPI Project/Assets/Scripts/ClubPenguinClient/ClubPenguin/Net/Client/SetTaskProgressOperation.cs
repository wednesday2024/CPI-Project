using ClubPenguin.Net.Client.Mappers;
using ClubPenguin.Net.Domain;
using ClubPenguin.Net.Offline;
using Disney.Manimal.Common.Util;
using hg.ApiWebKit.core.attributes;
using hg.ApiWebKit.mappers;
using System;

namespace ClubPenguin.Net.Client
{
	[HttpPath("cp-api-base-uri", "/task/v1")]
	[RequestQueue("Task")]
	[HttpContentType("application/json")]
	[HttpAccept("application/json")]
	[HttpPOST]
	[HttpBasicAuthorization("cp-api-username", "cp-api-password")]
	public class SetTaskProgressOperation : CPAPIHttpOperation
	{
		[HttpRequestJsonBody]
		public SignedResponse<TaskProgress> RequestBody;

		public SetTaskProgressOperation(SignedResponse<TaskProgress> task)
		{
			RequestBody = task;
		}

		protected override void PerformOfflineAction(OfflineDatabase offlineDatabase, IOfflineDefinitionLoader offlineDefinitions)
		{
			if (RequestBody != null)
			{
				SaveProgress(offlineDatabase, RequestBody.Data);
			}
		}

		public static long GetCurrentDay()
		{
			return DateTime.UtcNow.Date.GetTimeInMilliseconds();
		}

		// Progress of an earlier day is dropped rather than returned: the challenge
		// list is picked per day, so old counters belong to tasks that are gone.
		public static DailyTaskProgress ReadCurrentDay(OfflineDatabase offlineDatabase)
		{
			DailyTaskProgress dailyTaskProgress = offlineDatabase.Read<DailyTaskProgress>();
			long currentDay = GetCurrentDay();
			if (dailyTaskProgress.Tasks == null || dailyTaskProgress.Day != currentDay)
			{
				dailyTaskProgress.Init();
				dailyTaskProgress.Day = currentDay;
			}
			return dailyTaskProgress;
		}

		public static TaskProgressList GetProgressList(OfflineDatabase offlineDatabase)
		{
			TaskProgressList taskProgressList = new TaskProgressList();
			taskProgressList.AddRange(ReadCurrentDay(offlineDatabase).Tasks);
			return taskProgressList;
		}

		public static void SaveProgress(OfflineDatabase offlineDatabase, TaskProgress progress)
		{
			if (string.IsNullOrEmpty(progress.taskId))
			{
				return;
			}
			DailyTaskProgress dailyTaskProgress = ReadCurrentDay(offlineDatabase);
			progress.day = dailyTaskProgress.Day;
			for (int i = 0; i < dailyTaskProgress.Tasks.Count; i++)
			{
				if (dailyTaskProgress.Tasks[i].taskId == progress.taskId)
				{
					// A claimed reward stays claimed: a later counter update must not
					// reopen it, or the same task could be paid out twice.
					progress.claimed |= dailyTaskProgress.Tasks[i].claimed;
					dailyTaskProgress.Tasks[i] = progress;
					offlineDatabase.Write(dailyTaskProgress);
					return;
				}
			}
			dailyTaskProgress.Tasks.Add(progress);
			offlineDatabase.Write(dailyTaskProgress);
		}
	}
}
