﻿using UnityEngine;
using FYFY;

public class DropSystem : FSystem {
	// Récupération des familles sur lesquelles agit le système
	private Family largables = FamilyManager.getFamily(new AllOfComponents(typeof(Largable), typeof(Masse)));

	// Use this to update member variables when system pause. 
	// Advice: avoid to update your families inside this function.
	protected override void onPause(int currentFrame) {
	}

	// Use this to update member variables when system resume.
	// Advice: avoid to update your families inside this function.
	protected override void onResume(int currentFrame){
	}

	// Use to process your families.
	protected override void onProcess(int familiesUpdateCount) {
		foreach (GameObject go in largables)
		{
			if (go.GetComponent<Largable> ().toDrop)
			{
				Rigidbody rb = go.GetComponent<Rigidbody>();
				if (rb != null) {
					/*float parentMass = go.transform.parent.GetComponent<Rigidbody> ().mass;
					float goMass = rb.mass;
					float factor = parentMass / goMass;*/
					rb.velocity = go.transform.parent.GetComponentInParent<Rigidbody> ().velocity;

					// Ajout d'une force sur le côté pour éloigner l'objet
					rb.AddForce (go.transform.localPosition.normalized * 1e3f);
					rb.AddTorque (new Vector3 (0, 0, -go.transform.localPosition.x));

					// Séparation en bougeant de la hiérarchie
					go.transform.parent = null;

					// Suppression composant largable car déjà largué
					GameObjectManager.removeComponent<Largable> (go);
				} else {

					Propulseur[] propfils = go.GetComponentsInChildren<Propulseur> ();
					foreach (Propulseur p in propfils)
					{
						prepareToJettison (go, p.gameObject);
					}


					// Désactivation du Tank
					if (go.CompareTag ("Tank")) {
						Debug.Log ("This is it");
						go.GetComponent<Flames> ().isOn = false;
						GameObjectManager.removeComponent<Flames> (go);
						GameObjectManager.removeComponent<Propulseur> (go);
					}
				}
			}
		}
	}

	/**
	 * Rajoute le RigidBody au parent et met les targets des propulseurs à jour
	 */
	void prepareToJettison(GameObject father, GameObject dropped)
	{
		// Création d'un RigidBody à lui ajouter
		if (dropped.GetComponent<Rigidbody> () == null && dropped == father) {
			GameObjectManager.addComponent<Rigidbody> (dropped);
		}

		// Mise à jour des target des components
		dropped.GetComponent<Masse> ().target = father;
		Propulseur prop = dropped.GetComponent<Propulseur> ();
		if (prop != null) {
			prop.target = father;
		}
	}
}