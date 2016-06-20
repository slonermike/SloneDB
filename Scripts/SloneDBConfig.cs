using UnityEngine;
using System.Collections;

public class SloneDBConfig : MonoBehaviour {

	[Tooltip("Specify the name of the file where the database should be saved.")]
	public string databaseFilename = "GameData.db";

	[Tooltip("Set to true to enable debug setting where all raw SQL queries are output to console.")]
	public bool queryDebug = false;
}
