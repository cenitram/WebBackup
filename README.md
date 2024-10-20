# WebBackup

WebBackup is a simple tool for backing up data from a server using FTP. It downloads a selected folder from the server and saves it.

WebBackup is written in the .NET8 console application. It uses the [FluentFTP](https://github.com/robinrodricks/FluentFTP) library for FTP communication. 
For nice console output, it uses the [Spectre.Console](https://github.com/spectreconsole/spectre.console) library.

## Configuration
The appsetting.json file is used to set up the application. 
All fields must be filled in before starting the application. 
In order not to store a password in the configuration, the application will ask for it when the application is started.

```json
{
  "WebFtpSettings": [
    {
      "Name": "", 
      "Host": "",
      "Username": "",
      "RemotePath": "",
      "LocalPath": ""
    }
  ]
}
```
