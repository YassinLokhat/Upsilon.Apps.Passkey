**Upsilon.Apps.Passkey**
=============================================

**Overview**
------------

This is a C# implementation of a local stored password manager in .Net 10. The application provides a secure way to store and manage passwords locally on the user's device.

**Features**
------------

*   **Password Storage**: Store accounts and services passwords securely
*   **History log**: Log every events
*   **Trigger warnings**: Trigger warnings when detected
*   **Autosave**: Autosave updates
*   **Password Generation**: Generate strong, unique passwords

**Security**
------------

*   **Encryption**: All passwords are encrypted using AES with a set of keys and RSA with a 4096-bit key
*   **Access Control**: Access to the password store is restricted to authorized users only

**Models**

----------

### Class diagram
```mermaid
classDiagram
    direction LR

    %% Main Interfaces

    namespace Upsilon.Apps.Passkey.Interfaces.Utils {
        class ISerializationCenter {
            <<interface>>
            +Serialize(in toSerialize T) string
            +Deserialize(in toDeserialize string) T
        }

        class IClipboardManager {
            <<interface>>
            +RemoveAllOccurrence(in removeList IEnumerable~string~) int
        }

        class IPasswordFactory {
            <<interface>>
            +string Alphabetic
            +string Numeric
            +string SpecialChars

            +GeneratePassword(in length int, in alphabet string, in checkIfLeaked bool) string
            +PasswordLeaked(in password string) bool
        }

        class ICryptographyCenter {
            <<interface>>
            +int HashLength
            +KdfParameters DefaultSlowHashParameters

            +GetHash(in source string) string
            +GetSlowHash(in source string, in parameters KdfParameters) string
            +EncryptSymmetrically(in source string, in passwords IEnumerable~string~) string
            +DecryptSymmetrically(in source string, in passwords IEnumerable~string~) string
            +GenerateRandomKeys(out publicKey string, out privateKey string) void
            +EncryptAsymmetrically(in source string, in key string) string
            +DecryptAsymmetrically(in source string, in key string) string
            +GetPublicKey(in privateKey string) string
            +Sign(in source string, in privateKey string) string
            +Verify(in source string, in signature string, in publicKey string) bool
        }

        class KdfParameters {
            +int Version
            +KdfAlgorithm Algorithm
            +int Iterations
            +int OutputLength
            +string Salt
        }
    }

    namespace Upsilon.Apps.Passkey.Interfaces.Models {
        class IItem {
            <<interface>>
            +string ItemId
            +IDatabase Database
            +HasChanged(void) bool
        }

        class IAccount {
            <<interface>>
            +IService Service
            +string Label
            +string Notes
            +IEnumerable~string~ Identifiers
            +string Password
            +Dictionary~DateTime_string~ Passwords
            +int PasswordUpdateReminderDelay
            +AccountOption Options
        }

        class IService {
            <<interface>>
            +IUser User
            +string ServiceName
            +Uri Url
            +string Notes
            +IEnumerable~IAccount~ Accounts
            +AddAccount(in label string, in identifiers IEnumerable~string~, in password string) IAccount
            +AddAccount(in label string, in identifiers IEnumerable~string~) IAccount
            +AddAccount(in identifiers IEnumerable~string~, in password string) IAccount
            +AddAccount(in identifiers IEnumerable~string~) IAccount
            +DeleteAccount(in account IAccount) void
        }

        class IUser {
            <<interface>>
            +string Username
            +IEnumerable~string~ Passkeys
            +int LogoutTimeout
            +int CleaningClipboardTimeout
            +int ShowPasswordDelay
            +int NumberOfOldPasswordToKeep
            +int NumberOfMonthActivitiesToKeep
            +WarningType WarningsToNotify
            +IEnumerable~IService~ Services
            +AddService(in serviceName string) IService
            +DeleteService(in service IService) void
        }

        class IDatabase {
            <<interface>>
            +string DatabaseFile
            +IUser User
            +int SessionLeftTime
            +IEnumerable~IActivity~ Activities
            +IEnumerable~IWarning~ Warnings
            +ISerializationCenter SerializationCenter
            +ICryptographyCenter CryptographyCenter
            +IPasswordFactory PasswordFactory
            +IClipboardManager ClipboardManager
            +EventHandler~WarningsUpdatedEventArgs~ WarningsUpdated
            +EventHandler~AutoSaveDetectedEventArgs~ AutoSaveDetected
            +EventHandler DatabaseSaved
            +EventHandler~LogoutEventArgs~ DatabaseClosed
            +Login(in passkey string) IUser
            +Login(in passkey SecureString) IUser
            +Save(void) void
            +Delete(void) void
            +Close(void) void
            +HasChanged(in itemId string) bool
            +HasChanged(in itemId string, in fieldName string) bool
            +ImportFromFile(in filePath string) bool
            +ExportToFile(in filePath string) bool
        }

        class IActivity {
            <<interface>>
            +DateTime DateTime
            +string ItemId
            +ActivityEventType EventType
            +string Message
            +bool NeedsReview
        }

        class IWarning {
            <<interface>>
            +WarningType WarningType
            +IEnumerable~IActivity~ Activities
            +IEnumerable~IAccount~ Accounts
        }
    }
    
    %% Enums
    namespace Upsilon.Apps.Passkey.Interfaces.Enums {
        class AccountOption {
            <<enumeration>>
            None
            WarnIfPasswordLeaked
            WarnIfDuplicatedPassword
        }
        
        class WarningType {
            <<enumeration>>
            ActivityReviewWarning
            PasswordUpdateReminderWarning
            DuplicatedPasswordsWarning
            PasswordLeakedWarning
        }
        
        class AutoSaveMergeBehavior {
            <<enumeration>>
            Undefined
            MergeAndSaveThenRemoveAutoSaveFile
            MergeWithoutSavingAndKeepAutoSaveFile
            DontMergeAndRemoveAutoSaveFile
            DontMergeAndKeepAutoSaveFile
        }

        class KdfAlgorithm {
            <<enumeration>>
            Pbkdf2HmacSha256
            Pbkdf2HmacSha512
        }

        class ActivityEventType {
            <<enumeration>>
            None
            MergeAndSaveThenRemoveAutoSaveFile
            MergeWithoutSavingAndKeepAutoSaveFile
            DontMergeAndRemoveAutoSaveFile
            DontMergeAndKeepAutoSaveFile
            DatabaseCreated
            DatabaseOpened
            DatabaseSaved
            DatabaseClosed
            LoginSessionTimeoutReached
            LoginFailed
            UserLoggedIn
            UserLoggedOut
            ImportingDataStarted
            ImportingDataSucceded
            ImportingDataFailed
            ExportingDataStarted
            ExportingDataSucceded
            ExportingDataFailed
            ItemUpdated
            ItemAdded
            ItemDeleted
            ActivityLogTampered
        }
    }
    
    %% Event Args Classes
    namespace Upsilon.Apps.Passkey.Interfaces.Events {
        class AutoSaveDetectedEventArgs {
            +AutoSaveMergeBehavior MergeBehavior
        }
        
        class WarningsUpdatedEventArgs {
            +IEnumerable~IWarning~ Warnings
        }
        
        class LogoutEventArgs {
            +bool LoginTimeoutReached
        }
    }

    %% Inheritance Relations
    IUser --|> IItem
    IService --|> IItem
    IAccount --|> IItem
    
    %% Link Relations
    IItem --> IDatabase : Database
    IAccount --> IService : Service
    IAccount --> AccountOption : Options
    IActivity --> ActivityEventType : EventType
    ICryptographyCenter --> KdfParameters : DefaultSlowHashParameters
    KdfParameters --> KdfAlgorithm : Algorithm
    IService "0" --> "*" IAccount : Accounts
    IService --> IUser : User
    IUser "0" --> "*" IService : Services
    IDatabase --> ISerializationCenter : SerializationCenter
    IDatabase --> ICryptographyCenter : CryptographyCenter
    IDatabase --> IPasswordFactory : PasswordFactory
    IDatabase --> IClipboardManager : ClipboardManager
    IDatabase --> IUser : User
    IDatabase "0" --> "*" IWarning : Warnings
    IDatabase "0" --> "*" IActivity : Activities
    IDatabase --> WarningsUpdatedEventArgs : WarningsUpdated
    IDatabase --> AutoSaveDetectedEventArgs : AutoSaveDetected
    IDatabase --> LogoutEventArgs : DatabaseClosed
    IWarning --> WarningType : WarningType
    IWarning "0" --> "*" IActivity : Activities
    IWarning "0" --> "*" IAccount : Accounts
    AutoSaveDetectedEventArgs --> AutoSaveMergeBehavior : MergeBehavior
    WarningsUpdatedEventArgs "0" --> "*" IWarning : Warnings
```

