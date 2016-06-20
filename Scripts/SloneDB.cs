/************************************************************
 * 
 *                       Slone DB
 *                 2016 Slonersoft Games
 * 
 * Wrapper for game database.  Do all sqlite clals through this
 * wrapper if you can.  Extend functionality here, rather than
 * work around it elsewhere.
 *
 * IMPORTANT NOTE 1: For this to work in a built game, you have
 * to change the API compatibility level to ".Net 2.0" instead
 * of ".Net 2.0 Subset" and copy Mono.Data.dll and
 * Mono.Data.Sqlite.dll into Plugins.
 * 
 * IMPORTANT NOTE 2: You must find the appropriate sqlite3.dll
 * and sqlite3.def files for your platform (x86 and/or x64) and
 * place them in your plugins folder.  This will not work
 * without that.  In many cases you may need to get both, assign
 * the x64 version to be used by the editor, and assign the x86
 * version to be used by your Player.
 * 
 ************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Data;
using Mono.Data.Sqlite;

// Types of relational conditions that can be used to narrow down query results.
//
public enum DBConditionType {
	EQUAL,
	LESS_THAN,
	GREATER_THAN,
	LESS_THAN_EQUAL,
	GREATER_THAN_EQUAL
}

// Data types that can be assigned to table fields.
//
public enum DBFieldType {
	INT,
	TEXT,
	VARCHAR
};

// A single row from a database -- can read and add values to the row.
public class DBRow {
	public Dictionary<string, long> intValues = new Dictionary<string, long>();
	public Dictionary<string, string> strValues = new Dictionary<string, string>();

	// Returns true if the field is present.
	//
	// fieldname: name of the field to look for.
	//
	public bool HasField(string fieldname)
	{
		return intValues.ContainsKey (fieldname) || strValues.ContainsKey (fieldname);
	}

	// Set a value in the form of an integer.
	//
	// field: name of the field to set.
	// value: value of the field to set.
	//
	public void SetValueInt(string field, long value)
	{
		intValues [field] = value;
	}

	// Get an integer value from the row.
	//
	// field: name of the field to grab.
	//
	// Returns 0 if field is not present.
	//
	public long GetValueInt(string field)
	{
		if (intValues.ContainsKey (field)) {
			return intValues [field];
		} else {
			Debug.LogError ("GetValueInt couldn't find field: " + field);
			return 0;
		}
	}

	// Set a value in the form of a string.
	//
	// field: name of the field to set.
	// value: value of the field to set.
	//
	public void SetValueString(string field, string value)
	{
		strValues [field] = value;
	}

	// Get a string value from the row.
	//
	// field: name of the field to grab.
	//
	// Returns empty string if field is not present.
	//
	public string GetValueString(string field)
	{
		if (strValues.ContainsKey (field)) {
			return strValues [field];
		} else {
			Debug.LogError ("GetValueString couldn't find field: " + field);
			return "";
		}
	}

	// Set a value in the form of a DateTime.
	//
	// field: name of the field to set.
	// time: value of the field to set.
	//
	public void SetValueDate(string field, System.DateTime time)
	{
		intValues [field] = time.Ticks;
	}

	// Get a value in the form of a DateTime.
	//
	// field: name of the field to grab.
	//
	// Returns DateTime.Now if field is not present.
	//
	public System.DateTime GetValueDate(string field)
	{
		if (intValues.ContainsKey (field)) {
			return new System.DateTime (intValues [field]);
		} else {
			Debug.LogError ("GetValueDate couldn't find field: " + field);
			return System.DateTime.Now;
		}
	}

	// Remove a field value from the row.
	//
	// field: name of the field you wish to remove from the row.
	//
	public void RemoveField(string field)
	{
		if (intValues.ContainsKey (field)) {
			intValues.Remove (field);
		} else if (strValues.ContainsKey (field)) {
			strValues.Remove (field);
		}
	}

	public string GetUpdateValueString()
	{
		string s = "";
		foreach (KeyValuePair<string, long> kvp in intValues) {
			if (s.Length > 0) {
				s += ", ";
			}

			s += kvp.Key + "='" + kvp.Value + "'";
		}

		foreach (KeyValuePair<string, string> kvp in strValues) {
			if (s.Length > 0) {
				s += ", ";
			}

			s += kvp.Key + "='" + kvp.Value + "'";
		}

		return s;
	}
}

// Populates and maintains a database result from a string query.
//
// Results stored in DBResult.rows.
//
public class DBResult {
	public List<DBRow> rows = new List<DBRow>();

	// Create a DBResult from a raw SQL query.
	//
	// connection: the connection to the database.
	// query: the SQL query used to populate the result.
	//
	public DBResult(SqliteConnection connection, string query)
	{
		SloneDB.QueryDebug(query);

		SqliteCommand command = new SqliteCommand(query, connection);
		SqliteDataReader reader = command.ExecuteReader();

		while(reader.Read()) {
			DBRow row = new DBRow();
			for (int i = 0; i < reader.FieldCount; i++) {
				string dataType = reader.GetDataTypeName(i).ToString();
				string fieldName = reader.GetName(i).ToString();

				if (dataType == "INT") {
					row.SetValueInt (fieldName, reader.GetInt64 (i));
				} else if (dataType == "TEXT" || dataType.StartsWith ("VARCHAR")) {
					row.SetValueString (fieldName, reader.GetString (i));

				// SQLite default type is "Numeric".
				} else {
					row.SetValueInt (fieldName, reader.GetInt64 (i));
				}
			}
			rows.Add(row);
		}

		reader.Close();
	}
}

// Used to construct queries without having to write any SQL.
//
public class DBQuery
{
	// Used within DBQuery to create the conditional part of a SQL query string.
	//
	private class DBCondition {
		
		string field;
		DBConditionType type;
		object value;

		// field: The field on which the condition is being tested.
		// value: The value against which the field is being tested.
		// type: The type of condition being tested (e.g. less than, greater than, etc.)
		//
		// EXAMPLE: to query for cases where "score" is greater than 10, you'd pass in:
		// "score", 10, DBConditionType.GREATER_THAN
		//
		public DBCondition(string _field, object _value, DBConditionType _type = DBConditionType.EQUAL)
		{
			field = _field;
			type = _type;
			value = _value;
		}

		// Convert the condition to a string a SQL "WHERE" string.
		//
		// EXAMPLE: Constructor example would evaluate to "WHERE 'score' > 10"
		//
		public override string ToString ()
		{
			string op = "=";
			switch (type) {
			case DBConditionType.GREATER_THAN:
				op = ">";
				break;
			case DBConditionType.GREATER_THAN_EQUAL:
				op = ">=";
				break;
			case DBConditionType.LESS_THAN:
				op = "<";
				break;
			case DBConditionType.LESS_THAN_EQUAL:
				op = "<=";
				break;
			case DBConditionType.EQUAL:
				op = "=";
				break;
			default:
				Debug.LogError("Unknown condition type: " + type);
				break;
			}

			return field + " " + op + " '" + value + "'";
		}
	}

	string tableName;												// Name of the table from which we're querying.
	List<DBCondition> conditionList = new List<DBCondition>();		// List of conditions to filter down the query.
	List<string> fieldList = new List<string>();					// List of fields to retrieve for the result.
	bool orderAscending;											// true for ascending order, false for descending.
	string orderField;												// Field by which we decide the order (if any).
	int limit;														// Maximum number of results to return.

	public int ConditionCount {
		get {
			return conditionList.Count;
		}
	}

	// Create a new database query.  Base query will grab the entire specified table in no specific order.
	//
	// Use conditions, fields, order, and limit to retrieve more specific types of results.
	//
	public DBQuery(string table)
	{
		tableName = table;
		orderField = null;
		orderAscending = true;
		limit = -1;
	}

	// Add a condition to narrow down results based on values in a specific field.
	//
	// EXAMPLE: to query for cases where "score" is greater than 10, you'd pass in:
	// "score", 10, DBConditionType.GREATER_THAN
	//
	public void AddCondition(string fieldName, object fieldValue, DBConditionType comparisonType = DBConditionType.EQUAL)
	{
		conditionList.Add(new DBCondition(fieldName, fieldValue, comparisonType));
	}

	// Add specific fields to return in the result, rather than just grabbing all available fields.
	//
	// fields: list of fields to add to result rows.
	//
	public void AddFields(string[] fields)
	{
		foreach (string field in fields) {
			fieldList.Add (field);
		}
	}

	// Set the order in which the rows should appear, based on the value in a field.
	//
	// orderByField: the field by which you're ordering the results.
	// increasingOrder: true to return results in ascending order, false for descending order.
	//
	public void SetOrder(string orderByField, bool increasingOrder)
	{
		orderAscending = increasingOrder;
		orderField = orderByField;
	}

	// Limit the number of results returned.
	//
	// resultLimit: maximum number of results to return.
	//
	public void SetResultLimit(int resultLimit)
	{
		limit = resultLimit;
	}

	private string GetFieldString()
	{
		string fieldString = "";
		if (fieldList.Count == 0) {
			fieldString = "*";
		} else {
			foreach (string field in fieldList) {
				if (fieldString.Length > 0) {
					fieldString += ", ";
				}
				fieldString += field;
			}
		}
		return fieldString;
	}

	private string GetWhereString()
	{
		string whereString = "";
		if (conditionList.Count > 0) {
			foreach (DBCondition qc in conditionList) {
				if (whereString.Length > 0) {
					whereString += " AND ";
				}
				whereString += qc.ToString ();
			}
			whereString = " WHERE " + whereString;
		}
		return whereString;
	}

	private string GetOrderString()
	{
		if (orderField != null) {
			return " ORDER BY " + orderField + (orderAscending ? " ASC" : " DESC");
		} else {
			return "";
		}
	}

	private string GetLimitString()
	{
		if (limit > 0) {
			return " LIMIT " + limit;
		} else {
			return "";
		}
	}

	// Convert the query to a SQL string.
	//
	public override string ToString ()
	{
		string fieldString = GetFieldString();
		string whereString = GetWhereString();
		string orderString = GetOrderString ();
		string limitString = GetLimitString ();

		string query = "SELECT " + fieldString + " FROM " + tableName + whereString + orderString + limitString;
		return query;
	}

	public string ToUpdateString(DBRow updateValues)
	{
		string query = "UPDATE " + tableName + " SET " + updateValues.GetUpdateValueString() + GetWhereString();
		return query;
	}

	public string ToDeleteString()
	{
		string query = "DELETE FROM " + tableName + GetWhereString ();
		return query;
	}
}

// Information used for setting up a new table field.
//
public class DBFieldInfo {
	
	DBFieldType type;		// Data type of the field.
	string name;			// Name of the field.
	bool unique;			// True if all rows must have a unique value in this field.
	bool primaryKey;		// True if this is to be used as the primary unique identifier in this table.
	bool required;			// False to allow null values.
	int size;				// Size of the field (used by VARCHAR only)

	// name: name of the field being created.
	// type: the data type of the field being created.
	// size: (for VARCHAR only) the size of the field.
	//
	public DBFieldInfo(string _name, DBFieldType _type, int _size = 0)
	{
		type = _type;
		name = _name;
		required = false;
		primaryKey = false;
		unique = false;
		size = _size;
	}

	// True if no two rows in the table should be allowed to have the same value in this field.
	//
	public void SetUnique(bool _unique = true)
	{
		unique = _unique;
	}

	// True if this field cannot be left null.
	//
	public void SetRequired(bool _required = true)
	{
		required = _required;
	}

	// True if this field is to be used as the primary unique identifier for the row.
	//
	public void SetPrimaryKey(bool _primaryKey = true)
	{
		primaryKey = _primaryKey;
	}

	// Convert to a string which can be used to create the field in a table.
	//
	// EXAMPLE: "username VARCHAR(10) NOT NULL PRIMARY KEY"
	//
	public override string ToString ()
	{
		string fieldString = name;
		switch (type) {
		case DBFieldType.INT:
			fieldString += " INT";
			break;
		case DBFieldType.TEXT:
			fieldString += " TEXT";
			break;
		case DBFieldType.VARCHAR:
			fieldString += " VARCHAR(" + size + ")";
			break;
		default:
			Debug.LogError ("Uknown type in create table.  Assigning TEXT.");
			fieldString += " TEXT";
			break;
		}

		if (required) {
			fieldString += " NOT NULL";
		}
		if (unique) {
			fieldString += " UNIQUE";
		}
		if (primaryKey) {
			fieldString += " PRIMARY KEY";
		}

		return fieldString;
	}
}

// In-Game object used to access SQLite databases.
//
public class SloneDB : MonoBehaviour {

	const string defaultDBFilename = "GameData.db";
	static bool queryDebug = false;

	// Access the singleton instance of SloneDB via SloneDB.inst.
	//
	private static SloneDB _inst;
	public static SloneDB inst {
		get {
			if (_inst == null) {
				_inst = GameObject.FindObjectOfType<SloneDB> ();
				if (_inst == null) {
					GameObject o = new GameObject ();
					_inst = o.AddComponent<SloneDB> ();
					_inst.CreateDB ();
				}
			}

			return _inst;
		}
	}

	// Maintained connection to the database.
	private SqliteConnection dbConnection;

	// Be sure to destroy the database connection when the object is destroyed.
	//
	void OnDisable()
	{
		if (dbConnection != null) {
			dbConnection.Close ();
			dbConnection = null;
		}
	}

	// If query debugging is on, this will output query to the console.
	//
	public static void QueryDebug(string query)
	{
		if (queryDebug) {
			Debug.Log ("Query: " + query);
		}
	}

	// Create a database with the assigned filename.
	//
	public void CreateDB()
	{
		string filename = defaultDBFilename;

		SloneDBConfig config = GameObject.FindObjectOfType<SloneDBConfig> ();
		if (config) {
			filename = config.databaseFilename;
			queryDebug = config.queryDebug;
		}

		string fullFileName = Application.persistentDataPath + "/" + filename;
		if (!System.IO.File.Exists (fullFileName)) {
			Debug.Log ("Creating new database at: " + fullFileName);
			SqliteConnection.CreateFile (fullFileName);
		} else {
			Debug.Log ("Opening existing database at: " + fullFileName);
		}

		dbConnection = new SqliteConnection ("URI=file:" + fullFileName);
		dbConnection.Open ();
	}

	// Check if the open database has the requested table.
	//
	// tableName: the name of the table to check for.
	//
	public bool HasTable(string tableName)
	{
		string query = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "'";
		QueryDebug (query);
		SqliteCommand cmd = new SqliteCommand(query, dbConnection);
		SqliteDataReader reader = cmd.ExecuteReader ();
		bool exists = reader.HasRows;
		reader.Close ();
		return exists;
	}

	// Add a row to a table.
	//
	// tableName: name of the table we're adding to.
	// entry: the entry being inserted into the table.
	//
	public bool AddEntry(string tableName, DBRow entry)
	{
		string fieldList = "";
		string valueList = "";

		foreach(KeyValuePair<string, string> kvp in entry.strValues)
		{
			if (fieldList.Length > 0) {
				fieldList += ", ";
				valueList += ", ";
			}

			fieldList += kvp.Key;
			valueList += "'" + kvp.Value + "'";
		}

		foreach (KeyValuePair<string, long> kvp in entry.intValues) {
			if (fieldList.Length > 0) {
				fieldList += ", ";
				valueList += ", ";
			}

			fieldList += kvp.Key;
			valueList += kvp.Value;
		}

		string query = "INSERT INTO " + tableName + " (" + fieldList + ") VALUES (" + valueList + ")";
		QueryDebug (query);
		SqliteCommand cmd = new SqliteCommand (query, dbConnection);
		return cmd.ExecuteNonQuery () > 0;
	}

	// Get the highest value for the column that appears in the table, and add one.
	//
	// table: name of the table to retrieve from
	// field: name of the field to auto-increment
	//
	// Returns 1 more than the highest value for the field in the table.  Returns 0 if no entry exist.
	//
	public int GetAutoIncrementValue(string table, string field)
	{
		string query = "SELECT MAX(" + field + ") as highest_val FROM " + table;
		DBResult res = ExecuteRawQuery (query);
		if (res.rows.Count == 0) {
			return 0;
		} else if (!res.rows [0].intValues.ContainsKey ("highest_val")) {
			return 0;
		} else {
			return (int)(res.rows [0].intValues ["highest_val"] + 1);
		}
	}

	// Execute a selection query from raw SQL.
	//
	// sql: the SQL query to execute.
	//
	public DBResult ExecuteRawQuery(string query)
	{
		DBResult res = new DBResult (dbConnection, query);
		return res;
	}

	// Execute a selection query from a DBQuery object.
	//
	public DBResult ExecuteQuery(DBQuery query)
	{
		return new DBResult (dbConnection, query.ToString());
	}

	// Execute an update query from a DBQuery object and DBRow.
	//
	// whichRows: defines which rows to update.
	// updateValues: defines the values to update within each row.
	//
	// returns: true if something was updated, false if not.
	//
	public bool ExecuteUpdate(DBQuery whichRows, DBRow updateValues, bool allowNoConditions = false)
	{
		if (whichRows.ConditionCount == 0 && !allowNoConditions) {
			Debug.LogError("Cannot update all rows in a table without setting allowNoConditions");
			return false;
		}

		string query = whichRows.ToUpdateString (updateValues);

		QueryDebug (query);
		SqliteCommand cmd = new SqliteCommand (query, dbConnection);
		return cmd.ExecuteNonQuery () > 0;
	}

	// Execute a delete on the rows described in "whichRows."
	//
	// whichRows: defines which rows to delete.
	// allowNoConditions: set to true only if you'll allow the delete to happen without
	//                    filtering which rows to delete.
	//
	// returns: true if something was deleted, false if not.
	//
	public bool ExecuteDelete(DBQuery whichRows, bool allowNoConditions = false)
	{
		if (whichRows.ConditionCount == 0 && !allowNoConditions) {
			Debug.LogError("Cannot delete all rows in a table without setting allowNoConditions");
			return false;
		}

		string query = whichRows.ToDeleteString ();
		QueryDebug (query);
		SqliteCommand cmd = new SqliteCommand (query, dbConnection);
		return cmd.ExecuteNonQuery () > 0;
	}

	// Get the entire contents of the specified table.
	//
	// name: the name of the table to retrieve.
	//
	public DBResult GetTable(string name)
	{
		DBQuery query = new DBQuery (name);
		return ExecuteQuery (query);
	}

	// Create a new table.
	//
	// name: the name of the table to create.
	// fields: the fields the table will have.
	//
	public void CreateTable(string name, List<DBFieldInfo> fields)
	{
		if (HasTable (name)) {
			Debug.LogError ("CreateTable - Table already exists: " + name);
			return;
		}

		string fieldList = "";
		foreach (DBFieldInfo field in fields) {

			if (fieldList.Length > 0) {
				fieldList += ", ";
			}

			fieldList += field.ToString ();
		}

		string query = "CREATE TABLE " + name + "(" + fieldList + ")";
		QueryDebug (query);
		SqliteCommand cmd = new SqliteCommand (query, dbConnection);
		cmd.ExecuteNonQuery ();
		if (!HasTable (name)) {
			Debug.LogError ("Failed to create table. Query: " + query);
		}
	}
}
