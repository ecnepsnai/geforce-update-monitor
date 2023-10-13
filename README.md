# Geforce Update Monitor

A Windows application to monitor for Geforce Driver Updates without the need for Geforce Experience.

![screenshot](https://github.com/ecnepsnai/geforce-update-monitor/assets/1607109/86379ec8-709c-40eb-a0c4-392d95b84549)

## Install

> [!NOTE]  
> The setup experience is currently manual. Automatic setup will come at a later date.

1. Download the latest release and place it somewhere permanent, such as AppData
1. Create `AppData\Roaming\GeforceUpdateMonitor\config.txt` with the contents:
   ```
   series_id = 107
   family_id = 904
   os_id = 135
   language_code = 1033
   ```
   You can determine what values to use by filling out the form on https://www.nvidia.com/en-us/geforce/drivers/ and inspecting the network request in developer tools. Look at the query parameters, i.e.: `psid=129&pfid=1004&osID=135&languageCode=1033`
1. Add a scheduled task with whatever frequency you want to run GeforceUpdateMonitor.exe

