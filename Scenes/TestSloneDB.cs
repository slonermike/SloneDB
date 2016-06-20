using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestSloneDB : MonoBehaviour {

	// Use this for initialization
	IEnumerator Main () {
		if (!SloneDB.inst.HasTable ("TestTable")) {
			CreateTable ();
		}

		// Insert rows with numbers 1 to 5.
		yield return InsertRows (1, 5);
		ShowNumRows ();
		PrintRows ();

		yield return new WaitForSeconds (1.0f);

		// Delete rows 2 to 4.
		DeleteRows (2, 4);
		ShowNumRows ();
		PrintRows ();

		yield return new WaitForSeconds (1.0f);

		// Update the dates on rows 1 to 5.
		UpdateRows (1, 5);
		PrintRows ();

		yield return new WaitForSeconds (1.0f);

		// Delete Rows 1 to 5.
		DeleteRows (1, 5);
		ShowNumRows ();
	}

	void CreateTable()
	{
		List<DBFieldInfo> fields = new List<DBFieldInfo> ();
		fields.Add(new DBFieldInfo("stringField", DBFieldType.VARCHAR, 16));
		fields.Add (new DBFieldInfo ("intField", DBFieldType.INT));
		fields.Add (new DBFieldInfo ("date", DBFieldType.INT));
		SloneDB.inst.CreateTable ("TestTable", fields);
	}

	IEnumerator InsertRows(int minVal, int maxVal)
	{
		Debug.Log ("Adding rows " + minVal + " to " + maxVal);
		for (int i = minVal; i <= maxVal; i++) {
			DBRow row = new DBRow ();
			row.SetValueDate ("date", System.DateTime.Now);
			row.SetValueInt ("intField", i);
			row.SetValueString ("stringField", "String #" + i);
			SloneDB.inst.AddEntry ("TestTable", row);
			yield return new WaitForSeconds (1.0f);
		}
	}

	void DeleteRows(int minVal, int maxVal)
	{
		Debug.Log ("Deleting rows " + minVal + " to " + maxVal);
		DBQuery delQuery = new DBQuery ("TestTable");
		delQuery.AddCondition("intField", minVal, DBConditionType.GREATER_THAN_EQUAL);
		delQuery.AddCondition("intField", maxVal, DBConditionType.LESS_THAN_EQUAL);
		SloneDB.inst.ExecuteDelete(delQuery);
	}

	void UpdateRows(int minVal, int maxVal)
	{
		Debug.Log ("Updating rows " + minVal + " to " + maxVal);
		DBQuery query = new DBQuery ("TestTable");
		query.AddCondition("intField", minVal, DBConditionType.GREATER_THAN_EQUAL);
		query.AddCondition("intField", maxVal, DBConditionType.LESS_THAN_EQUAL);
		DBRow newDate = new DBRow ();
		newDate.SetValueDate ("date", System.DateTime.Now);
		SloneDB.inst.ExecuteUpdate (query, newDate);
	}

	void ShowNumRows()
	{
		DBQuery query = new DBQuery ("TestTable");
		DBResult result = SloneDB.inst.ExecuteQuery(query);
		Debug.Log ("Num results found: " + result.rows.Count);
	}

	void PrintRows()
	{
		DBQuery query = new DBQuery ("TestTable");
		DBResult result = SloneDB.inst.ExecuteQuery(query);
		foreach (DBRow row in result.rows) {
			int rowIndex = (int)row.GetValueInt ("intField");
			System.DateTime date = row.GetValueDate ("date");
			string str = row.GetValueString ("stringField");
			Debug.Log (date.ToLongTimeString() + "\t" + rowIndex + "\t" + str);
		}
	}
}
