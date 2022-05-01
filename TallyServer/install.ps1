$name = "TallyServer"
$user = "TallyServer"

$description = "TallyServer from https://github.com/Dalesjo/Atem-Tally"
$displayName = "TallyServer"

$binary = $PSScriptRoot + "\TallyServer.exe"
$directory = $PSScriptRoot


$op = Get-LocalUser | where-Object Name -eq $user | Measure
if ($op.Count -ne 0) {
    Write-Output "User $user does not exist, creating user, Please use another username.";
    exit 1;
} 

#Write-Output "Creating $user, please select a secure password";
$account = New-LocalUser -Name $user 

# Set Permissions for user
Write-Output "Setting  permissions for user $user on path $directory";
$acl = Get-Acl "$directory"
$aclRuleArgs = "$user", "Read,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($aclRuleArgs)
$acl.SetAccessRule($accessRule)
$acl | Set-Acl "$directory"

# Remove Service
sc.exe delete $name

# Create Service
Write-Output "Creating Service, please fill in the password for user $user"
#New-Service -Name $name -BinaryPathName $binary -Credential $user -Description $description -DisplayName $displayName -StartupType Automatic

$params = @{
    Name = $name
    BinaryPathName = $binary
    DisplayName = $displayName
    StartupType = "Automatic"
    Description = $description
    Credential = $account
  }

New-Service @params
Start-Service -Name $name
