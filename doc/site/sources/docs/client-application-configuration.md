# Client application configuration

You can register custom client applications by creating an _IAP Application Protocol Configuration_ (IAPC) file. An
IAPC file defines the following:

* The name of the client application
* The VM instances that the client application applies to
* The executable to launch (optional)

IAPC are JSON files that use the `.iapc` file extension. During startup, IAP Desktop loads all
IAPC files in the `%appdata%\Google\IAP Desktop\Config` folder. 

The following code snippet shows an example file for the MySQL shell:

```
{
  "version": 1,
  "name": "MySQL Shell 8.0",
  "condition": "isInstance()",
  "remotePort": "3306",
  "client": {
    "executable": "%ProgramW6432%\\MySQL\\MySQL Shell 8.0\\bin\\mysqlsh.exe",
    "arguments": "-h {host} -P {port} -u \"{username}\""
  }
}
```

<table>
    <tr>
        <th>Field</th>
        <th>Synopsis</th>
        <th>Required</th>
    <tr>
    <tr>
        <td><code>version</code></td>
        <td>File format version, must be <code>1</code>.</td>
        <td>Required</td>
    <tr>
    <tr>
        <td><code>name</code></td>
        <td>Name of the application as shown in the user interface.</td>
        <td>Required</td>
    <tr>
    <tr>
        <td><code>condition</code></td>
        <td>Condition that defines which VM instances this client applies to.
            <bt/><bt/>
            The following conditions are supported:
            <ul>
                <li><code>isInstance()</code>: All instances</li>
                <li><code>isLinux()</code>: Linux instances</li>
                <li><code>isWindows()</code>: Windows instances</li>
            </ul>
        </td>
        <td>Optional</td>
    <tr>
    <tr>
        <td><code>executable</code></td>
        <td>Executable to be launched. This can be one of:
            <ul>
                <li>an absolute file system path to an <code>.exe</code> file. The path
                    can contain environment variables, for example <code>%AppData%\Altostrat\client.exe</code>.</li>
                <li>name of a <a href='https://learn.microsoft.com/en-us/windows/win32/shell/app-registration'>registered application</a>,
                    for example <code>chrome.exe</code>.</li>
            </ul>
            Note: Backslashes must be escaped with another backslash (<code>\\</code>).
        </td>
        <td>Optional</td>
    <tr>
    <tr>
        <td><code>arguments</code></td>
        <td>Command line arguments for the client executable. The value can contain environment variables
            such as <code>%AppData%</code> or <code>%USERPROFILE%</code>.
            Additionally, the command line arguments can contain the following special variables:
            <ul>
                <li><code>{host}</code>: Hostname to connect to</li>
                <li><code>{port}</code>: Port to connect to</li>
                <li><code>{user}</code>: Username to authenticate with</li>
            </ul>
        </td>
        <td>Optional</td>
    <tr>
</table>