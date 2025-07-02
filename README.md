# SloneDB
Wrapper for locally-stored game database.

## License
This code is freely available to you via the [MIT License](https://mit-license.org/)

### NOTE 1
For this to work in a built game, you have to change the API compatibility level to ".Net 2.0" instead of ".Net 2.0 Subset" and copy System.Data.dll and Mono.Data.Sqlite.dll into the Plugins folder for your project.  These files can be found online.

### NOTE 2
You must search online to find the appropriate sqlite3.dll and sqlite3.def files for your platform (x86 and/or x64) and place them in your plugins folder.  This will not work without that.  In many cases you may need to get both, then go into the asset properties for the dll files and assign the x64 version to be used by the editor, and assign the x86 version to be used by your Player.

## Example
To see how to set up a table, insert, update, select, and delete from it, check out the example in the TestSloneDB scene.

## Enums

### DBFieldType
* **INT** - 64-bit integer field.  Will be stored and retrieved as a 'long' and can be safely cast to 'int' if necessary.  This entry should also be used to store dates.
* **TEXT** - Unbounded text field for longer text entries.
* **VARCHAR** - Bounded text field for shorter text entries.  Requires user to specify a size, which will be the maximum number of characters for the field.

### DBConditionType
Used to describe the type of condition being executed.  Equals for straight equality, or other conditions, such as less than, greater than, etc, for other comparison conditions.

## Classes

### SloneDB
In-Game object used to access SQLite databases.
SloneDB should always be accessed via SloneDB.inst, as it is set up as a singleton.
#### Methods
* **HasTable** - Returns true if the table exists in the database.  False if not.
  * **tableName** - Name of the table to look for.
* **AddEntry** - Add a row to a table.
  * **tableName** - Name of the table into which we're adding an entry.
  * **entry** - Row to be inserted in to the table.
* **CreateTable** - Create a new table.
  * **name** - Name of the table to create.
  * **fields** - List of fields the table will ahve.
* **GetTable** - Get the entire contents of the specified table.
  * **name** - Name of the table to retrieve.
* **ExecuteUpdate** - Execute an update query from a dBQuery object and DBRow.
  * **whichRows** - Defines which rows to update.
  * **updateValues** - Defines the values to update within each row.
* **ExecuteDelete** - Executea  delete query on the rows specified.
  * **whichRows** - Query which defines which rows should be deleted.
* **ExecuteRawQuery** - Run a raw selection SQL query string.
  * Returns DBResult result of query.
* **GetAutoIncrementValue** - Finds the highest value in the specified field, and returns one more.  If no entry is found, returns 0.
  * **table** - Table where the field exists.
  * **field** - Field to auto-increment.

### SloneDBConfig
Place this on an object in your scene to configure custom settings for the database, such as debugging output and database filename.

### DBRow
A single row from a database -- can read and add values to the row.

#### Methods
* **HasField** - Returns true if the row has a field by the specified name.
* **SetValueInt** - Set an integer value in the row.
  * **field** - Name of the field to set.
  * **value** - Value to set the specified field to.
* **SetValueString** - Set a string value in the row.
  * **field** - Name of the field to set.
  * **value** - Value to set the specified field to.
* **SetValueDate** - Set a System.DateTime value in the row.
  * **field** - Name of the field to set.
  * **value** Value to set the specifie field to.
* **GetValueInt** - Get an integer value from the row.
  * **field** - Name of the field to retrieve.
* **GetValueString** - Get a string value from the row.
  * **field** - Name of the field to retrieve.
* **GetValueDate** - Get a System.DateTime value from the row.
  * **field** - Name of the field to retrieve.
* **RemoveField** - Remove the specified field from the row.
  * **field** - Name of the field to remove.

### DBResult
Populates and maintains a database result from a string query.
Results stored in DBResult.rows.

### DBQuery
Used to construct queries without having to write any SQL.

#### Methods
* **Constructor**
  * **field** - The name of the field we are querying by.
  * **value** - The value by which we're making the comparison.
  * **type** - The type of comparison we're making between 'value' and the value in the table.

### DBFieldInfo
Information used for setting up a new table field.

#### Methods
* **Constructor**
  * **name** - name of the field being created.
  * **type** - the data type of the field being created.
  * **size** - (for VARCHAR only) the size of the field.
* **SetUnique**
  * **_unique** - True if no entry should be allowed to have the same value as another entry in this field.
* **SetRequired**
  * **_required** - True if no entry should exist that does not have this field.
* **SetPrimaryKey**
  * **_primaryKey** - True if this field is the primary key of this table.
