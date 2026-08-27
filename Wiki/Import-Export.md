# Import and Export

`ImportFromFile` / `ExportToFile` (and their `Async` twins) are routed by **file extension**. Only `.json` and `.csv` are supported; any other extension fails and is recorded on the activity log.

Import requires a logged-in user. Export and import files are **unencrypted plaintext** — see [[Security]] known limitations. Protect or delete them after use.

## What each format carries

| Format | Settings | Services and accounts | Password history |
| ------ | -------- | --------------------- | ---------------- |
| `.json` | Yes | Yes | Yes (`Passwords` dictionary) |
| `.csv` | No | Yes | No in the file (current password only); import seeds one dated history entry from that password so password-update reminders and expiry checks work immediately |

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

Both return `false` on failure (missing file, destination already exists on export, bad extension, empty data, duplicate service name, malformed cells, and so on). Failures append `ImportingDataFailed` / `ExportingDataFailed` activities with `FieldName = ImportExportError`, `FieldValue = <enum member name>`, and `NeedsReview = true`. Localization happens in the WPF client — see [[Warnings and Activity]].

### Persistence side effects

* If the vault has unsaved edits, **import and export both call `Save` first** so dirty state is not left only in `autosave`.
* A **successful import also saves** again (imported services/settings are persisted into `database` and the activity trail is flushed). You do not need a separate `Save()` after a successful import.
* **Export refuses to overwrite**: if the destination path already exists, export fails with `ImportExportError.ExportFileAlreadyExists` (localized as “export file already exists” inside the activity sentence). Choose a new path or delete the file first.

## Concrete migration from another password manager

1. Export the other tool to CSV or JSON.
2. Reshape columns to the header list above. JSON-encode **each** TSV cell (a raw unquoted field will fail parse).
3. Unlock Passkey. If a service named `GitHub` already exists, rename or delete it first — import is all-or-nothing on that check.
4. `ImportFromFile("migration.csv")` — on success the vault is already saved.
5. **Securely delete** the plaintext file (and any copies in Recycle Bin / cloud sync folders).

## Failure modes (from tests)

Core records failures as `ImportingDataFailed` / `ExportingDataFailed` activities with `FieldName = ImportExportError` and `FieldValue = <ImportExportError member name>`. The WPF client localizes the reason via `EnumValue_ImportExportError_*` keys; the full Message column uses `Activity_ImportingDataFailed` / `Activity_ExportingDataFailed` (`Import failed because {0}` / `Export failed because {0}`).

| Situation | `ImportExportError` | English reason (`EnumValue_ImportExportError_*`) |
| --------- | ------------------- | ------------------------------------------------ |
| File missing | `ImportFileNotAccessible` | import file is not accessible |
| Extension `.txt` (or anything but `.json` / `.csv`) | `ExtentionFileNotSupported` | file extension type is not supported |
| Headers only / no rows | `NoDataToImport` | no data to import |
| Service name already in the vault | `ServiceAlreadyExists` | a service already exists |
| Blank service name | `BlankService` | a service is blank |
| Missing TSV header | `CSVHeadersDontMatch` | the CSV header does not match |
| Broken JSON | `ImportFileDeserializationFailed` | import file deserialization failed |
| Broken TSV cells | `IncorrectCSVFormat` | the CSV format is incorrect |
| Export destination already exists | `ExportFileAlreadyExists` | export file already exists |

URL handling on import: a service URL is kept only if `Uri.IsWellFormedUriString` accepts it; otherwise `Url` is `null`.
