using System.Collections.Generic;
using DV.Logic.Job;

namespace better_loading;

public class ContainerTransferQueueEntry
{
	// train car the container will be moved to/from
	public readonly Car car;
	// the slot in the ContainerArea which the container will be moved to/from. and the container itself
	public readonly KeyValuePair<ContainerArea.Slot, ShippingContainer> slotContainer;
	// the task to which the container belongs
	public readonly WarehouseTask task;

	public ContainerTransferQueueEntry(Car car, KeyValuePair<ContainerArea.Slot, ShippingContainer> slotContainer, WarehouseTask task)
	{
		this.car = car;
		this.slotContainer = slotContainer;
		this.task = task;
	}
}