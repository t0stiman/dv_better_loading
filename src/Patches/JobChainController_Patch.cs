using System.Collections.Generic;
using DV.Logic.Job;
using DV.ThingTypes;
using HarmonyLib;

namespace better_loading.Patches;

/// <summary>
/// When a new shunting-loading job is generated, with shipping containers, spawn those containers next to the loading crane.
/// </summary>
[HarmonyPatch(typeof(JobChainController))]
[HarmonyPatch(nameof(JobChainController.OnJobGenerated))]
public class JobChainController_OnJobGenerated_Patch
{
	private static void Postfix(Job generatedJob)
	{
		if (generatedJob.jobType != JobType.ShuntingLoad) return;
		
		Main.Debug($"{nameof(JobChainController_OnJobGenerated_Patch)} job {generatedJob.ID}");
		
		var loadTasks = FindLoadingTasks(generatedJob);
		if (loadTasks.Count == 0)
		{
			Main.Error($"{nameof(JobChainController_OnJobGenerated_Patch)} Could not find any load task on job {generatedJob.ID}");
			return;
		}

		foreach (var loadTask in loadTasks)
		{
			if (!(
				    AdvancedMachine.TryGetAdvancedMachine(loadTask.warehouseMachine, out var advancedMachine) &&
				    advancedMachine.GetType() == typeof(ContainerMachine)
			    ))
			{ return; }

			if (!ContainerMachine.IsInShippingContainer(loadTask.cargoType)) { return; }
		
			var containerMachine = (ContainerMachine)advancedMachine;
			containerMachine.SpawnContainers(loadTask);
		}
	}
	
	/// <summary>
	/// the loading tasks are nested underneath a structure of parallel and sequential tasks, so we'll recursively search for them
	/// </summary>
	private static List<WarehouseTask> FindLoadingTasks(Job job)
	{
		var loadingTasks = new List<WarehouseTask>();
		
		foreach (var task in job.tasks)
		{
			if(!FindLoadingTasks(task, out var foundTasks)) continue;
			loadingTasks.AddRange(foundTasks);
		}
		
		Main.Debug($"{nameof(FindLoadingTasks)}: loading tasks: {loadingTasks.Count}");
		return loadingTasks;
	}
	
	/// <returns>true if at least 1 loading task is found</returns>
	private static bool FindLoadingTasks(Task parentTask, out List<WarehouseTask> loadingTasks)
	{
		loadingTasks = new List<WarehouseTask>();

		switch (parentTask.InstanceTaskType)
		{
			case TaskType.Warehouse:
			{
				var warehouseTask = (WarehouseTask)parentTask;
				if (warehouseTask.warehouseTaskType == WarehouseTaskType.Loading)
				{
					loadingTasks.Add(warehouseTask);
				}
				break;
			}

			case TaskType.Parallel:
			{
				var parallelTask = (ParallelTasks)parentTask;
				foreach (var childTask in parallelTask.tasks)
				{
					if (FindLoadingTasks(childTask, out var loadingTasks2))
					{
						loadingTasks.AddRange(loadingTasks2);
					}
				}
				break;
			}

			case TaskType.Sequential:
				var sequentialTask = (SequentialTasks)parentTask;
				foreach (var childTask in sequentialTask.tasks)
				{
					if(FindLoadingTasks(childTask, out var loadingTasks2))
					{
						loadingTasks.AddRange(loadingTasks2);
					}
				}
				break;
		}
		
		return loadingTasks.Count > 0;
	}
}