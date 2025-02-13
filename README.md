

**Upsilon.Apps.Passkey.Core**
=============================================

**Overview**
------------

This C# implementation serves as the core of a password manager, enabling secure local storage and management of user credentials.

**Features**
------------

*   **Password Storage**: Store accounts and services passwords securely
*   **History log**: Log every events
*   **Trigger warnings**: Trigger warnings when detected like unauthorized access attempt or password leaked detection
*   **Autosave**: Autosave updates
*   **Password Generation**: Generate strong, unique passwords

**Security**
------------

*   **Encryption**: All passwords are encrypted using AES with a set of keys and RSA with a 1024-bit key
*   **Access Control**: Access to the password store is restricted to authorized users only

**Models**

----------

### Class diagram
![ClassDiagram](https://github.com/user-attachments/assets/7c17f2a2-c62b-4a9a-808f-c0f471c23609)

**Example Use Cases**

--------------------

### Create a new database

To create a new database, use the `IDatabase.Create` static method.

This method needs an `ICryptographyCenter` implementation, an `ISerializationCenter` implementation and an `IPasswordFactory` implementation.
The namespace `Upsilon.Apps.PassKey.Core.Public.Utils` already contains implementations for all of these intefaces.

The next parameters are a set of files : the database file itself, the autosave file and the log file.
These files will be created during the process.

Finally, the method take the username and the passkeys.
Note that the passkeys are used as master passwords to encrypt the database (and the other files).

```csharp
IDatabase database = IDatabase.Create(new Upsilon.Apps.PassKey.Core.Public.Utils.CryptographyCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.PasswordFactory(),
   "./database.pku",
   "./autosave.pks",
   "./log.pkl",
   "username",
   new string[] { "master_password_1", "master_password_2", "master_password_3" });
```

After creation, the method will directly open the database but it will not login directly to the current user.
So to login, check the **Login to an user** use case.

### Open an existing database

To open an existing database, use the `IDatabase.Open` static method.

This method needs the same `ICryptographyCenter` implementation, `ISerializationCenter` implementation and `IPasswordFactory` implementation as in the creation step.

The next parameters are a set of files : the database file itself, the autosave file and the log file.
The database file must, obviously, exist, the autosave file and log files are optional but must be the same as provided during the creating process.

Finally, the method take the username.

```csharp
IDatabase database = IDatabase.Open(new Upsilon.Apps.PassKey.Core.Public.Utils.CryptographyCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.PasswordFactory(),
   "./database.pku",
   "./autosave.pks",
   "./log.pkl",
   "username");
```

### Login to an user

After opening (or creating) a database, use the `IDatabase.Login` method to login the user.
To do that, call the login method with every passkeys used during the database creation process.
Only the last call of that method, with every correct and ordered passkeys, will return the `IUser` representing the current user successfuly loged in.
Else that method will return `null`.

```csharp
IUser? user = database.Login("master_password_1");	// Will return null
user = database.Login("master_password_2");			// Will also return null
user = database.Login("master_password_3");			// Will return a IUser this time
```

Once the IUser retrieved, it allow a full access to all services and accounts, all log history and all user parameters.

### Saving the changes

Use the `IDatabase.Save` method to save the user's updates.
Note that any update on the user, its services and/or accounts which is not saved will be keeped in the autosave file.

```csharp
user.LogoutTimeout = 5;	// Setting the logout timeout to 5 min will create an autosave file
database.Save();		// Will save the new logout timeout in the database file and removed the autosave file
```

### Logout/Close a database

To logout and close the database, use the `IDatabase.Close` method.
All unsaved updates are stored inside the autosave file.

```csharp
database.Close();
```

**Getting Started**
-------------------

1.  Clone the repository: `git clone https://github.com/YassinLokhat/Upsilon.Apps.Passkey.Core.git`
2.  Build the solution: `dotnet build`
3.  Run the API: `dotnet run`

**Contributing**
------------

Contributions are welcome! Please submit a pull request with your changes.

**License**
-------

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