**Example Use Cases**

--------------------

### Create a new database

To create a new database, use the `Upsilon.Apps.Passkey.Core.Models.Database.Create` static method.

This method needs an `ICryptographyCenter` implementation, an `ISerializationCenter` implementation, an `IPasswordFactory` implementation and an `IClipboardManager` implementation.
The namespace `Upsilon.Apps.Passkey.Core.Utils` already contains implementations for all of these interfaces except for the `IClipboardManager` which needs an OS specific implementation.

The next parameter is the database file itself, which will be created during the process.

Finally, the method take the username and the passkeys.
Note that the passkeys are used as master passwords to encrypt the database (and the other files).

```csharp
IDatabase database = Upsilon.Apps.Passkey.Core.Models.Database.Create(new Upsilon.Apps.Passkey.Core.Utils.CryptographyCenter(),
   new Upsilon.Apps.Passkey.Core.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.Passkey.Core.Utils.PasswordFactory(),
   new OSSpecificClipboardManager(),
   "./database.pku",
   "username",
   new string[] { "master_password_1", "master_password_2", "master_password_3" });
```

After creation, the method will directly open the database but it will not login directly to the current user.
So to login, check the **Login to an user** use case.

### Open an existing database

To open an existing database, use the `Upsilon.Apps.Passkey.Core.Models.Database.Open` static method.

