﻿using UnityEngine;
using FYFY;
using FYFY_plugins.CollisionManager;

public class GameControllerSystem : FSystem {

	Family controllers = FamilyManager.getFamily (new AllOfComponents(typeof(GameController)));

	Family collisions = FamilyManager.getFamily(new AllOfComponents(typeof(Rigidbody), typeof(InCollision3D)));
	Family explosives = FamilyManager.getFamily(new AnyOfTags("Explosive"), new AllOfComponents(typeof(Rigidbody)));


	int currentCheckPointIndex = 0;

	public GameControllerSystem(){
		foreach (GameObject go in controllers){
			GameController con = go.GetComponent<GameController> ();
			con.speedQueue = new LimitedQueue<float>(con.memory);
			con.accelerationQueue = new LimitedQueue<float>(con.memory);
			con.groundSpeedQueue = new LimitedQueue<float>(con.memory);
			con.altitudeQueue = new LimitedQueue<float>(con.memory);
			con.dragQueue = new LimitedQueue<float>(con.memory);
		}


	}

	protected override void onPause(int currentFrame) {
	}
		
	protected override void onResume(int currentFrame){
	}
		
	protected override void onProcess(int familiesUpdateCount) {

		foreach (GameObject go in controllers){
			GameController con = go.GetComponent<GameController> ();
			GameObject target = con.target;

			updateCurrentCheckPoint (con);
			updateSpeedAndAcceleration(con);
			updateGroundSpeed (con);
			updateDrag (con);
			updateOrientations (con);
			updateAltitude (con);
			CheckGFailure (con);
			CheckDragFailure (con);
			CheckDistanceFailure (con);
			CheckCollision (con);
		}

		foreach (GameObject go in explosives) {
			Rigidbody rb = go.GetComponent<Rigidbody> ();
			rb.useGravity = false;
			rb.AddExplosionForce (rb.mass*rb.mass, rb.position, 1);
			GameObject explo = GameObjectManager.instantiatePrefab ("FireExplosion");
			explo.transform.position = rb.position;
			go.tag = "Untagged";
		}
	}

	protected void updateCurrentCheckPoint(GameController con){
		int bestIndex = currentCheckPointIndex;

		float bestDistance = Vector3.Distance (con.generator.checkPoints [bestIndex].position, con.target.transform.position);
		int temp = Mathf.Min (bestIndex + 1, con.generator.checkPoints.Count - 1);
		float tempDistance = Vector3.Distance (con.generator.checkPoints [temp].position, con.target.transform.position);


		while (temp < con.generator.checkPoints.Count - 1 && tempDistance < bestDistance) {
			bestIndex = temp;
			bestDistance = tempDistance;
			temp++;
			tempDistance = Vector3.Distance (con.generator.checkPoints [temp].position, con.target.transform.position);
		}

		temp = (int)Mathf.Max (bestIndex - 1, 0);
		tempDistance = Vector3.Distance (con.generator.checkPoints [temp].position, con.target.transform.position);
		while (temp > 0 && tempDistance < bestDistance) {
			bestIndex = temp;
			bestDistance = tempDistance;
			temp++;
			tempDistance = Vector3.Distance (con.generator.checkPoints [temp].position, con.target.transform.position);
		}
		currentCheckPointIndex = bestIndex;

	}

	protected void updateSpeedAndAcceleration(GameController con){
		Rigidbody rb = con.target.GetComponent<Rigidbody> ();
		con.speedQueue.Enqueue (rb.velocity.magnitude);
		con.accelerationQueue.Enqueue ((rb.velocity.magnitude - con.speed) * 9.81f * 10);
		con.speed = getQueueMean (con.speedQueue);
		con.acceleration = getQueueMean (con.accelerationQueue);

	}

	protected void updateGroundSpeed(GameController con){
		Rigidbody rb = con.target.GetComponent<Rigidbody> ();
		con.groundSpeedQueue.Enqueue (0f);
		con.groundSpeed = getQueueMean (con.groundSpeedQueue);
	}

	protected void updateG(GameController con ){
		Rigidbody rb = con.target.GetComponent<Rigidbody> ();

		con.accelerationQueue.Enqueue (rb.velocity.magnitude);
		con.speed = getQueueMean (con.speedQueue);
	}

	protected void updateDrag(GameController con){
		Rigidbody rb = con.target.GetComponent<Rigidbody> ();
		float drag = .5f * rb.drag * Mathf.Pow (rb.velocity.magnitude, 2f) * (1f + Vector3.Angle (rb.velocity, rb.transform.up) / 10f);
		con.dragQueue.Enqueue (drag);
		con.drag = getQueueMean (con.dragQueue);
	}

	protected void updateOrientations(GameController con){
		Rigidbody rb = con.target.GetComponent<Rigidbody> ();
		con.orientation = con.target.transform.rotation.x * 180f - con.generator.checkPoints [currentCheckPointIndex].orientation;
		Vector3 dirGravity = - con.target.transform.position.normalized;
		Vector3 dirShuttle = con.target.transform.up.normalized;
		con.earthOrientation = Vector3.Angle (dirShuttle, dirGravity) * Mathf.Sign (Vector3.Cross (dirGravity, dirShuttle).x) + 180f;
			
	}

	protected void updateAltitude(GameController con){
		con.altitude = PhysicsConstants.GetAltitude (con.target.transform.position);
	}

	protected void CheckGFailure(GameController con){
		if (con.acceleration > con.GFailThreshold) {
			Explode (con.target);
		}
	}

	protected void CheckDragFailure(GameController con){
		if (con.drag > con.DragFailThreshold)
			Explode (con.target);
	}

	protected void CheckDistanceFailure(GameController con){
		if (Vector3.Distance (con.target.transform.position, con.generator.checkPoints [currentCheckPointIndex].position) > con.maxTrajectoryDistance) {
			Explode (con.target);
		}
	}

	protected void CheckCollision(GameController con){
		foreach (GameObject go in collisions) {

			Rigidbody rb = go.GetComponent<Rigidbody> ();
			Debug.Log (rb.velocity.magnitude);
			if (rb.velocity.magnitude > con.MaxCollisionSpeed) {
				Explode (go);
				Debug.Log ("Explose");
			}
		}
	}

	protected void Explode(GameObject go)
	{
		foreach (Largable largable in go.GetComponentsInChildren<Largable> ()) {
			largable.toDrop = true;
			largable.gameObject.tag = "Explosive";
		}
	}

	public float getQueueMean (LimitedQueue<float> q)
	{
		if (q.Count == 0)
			return 0;

		float res = 0f;
		foreach (float o in q) {
			res += (float)o;
		}

		return res /= q.Count;
	}

}