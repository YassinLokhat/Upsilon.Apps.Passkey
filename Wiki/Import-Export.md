# Import and Export

`ImportFromFile` / `ExportToFile` (and their `Async` twins) are routed by **file extension**. Only `.json` and `.csv` are supported; any other extension fails and is recorded on the activity log.

Import requires a logged-in user. Export and import files are **unencrypted plaintext** — see [[Security]] known limitations. Protect or delete them after use.

## What each format carries

| Format | Settings | Services and accounts | Password history |
| ------ | -------- | --------------------- | ---------------- |
| `.json` | Yes | Yes | Yes (`Passwords` dictionary) |
| `.csv` | No | Yes | No (current password only) |

The `.csv` path is **tab-separated (TSV)** with **JSON-encoded cells**, so commas, quotes, and notes survive. Identifiers inside a cell are joined with `|`.

## JSON shape

Enums use `JsonStringEnumConverter`. Flags (`Options`, `WarningsToNotify`) are comma-separated names.

`ItemId` values appear on export. Import assigns identities through `AddService` / `AddAccount`; do not rely on round-tripping ids as a merge key. Import **fails** if a service name already exists in the vault or is blank — there is no merge-by-name.

```json
{
  "Settings": {
    "LogoutTimeout": 9,
    "CleaningClipboardTimeout": 99,
    "ShowPasswordDelay": 999,
    "NumberOfOldPasswordToKeep": 9,
    "NumberOfMonthActivitiesToKeep": 9,
    "WarningsToNotify": "PasswordUpdateReminderWarning, DuplicatedPasswordsWarning, PasswordLeakedWarning"
  },
  "Services": [
    {
      "ServiceName": "GitHub",
      "Url": "https://github.com",
      "Notes": "Work org",
      "Accounts": [
        {
          "Label": "work",
          "Identifiers": [
            "alice@company.com",
            "alice-backup"
          ],
          "Password": "use-a-real-secret",
          "Passwords": {
            "2025-11-28T14:48:28.6023277+03:00": "use-a-real-secret"
          },
          "Notes": "",
          "PasswordUpdateReminderDelay": 6,
          "Options": "WarnIfPasswordLeaked, WarnIfDuplicatedPassword"
        }
      ]
    }
  ]
}
```

`AccountOption` values: `None`, `WarnIfPasswordLeaked`, `WarnIfDuplicatedPassword` (flags).

## CSV / TSV shape

Required headers, in any column order as long as **all names are present**:

`ServiceName`, `ServiceUrl`, `ServiceNotes`, `AccountLabel`, `Identifiers`, `Password`, `AccountNotes`, `AccountOptions`, `PasswordUpdateReminderDelay`

Example (tabs between columns; each cell is a JSON string):

```
ServiceName	ServiceUrl	ServiceNotes	AccountLabel	Identifiers	Password	AccountNotes	AccountOptions	PasswordUpdateReminderDelay
"GitHub"	"https://github.com"	"Work org"	"work"	"alice@company.com|alice-backup"	"secret"	""	"WarnIfPasswordLeaked"	6
"GitHub"	"https://github.com"	"Work org"	"personal"	"alice@pm.me"	"other-secret"	"2FA"	"None"	0
```

Two rows with the same `ServiceName` become two accounts on one service, in file order.

## API usage

```csharp
bool imported = await database.ImportFromFileAsync(@"C:\temp\migration.json");
bool exported = await database.ExportToFileAsync(@"C:\temp\backup.csv");
```

Both return `false` on failure (missing file, bad extension, empty data, duplicate service name, malformed cells, and so on). Details are written as `ImportingDataFailed` / `ExportingDataFailed` activities — see [[Warnings and Activity]].

## Concrete migration from another password manager

1. Export the other tool to CSV or JSON.
2. Reshape columns to the header list above. JSON-encode **each** TSV cell (a raw unquoted field will fail parse).
3. Unlock Passkey. If a service named `GitHub` already exists, rename or delete it first — import is all-or-nothing on that check.
4. `ImportFromFile("migration.csv")` then `Save()`.
5. **Securely delete** the plaintext file (and any copies in Recycle Bin / cloud sync folders).

## Failure modes (from tests)

| Situation | Typical activity message |
| --------- | ------------------------ |
| File missing | import file is not accessible |
| Extension `.txt` (or anything but `.json` / `.csv`) | extension type is not handled |
| Headers only / no rows | there is no data to import |
| Service name already in the vault | service '…' already exists |
| Blank service name | service name cannot be blank |
| Missing TSV header | the CSV headers should be : 'ServiceName', … |
| Broken JSON / broken TSV cells | deserialization failed / CSV data format is incorrect |

URL handling on import: a service URL is kept only if `Uri.IsWellFormedUriString` accepts it; otherwise `Url` is `null`.
