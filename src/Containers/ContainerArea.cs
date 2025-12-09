using System;
using System.Collections.Generic;
using System.Linq;
using DV.Logic.Job;
using UnityEngine;
using Object = UnityEngine.Object;

namespace better_loading;

public class ContainerArea: MonoBehaviour
{
	public record struct Slot(
		int row, int column, int layer
	);
	
	private const int MAX_COLUMNS = 2;
	private const int MAX_LAYERS = 2; //vertical
	
	private readonly Dictionary<Slot, ShippingContainer> containers = new();

	private void Start()
	{
		Utilities.CreateDebugCube(transform, nameof(ContainerArea));
	}

	public void SpawnContainersForLoading(WarehouseTask task)
	{
		Main.Log($"[{nameof(ContainerArea)}.{nameof(SpawnContainersForLoading)}] Spawning {task.cars.Count} containers of {task.cargoType} for job {task.Job.ID}");
		
		var slots = GetAvailableSlots(task.cars.Count, task.Job.ID);
		for (var carIndex = 0; carIndex < task.cars.Count; carIndex++)
		{
			var slot = slots[carIndex];
			var containerPosition = GetSlotPosition(slot);
			containers.Add(slot, new ShippingContainer(task.cars[carIndex], task, containerPosition, transform.rotation, transform)); //todo the containers are sometimes rotated 180 degrees (and they aren't symmetrical)
		}
	}

	public KeyValuePair<Slot, ShippingContainer> SpawnContainerForUnloading(WarehouseTask task, Car car)
	{
		Main.Log($"[{nameof(ContainerArea)}.{nameof(SpawnContainerForUnloading)}] Spawning 1 container of {task.cargoType} for job {task.Job.ID}");
		
		var slot = GetAvailableSlots(1, task.Job.ID)[0];
		// container position is set in ContainerMachine.LoadingUnloading
		var container = new ShippingContainer(car, task, Vector3.zero, transform.rotation, transform); //todo the containers are sometimes rotated 180 degrees (and they aren't symmetrical)
		container.containerObject.SetActive(false);
		
		containers.Add(slot, container);
		return new KeyValuePair<Slot, ShippingContainer>(slot, container);
	}

	// returns the position of the slot in world space
	public Vector3 GetSlotPosition(Slot slot)
	{
		var containerDimensions = new Vector3(12.19f, 2.59f, 2.44f); //todo use the real dimensions of the cargo prefab
		
		var localPosition = new Vector3(
			slot.row * containerDimensions.x,
			slot.layer * containerDimensions.y, 
			slot.column * containerDimensions.z
		);
		
		return transform.TransformPoint(localPosition);
	}

	private Slot[] GetAvailableSlots(int slotCount, string jobID)
	{
		var nextAvailableSlots = new Slot[slotCount];
		var slotIndex = 0;

		int row = 0;
		while (true)
		{
			for (int column = 0; column < MAX_COLUMNS; column++)
			{
				// Only stack containers of the same job 
				if (SlotIsTaken(row, column, 0 , out var existingContainer) &&
				    existingContainer.task.Job.ID != jobID)
				{
					continue;
				}

				for (int layer = 0; layer < MAX_LAYERS; layer++)
				{
					if(SlotIsTaken(row, column, layer, out _)) { continue; }
					
					nextAvailableSlots[slotIndex] = new Slot(row, column, layer);
					slotIndex++;
					
					if (slotIndex == nextAvailableSlots.Length) { return nextAvailableSlots; }
				}
			}

			row++;
		}
	}

	private bool SlotIsTaken(int row, int column, int layer, out ShippingContainer existingContainer)
	{
		return containers.TryGetValue(new Slot(row, column, layer), out existingContainer);
	}

	public KeyValuePair<Slot, ShippingContainer> GetSlotContainerPair(Car car)
	{
		try
		{
			return containers.First(pair => pair.Value.car.ID == car.ID);
		}
		catch (InvalidOperationException)
		{
			Main.Debug($"[{nameof(GetSlotContainerPair)}] not found. IDs:");
			foreach (var pair in containers)
			{
				Main.Debug(pair.Value.car.ID);
			}
			throw;
		}
	}

	public void Destroy(KeyValuePair<Slot, ShippingContainer> pair)
	{
		Object.Destroy(pair.Value.containerObject);
		containers.Remove(pair.Key);
	}
	
	public void PlaceInArea(Transform toPlace)
	{
		toPlace.SetParent(transform, true);
	}
}