This method needs an `ICryptographyCenter` implementation, an `ISerializationCenter` implementation, an `IPasswordFactory` implementation and an `IClipboardManager` implementation as in the creation step.

The next parameter is the database file itself and must, obviously, exist.

Finally, the method take the username.

```csharp
IDatabase database = Upsilon.Apps.Passkey.Core.Models.Database.Open(new Upsilon.Apps.Passkey.Core.Utils.CryptographyCenter(),
   new Upsilon.Apps.Passkey.Core.Utils.JsonSerializationCenter(),
   new Upsilon.Apps.Passkey.Core.Utils.PasswordFactory(),
   new OSSpecificClipboardManager(),
   "./database.pku",
   "username");
```

### Login to an user

After opening (or creating) a database, use the `IDatabase.Login` method to login the user.
To do that, call the login method with every passkeys used during the database creation process.
Only the last call of that method, with every correct and ordered passkeys, will return the `IUser` representing the current user successfully logged in.
Else that method will return `null`.

```csharp
IUser? user = database.Login("master_password_1");	// Will return null
user = database.Login("master_password_2");			// Will also return null
user = database.Login("master_password_3");			// Will return a IUser this time
```

**Important — no rollback on a wrong passkey.** Each `Login` call appends the
passkey to the in-memory onion stack. A mistyped value is never undone: further
`Login` calls keep stacking on top of it, so even the correct passkeys will keep
failing until you `Close()` the database and `Open` it again. That is intentional
(an online anti-brute-force friction layer on top of PBKDF2); see
[SECURITY.md](SECURITY.md#progressive-login-without-rollback-online-brute-force-friction).
In the GUI, cancelling the login (e.g. Escape) ends the session so the user can
restart cleanly.

Once the IUser retrieved, it allow a full access to all services and accounts, all log history and all user parameters.

### Saving the changes

Use the `IDatabase.Save` method to save the user's updates.
Note that any update on the user, its services and/or accounts which is not saved will be kept in a hidden autosave file.

```csharp
user.LogoutTimeout = 5;	// Setting the logout timeout to 5 min will create a hidden autosave file
database.Save();		// Will save the new logout timeout in the database file and remove the autosave file
```

### Logout/Close a database

To logout and close the database, use the `IDatabase.Close` method.
All unsaved updates are stored inside the hidden autosave file.

```csharp
database.Close();
```

**Getting Started**
-------------------

1.  Clone the repository: `git clone https://github.com/YassinLokhat/Upsilon.Apps.Passkey.git`
2. 1. Build the solution for Windows users: `dotnet build Upsilon.Apps.Passkey.Windows.slnx`
2. 2. Build the solution for Linux users: `dotnet build Upsilon.Apps.Passkey.Linux.slnx`

**Contributing**
------------

Contributions are welcome! Please submit a pull request with your changes.

**License**
-------

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
