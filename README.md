

**Upsilon.Apps.Passkey.Core**
=============================================

**Overview**
------------

This is a C# implementation of a local stored password manager core API. The API provides a secure way to store and manage passwords locally on a user's device.

**Features**
------------

*   **Password Storage**: Store passwords securely using AES encryption
*   **Password Retrieval**: Retrieve stored passwords by their corresponding IDs
*   **Password Update**: Update existing passwords
*   **Password Deletion**: Delete stored passwords
*   **Password Generation**: Generate strong, unique passwords

**Security**
------------

*   **Encryption**: All passwords are encrypted using AES with a 256-bit key
*   **Key Management**: The encryption key is stored securely using a password-based key derivation function (PBKDF2)
*   **Access Control**: Access to the password store is restricted to authorized users only

**Models**
----------

### Class diagram
![Interfaces](https://github.com/user-attachments/assets/a4ad591c-1334-426f-86de-b7d264ea904b)

**Example Use Cases**

--------------------

### Create a new database

```csharp
IDatabase database = IDatabase.Create(new Upsilon.Apps.PassKey.Core.Public.Utils.CryptographyCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.SerializationCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.PasswordFactory(),
   "./database.pku",
   "./autosave.pks",
   "./log.pkl",
   "username",
   new string[] { "master_password_1", "master_password_2", "master_password_3" });
```

### Open an existing database

```csharp
IDatabase database = IDatabase.Open(new Upsilon.Apps.PassKey.Core.Public.Utils.CryptographyCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.SerializationCenter(),
   new Upsilon.Apps.PassKey.Core.Public.Utils.PasswordFactory(),
   "./database.pku",
   "./autosave.pks",
   "./log.pkl",
   "username");
```

### Login to an user

```csharp

```

**Getting Started**
-------------------

1.  Clone the repository: `git clone https://github.com/your-username/local-stored-password-manager-core-api.git`
2.  Build the solution: `dotnet build`
3.  Run the API: `dotnet run`
4.  Use a tool like Postman or cURL to interact with the API

**Contributing**
------------

Contributions are welcome! Please submit a pull request with your changes.

**License**
-------

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
