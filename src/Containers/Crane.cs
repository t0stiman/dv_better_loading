using System.Collections;
using UnityEngine;

namespace better_loading;

public class Crane: MonoBehaviour
{
	private const float CLOSE_ENOUGH = 0.01f;
	private readonly float CLOSE_ENOUGH_SQUARED = Mathf.Pow(CLOSE_ENOUGH, 2);
	private const float HORIZONTAL_MOVE_ALTITUDE_LOCAL = 6.3f;
	
	//the bottom part of the crane, that drives on rails 
	private Transform based; //can't call this variable 'base'
	//the part of the crane that has the cab and connects the base with the grabber
	private Transform cab;
	//the part of the crane that moves up and down and grabs the containers
	private Transform grabber;

	public void Initialize()
	{
		gameObject.GetComponent<Animator>().enabled = false;
		based = transform.GetChildByName("Portal_Crane_Base");
		cab = based.GetChildByName("Portal_Crane_Cab");
		
		grabber = Utilities.CreateDebugCube(cab, Vector3.zero, cab.rotation, nameof(grabber)).transform;
		grabber.localPosition = new Vector3(0, 6.3f, -5.1f);
		grabber.localScale = new Vector3(12, 0.01f, 2.5f);
		grabber.GetComponent<BoxCollider>().enabled = false;
	}
	
	// move the crane to targetWorldPosition
	public IEnumerator MoveTo(Vector3 targetWorldPosition)
	{
		if(Vector3.Distance(targetWorldPosition, grabber.position) <= CLOSE_ENOUGH) yield break;
		
		//up
		Main.Debug($"{nameof(Crane)}.{nameof(MoveTo)} up");
		yield return MoveToHorizontalMoveAltitude();

		//sideways
		{
			Main.Debug($"{nameof(Crane)}.{nameof(MoveTo)} sideways");
			Vector3 positionDelta;
			do
			{
				yield return null;

				positionDelta = targetWorldPosition - grabber.position;
				var localPositionDelta = grabber.InverseTransformDirection(positionDelta);
				var maxStepSize = Time.deltaTime * Main.MySettings.ContainerLoadSpeed;
				
				cab.Translate(localPositionDelta.OnlyZ().ClampMagnitude(maxStepSize), Space.Self);
				based.Translate(localPositionDelta.OnlyX().ClampMagnitude(maxStepSize), Space.Self);

			} while (positionDelta.OnlyXZ().sqrMagnitude > CLOSE_ENOUGH_SQUARED);
		}
		
		//down
		{
			Main.Debug($"{nameof(Crane)}.{nameof(MoveTo)} down");
			Vector3 positionDelta;
			do
			{
				yield return null;

				positionDelta = targetWorldPosition - grabber.position;
				var localPositionDelta = grabber.InverseTransformDirection(positionDelta);
				var maxStepSize = Time.deltaTime * Main.MySettings.ContainerLoadSpeed;

				grabber.Translate(localPositionDelta.OnlyY().ClampMagnitude(maxStepSize), Space.Self);
				cab.Translate(localPositionDelta.OnlyZ().ClampMagnitude(maxStepSize), Space.Self);
				based.Translate(localPositionDelta.OnlyX().ClampMagnitude(maxStepSize), Space.Self);

			} while (positionDelta.sqrMagnitude > CLOSE_ENOUGH_SQUARED);
		}
		
		Main.Debug($"{nameof(Crane)}.{nameof(MoveTo)} done");
	}

	public IEnumerator MoveToHorizontalMoveAltitude()
	{
		float localYDelta;
		do
		{
			yield return null;
			
			localYDelta = HORIZONTAL_MOVE_ALTITUDE_LOCAL - grabber.localPosition.y;
			var maxStepSize = Time.deltaTime * Main.MySettings.ContainerLoadSpeed;
			var step = new Vector3(0, localYDelta < maxStepSize ? localYDelta : maxStepSize, 0);
			
			grabber.Translate(step, Space.Self);

		} while (Mathf.Abs(localYDelta) > CLOSE_ENOUGH);
	}

	public void Grab(Transform toGrab)
	{
		toGrab.SetParent(grabber, true);
	}
}