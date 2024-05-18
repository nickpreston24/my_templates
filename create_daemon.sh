#!/usr/bin/env bash

print_help() {
  cat <<EOF

  create_daemon.sh 👀

  Usage:
    create_daemon.sh --inline <ip>

  Options:
    --name : name your new .net daemon.

EOF
}


# STEP 1. - Create worker application
create_daemon() {
  echo "daemon name set to $DAEMON_NAME";
  mkdir -p $DAEMON_NAME
  cd $DAEMON_NAME
  dotnet new console
}

install_coravel(){
  dotnet add package Coravel;
  dotnet add package  Microsoft.Extensions.Hosting;
  dotnet add package  Microsoft.Extensions.Hosting.Systemd;
}

install_codemechanic_dependencies(){

## Add nuget.config for all CodeMechanic dependencies
cat > nuget.config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="MyGet" value="https://www.myget.org/F/code-mechanic/api/v3/index.json" />
    </packageSources>
    <packageSourceCredentials>
        <MyGet>
            <add key="Username" value="nickpreston17" />
            <add key="ClearTextPassword" value="************" />
        </MyGet>
    </packageSourceCredentials>
    <activePackageSource>
        <add key="All" value="(Aggregate source)" />
    </activePackageSource>
</configuration>

EOF

dotnet add package CodeMechanic.Types;
dotnet add package CodeMechanic.Diagnostics;
dotnet add package CodeMechanic.FileSystem;
dotnet add package CodeMechanic.Systemd.Daemons;
}

create_program(){

## Change Program.cs
cat > Program.cs <<EOF
using CodeMechanic.Systemd.Daemons;
using Coravel;
using Coravel.Invocable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace $DAEMON_NAME;

public class MyFirstInvocable : IInvocable
{
    public async Task Invoke()
    {
        /// Sample MySQL logging (requires MYSQL_* .env variables to be set in your new .env).
        if (Environment.GetEnvironmentVariable("MYSQLPASSWORD").NotEmpty())
        {
            int rows = await MySQLExceptionLogger.LogInfo("Invoking from /srv!", nameof($DAEMON_NAME));
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        IHost host = CreateHostBuilder(args)
            .UseSystemd()
            .Build();

        host.Services.UseScheduler(scheduler =>
        {
            // Yes, it's this easy!
            scheduler
                .Schedule<MyFirstInvocable>()
                .EveryFiveSeconds();
            // Console.WriteLine("cool. I loaded the host w/o dying...");
        });

        host.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddScheduler();
                services.AddTransient<MyFirstInvocable>();
            });
}
EOF

install_codemechanic_dependencies;
install_coravel;

# Restore dependencies
dotnet restore

# Publish to a local bin sub directory
dotnet publish --configuration Release --output bin

# Run local to verify all is good
# dotnet ./bin/$DAEMON_NAME.dll

}


publish_to_srv(){
  echo "publishing daemon to your /srv/ folder"
  sudo mkdir -p /srv/  # if srv doesn't exist, create it.
  sudo mkdir -p /srv/$DAEMON_NAME
  sudo dotnet publish --configuration Release --output /srv/$DAEMON_NAME

  echo "setting new chmod permissions on this new daemon"
  chmod +x /srv/$DAEMON_NAME/$DAEMON_NAME

  touch .env
  cp .env /srv/$DAEMON_NAME
}


##  2 - CREATE .SYSTEMD File
create_systemd_file() {
echo "Creating local systemd file ..."

cat > $DAEMON_NAME.service <<EOF

[Unit]
Description=Long running service/daemon created from .NET worker template

[Service]
Type=notify
# will set the Current Working Directory (CWD). Worker service will have issues without this setting
WorkingDirectory=/srv/$DAEMON_NAME
# systemd will run this executable to start the service
# if /usr/bin/dotnet doesn't work, use `which dotnet` to find correct dotnet executable path
ExecStart=/usr/bin/dotnet /srv/$DAEMON_NAME/$DAEMON_NAME.dll
# to query logs using journalctl, set a logical name here
SyslogIdentifier=$DAEMON_NAME

# Use your username to keep things simple.
# If you pick a different user, make sure dotnet and all permissions are set correctly to run the app
# To update permissions, use 'chown $USER -R /srv/$DAEMON_NAME' to take ownership of the folder and files,
#       Use 'chmod +x /srv/$DAEMON_NAME/$DAEMON_NAME' to allow execution of the executable file
User=$USER

# ensure the service restarts after crashing
Restart=always
# amount of time to wait before restarting the service                  
RestartSec=5

# This environment variable is necessary when dotnet isn't loaded for the specified user.
# To figure out this value, run 'env | grep DOTNET_ROOT' when dotnet has been loaded into your shell.
Environment=DOTNET_ROOT=/usr/lib64/dotnet  

[Install]
WantedBy=multi-user.target


EOF
  
}


configure_systemd() {

echo "Configuring systemd..."

  # Copy service file to a System location
sudo cp $DAEMON_NAME.service /lib/systemd/system

# Reload SystemD and enable the service, so it will restart on reboots
sudo systemctl daemon-reload 
sudo systemctl enable $DAEMON_NAME

# Start service
sudo systemctl start $DAEMON_NAME 

# View service status
systemctl status $DAEMON_NAME

}


create_republish_script(){

cat > republish_$DAEMON_NAME.sh <<EOF
echo "stopping running service ... "
sudo systemctl stop $DAEMON_NAME # stop the $DAEMON_NAME service to remove any file-locks
echo "service stopped."
echo "Republishing service..."
sudo dotnet publish -c Release -o /srv/$DAEMON_NAME # release to your user directory
sudo cp .env /srv/$DAEMON_NAME/.env

echo "Updating systemctl ..."
sudo cp $DAEMON_NAME.service /etc/systemd/system/$DAEMON_NAME.service
sudo systemctl daemon-reload
sudo systemctl start $DAEMON_NAME  

echo "restarting service..."
sudo systemctl start $DAEMON_NAME # start service
EOF
}


create_stop_script(){
cat > stop_$DAEMON_NAME.sh <<EOF
sudo systemctl stop $DAEMON_NAME
EOF
echo "created stop script."
}


create_tail_script(){
cat > tail_$DAEMON_NAME.sh <<EOF
journalctl --unit $DAEMON_NAME --follow
EOF
echo "created tail script."
}

prompt_add_daemon_alias(){
  
read -p "Create an alias for $DAEMON_NAME? (yes/no) " yn

case $yn in 
	yes ) 
  echo "ok, we will proceed..."
  alias $DAEMON_NAME="$DAEMON_NAME"
  ;;
	no ) echo skipping...;
		exit;;
	* ) echo invalid response;
		exit 1;;
esac

  ## TODO: Ask the user if he wants to add his brand new daemon as an alias on his system.
}


# cli switcher
case "$1" in
  --name)
      DAEMON_NAME=${2}
      create_daemon;
      create_program;
      publish_to_srv;
      create_systemd_file;
      create_stop_script;
      create_republish_script;
      create_tail_script;
      configure_systemd;

      # prompt_add_daemon_alias;
      # cd ./$DAEMON_NAME


    ;;

# case "$1" in
#   --nuke)
#       sh nuke_$DAEMON_NAME.sh
#     ;;
  *)
    print_help
    ;;
esac

