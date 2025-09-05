using System.Collections;
using UnityEngine;

namespace better_loading;

public class Crane: MonoBehaviour
{
	private const float CRANE_SPEED = 2f;
	private const float CLOSE_ENOUGH = 0.01f;
	private const float HORIZONTAL_MOVE_ALTITUDE_LOCAL = 4f;
	
	//the bottom part of the crane, that drives on rails 
	private Transform based; //can't name this 'base'
	//the part of the crane that has the cab and connects the base with the grabber
	private Transform cab;
	//the part of the crane that moves up and down and grabs the containers
	private Transform grabber;
	
	public CraneInfo info;

	public void Initialize(CraneInfo info_)
	{
		info = info_;
		
		gameObject.GetComponent<Animator>().enabled = false;
		based = transform.GetChildByName("Portal_Crane_Base");
		cab = based.GetChildByName("Portal_Crane_Cab");
		
		//todo
		grabber = Utilities.CreateDebugCube(cab, Vector3.zero, cab.rotation, nameof(grabber)).transform;
		grabber.localPosition = new Vector3(0, 6.3f, -5.1f);
		grabber.localScale = new Vector3(12, 0.01f, 2.5f);
		grabber.GetComponent<BoxCollider>().enabled = false;
	}

	public IEnumerator MoveTo(Vector3 targetWorldPosition)
	{
		Vector3 positionDelta;
		
		do
		{
			yield return null;
			var stepSize = Time.deltaTime * CRANE_SPEED;
			positionDelta = targetWorldPosition - grabber.position;
			var localPositionDelta = grabber.InverseTransformDirection(positionDelta);
			
			grabber.Translate(localPositionDelta.OnlyY().ClampMagnitude(stepSize), Space.Self);
			cab.Translate(localPositionDelta.OnlyZ().ClampMagnitude(stepSize), Space.Self);
			based.Translate(localPositionDelta.OnlyX().ClampMagnitude(stepSize), Space.Self);
			
		} while (positionDelta.magnitude > CLOSE_ENOUGH); //todo sqrMagnitude
	}
	
	//todo constraints

	public void Grab(Transform toGrab)
	{
		toGrab.SetParent(grabber, true);
	}
}