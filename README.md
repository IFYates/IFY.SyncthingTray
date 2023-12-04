# Syncthing Tray
Windows systray icon for monitoring local [Syncthing](https://github.com/syncthing/syncthing) status.

# Features
The tool displays a systray icon, showing the running state of the target Syncthing service.
The icon immediately shows when the service has encountered an issue or is currently working on syncing files.

# Configuration
In the `appsettings.json` file, set the following values:
* `Syncthing:Host`: The host and port of your Syncthing UI
  This is likely the default of `127.0.0.1:8384`
* `Syncthing:ApiKey`: The API key of your Syncthing UI, found under Actions > Settings
* `Syncthing:NotifyOnFailure`: Set to `true` if you want the popup to appear automatically on a Syncthing failure

**Example:**
```json
{
  "Syncthing": {
    "Host": "127.0.0.1:8384",
    "ApiKey": "my-local-apikey",
    "NotifyOnFailure": true
  }
}
```